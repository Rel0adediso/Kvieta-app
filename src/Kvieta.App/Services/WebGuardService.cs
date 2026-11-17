using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Kvieta.Core.Models;

namespace Kvieta.App.Services;

public sealed record WebGuardPayload(
    bool WebGuardEnabled,
    bool SafeSearchEnforced,
    bool BlockShortFormVideo,
    List<string> BlockedWebDomains);

public static class WebGuardService
{
    private const string BlockStart = "# >>> KVIETA WEB GUARD >>>";
    private const string BlockEnd = "# <<< KVIETA WEB GUARD <<<";

    [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache", SetLastError = true)]
    private static extern int DnsFlushResolverCache();

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern nint SendMessageTimeout(
        nint hWnd,
        uint msg,
        nuint wParam,
        string lParam,
        uint fuFlags,
        uint uTimeout,
        out nuint lpdwResult);

    private const nint HWND_BROADCAST = 0xffff;
    private const uint WM_SETTINGCHANGE = 0x001A;
    private const uint SMTO_ABORTIFHUNG = 0x0002;

    private static string GetHostsPath()
    {
        string systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        return Path.Combine(systemRoot, "System32", "drivers", "etc", "hosts");
    }

    public static bool IsAdministrator()
    {
        if (!OperatingSystem.IsWindows()) return false;
        try
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public static WebGuardPayload FromSettings(ControlSettings settings) => new(
        settings.WebGuardEnabled,
        settings.SafeSearchEnforced,
        settings.BlockShortFormVideo,
        settings.BlockedWebDomains ?? []);

    public static bool Apply(ControlSettings settings)
    {
        if (!OperatingSystem.IsWindows()) return false;

        WebGuardPayload payload = FromSettings(settings);

        if (!NeedsApply(payload))
        {
            return true;
        }

        if (IsAdministrator())
        {
            return ApplyDirect(payload);
        }

        // Try direct write first (in case running in an environment with granted permissions)
        try
        {
            if (ApplyDirect(payload))
            {
                return true;
            }
        }
        catch
        {
            // Fall through to elevation
        }

        // Request UAC elevation via self worker process
        return RunElevatedWorker(payload);
    }

    private static bool NeedsApply(WebGuardPayload payload)
    {
        if (payload.WebGuardEnabled || payload.BlockShortFormVideo || payload.SafeSearchEnforced || payload.BlockedWebDomains.Count > 0)
        {
            return true;
        }

        try
        {
            string hostsPath = GetHostsPath();
            if (File.Exists(hostsPath) && File.ReadAllText(hostsPath, Encoding.UTF8).Contains(BlockStart, StringComparison.Ordinal))
            {
                return true;
            }

            string[] checkPaths = [
                @"Software\Policies\BraveSoftware\Brave\URLBlocklist",
                @"Software\Policies\Google\Chrome\URLBlocklist",
                @"Software\Policies\Microsoft\Edge\URLBlocklist",
                @"Software\Policies\BraveSoftware\Brave\ExtensionInstallForcelist",
                @"Software\Policies\Google\Chrome\ExtensionInstallForcelist",
                @"Software\Policies\Microsoft\Edge\ExtensionInstallForcelist"
            ];
            foreach (string p in checkPaths)
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(p);
                if (key is not null) return true;
            }
        }
        catch
        {
        }

        return false;
    }

    private static bool RunElevatedWorker(WebGuardPayload payload)
    {
        try
        {
            string executablePath = Environment.ProcessPath ??
                Process.GetCurrentProcess().MainModule?.FileName ??
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Kvieta.exe");

            if (!File.Exists(executablePath))
            {
                return false;
            }

            string json = JsonSerializer.Serialize(payload);
            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

            ProcessStartInfo startInfo = new()
            {
                FileName = executablePath,
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("--apply-web-guard");
            startInfo.ArgumentList.Add(base64);

            using Process? process = Process.Start(startInfo);
            if (process is not null)
            {
                process.WaitForExit(12000);
                return process.ExitCode == 0;
            }

            return false;
        }
        catch
        {
            // User clicked No on UAC or elevation was cancelled
            return false;
        }
    }

    public static int ExecuteElevatedWorker(string? payloadBase64)
    {
        if (string.IsNullOrWhiteSpace(payloadBase64))
        {
            return 1;
        }

        try
        {
            byte[] bytes = Convert.FromBase64String(payloadBase64);
            string json = Encoding.UTF8.GetString(bytes);
            WebGuardPayload? payload = JsonSerializer.Deserialize<WebGuardPayload>(json);
            if (payload is null) return 1;

            return ApplyDirect(payload) ? 0 : 1;
        }
        catch
        {
            return 1;
        }
    }

    public static bool ApplyDirect(WebGuardPayload payload)
    {
        if (!OperatingSystem.IsWindows()) return false;

        bool browserOk = ApplyBrowserPolicies(payload);
        bool hostsOk = ApplyHostsFile(payload);

        try
        {
            _ = SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, 0, "Policy", SMTO_ABORTIFHUNG, 2000, out _);
        }
        catch
        {
        }

        return browserOk && hostsOk;
    }

    private static bool ApplyHostsFile(WebGuardPayload payload)
    {
        string hostsPath = GetHostsPath();
        if (!File.Exists(hostsPath)) return false;

        try
        {
            string content = File.ReadAllText(hostsPath, Encoding.UTF8);
            string sanitized = RemoveKvietaBlock(content);

            bool shouldBlock = payload.WebGuardEnabled || payload.BlockShortFormVideo;
            if (shouldBlock && (payload.BlockedWebDomains.Count > 0 || payload.SafeSearchEnforced || payload.BlockShortFormVideo))
            {
                var sb = new StringBuilder(sanitized.TrimEnd());
                sb.AppendLine();
                sb.AppendLine(BlockStart);

                if (payload.SafeSearchEnforced)
                {
                    sb.AppendLine("# Google SafeSearch");
                    sb.AppendLine("216.239.38.120 forcesafesearch.google.com");
                    sb.AppendLine("216.239.38.120 google.com");
                    sb.AppendLine("216.239.38.120 www.google.com");
                    sb.AppendLine("216.239.38.120 google.com.tr");
                    sb.AppendLine("216.239.38.120 www.google.com.tr");

                    sb.AppendLine("# Bing Strict SafeSearch");
                    sb.AppendLine("204.79.197.220 strict.bing.com");
                    sb.AppendLine("204.79.197.220 bing.com");
                    sb.AppendLine("204.79.197.220 www.bing.com");

                    sb.AppendLine("# YouTube Restricted Mode");
                    sb.AppendLine("216.239.38.119 restrict.youtube.com");
                    sb.AppendLine("216.239.38.119 youtube.com");
                    sb.AppendLine("216.239.38.119 www.youtube.com");
                    sb.AppendLine("216.239.38.119 m.youtube.com");
                    sb.AppendLine("216.239.38.119 youtubei.googleapis.com");
                }

                if (payload.BlockShortFormVideo)
                {
                    sb.AppendLine("# Dopamine Trap Protection (TikTok)");
                    sb.AppendLine("0.0.0.0 tiktok.com");
                    sb.AppendLine("0.0.0.0 www.tiktok.com");
                    sb.AppendLine("0.0.0.0 m.tiktok.com");
                    sb.AppendLine("0.0.0.0 v.tiktok.com");
                }

                if (payload.BlockedWebDomains.Count > 0)
                {
                    sb.AppendLine("# Blocked Domains");
                    foreach (string rawDomain in payload.BlockedWebDomains)
                    {
                        string domain = CleanDomain(rawDomain);
                        if (string.IsNullOrWhiteSpace(domain)) continue;

                        sb.AppendLine($"0.0.0.0 {domain}");
                        if (!domain.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
                        {
                            sb.AppendLine($"0.0.0.0 www.{domain}");
                        }
                    }
                }

                sb.AppendLine(BlockEnd);
                sanitized = sb.ToString();
            }

            FileAttributes originalAttributes = File.GetAttributes(hostsPath);
            if ((originalAttributes & FileAttributes.ReadOnly) != 0)
            {
                File.SetAttributes(hostsPath, originalAttributes & ~FileAttributes.ReadOnly);
            }

            File.WriteAllText(hostsPath, sanitized, Encoding.UTF8);

            if ((originalAttributes & FileAttributes.ReadOnly) != 0)
            {
                File.SetAttributes(hostsPath, originalAttributes);
            }

            try { DnsFlushResolverCache(); }
            catch { }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string RemoveKvietaBlock(string content)
    {
        int startIndex = content.IndexOf(BlockStart, StringComparison.Ordinal);
        if (startIndex == -1) return content;

        int endIndex = content.IndexOf(BlockEnd, StringComparison.Ordinal);
        if (endIndex == -1) return content.Substring(0, startIndex).TrimEnd() + Environment.NewLine;

        endIndex += BlockEnd.Length;
        string before = content.Substring(0, startIndex).TrimEnd();
        string after = content.Substring(endIndex).TrimStart();

        return string.IsNullOrEmpty(after)
            ? before + Environment.NewLine
            : before + Environment.NewLine + after;
    }

    private static readonly Regex DomainValidationRegex = new(
        @"^[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?(\.[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?)*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string CleanDomain(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        string cleaned = input.Replace("\r", string.Empty)
                              .Replace("\n", string.Empty)
                              .Replace("\t", string.Empty)
                              .Replace(" ", string.Empty)
                              .Trim()
                              .ToLowerInvariant();

        if (cleaned.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned.Substring(8);
        }
        else if (cleaned.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned.Substring(7);
        }

        int slashIndex = cleaned.IndexOf('/');
        if (slashIndex != -1)
        {
            cleaned = cleaned.Substring(0, slashIndex);
        }

        int colonIndex = cleaned.IndexOf(':');
        if (colonIndex != -1)
        {
            cleaned = cleaned.Substring(0, colonIndex);
        }

        return DomainValidationRegex.IsMatch(cleaned) ? cleaned : string.Empty;
    }

    private static bool ApplyBrowserPolicies(WebGuardPayload payload)
    {
        string[] chromiumPolicies = [
            @"Software\Policies\BraveSoftware\Brave",
            @"Software\Policies\Google\Chrome",
            @"Software\Policies\Microsoft\Edge",
            @"Software\Policies\Chromium",
            @"Software\Policies\Vivaldi",
            @"Software\Policies\Opera Software",
            @"Software\Policies\Opera"
        ];

        List<string> chromiumUrlBlocklist = [];
        List<string> firefoxUrlBlocklist = [];

        if (payload.BlockShortFormVideo)
        {
            // Chromium URLBlocklist schema: [scheme://][.]host[:port][/path] (no * wildcards in host or path)
            chromiumUrlBlocklist.AddRange([
                // YouTube Shorts navigations and internal streaming APIs
                ".youtube.com/shorts",
                "www.youtube.com/shorts",
                "youtube.com/shorts",
                "m.youtube.com/shorts",
                "youtu.be/shorts",
                ".youtube.com/youtubei/v1/reel",
                "www.youtube.com/youtubei/v1/reel",
                "youtube.com/youtubei/v1/reel",
                "m.youtube.com/youtubei/v1/reel",
                // Instagram Reels
                ".instagram.com/reels",
                "www.instagram.com/reels",
                "instagram.com/reels",
                ".instagram.com/reel",
                "www.instagram.com/reel",
                "instagram.com/reel",
                ".instagram.com/api/v1/clips",
                "www.instagram.com/api/v1/clips",
                "instagram.com/api/v1/clips",
                // Facebook Reels
                ".facebook.com/reel",
                "www.facebook.com/reel",
                "facebook.com/reel",
                ".facebook.com/reels",
                "www.facebook.com/reels",
                "facebook.com/reels",
                // TikTok
                ".tiktok.com",
                "tiktok.com",
                "www.tiktok.com",
                "m.tiktok.com",
                "v.tiktok.com"
            ]);

            // Firefox WebsiteFilter schema: WebExtension match patterns (*://<host>/<path>)
            firefoxUrlBlocklist.AddRange([
                "*://*.youtube.com/shorts*",
                "*://youtube.com/shorts*",
                "*://m.youtube.com/shorts*",
                "*://*.youtube.com/youtubei/v1/reel*",
                "*://*.instagram.com/reels*",
                "*://instagram.com/reels*",
                "*://*.instagram.com/reel*",
                "*://instagram.com/reel*",
                "*://*.facebook.com/reel*",
                "*://*.facebook.com/reels*",
                "*://facebook.com/reel*",
                "*://facebook.com/reels*",
                "*://*.tiktok.com/*",
                "*://tiktok.com/*"
            ]);
        }

        if (payload.WebGuardEnabled && payload.BlockedWebDomains.Count > 0)
        {
            foreach (string raw in payload.BlockedWebDomains)
            {
                string domain = CleanDomain(raw);
                if (string.IsNullOrWhiteSpace(domain)) continue;

                chromiumUrlBlocklist.Add(domain);

                firefoxUrlBlocklist.Add($"*://*.{domain}/*");
                firefoxUrlBlocklist.Add($"*://{domain}/*");
            }
        }

        bool allOk = true;
        foreach (string basePath in chromiumPolicies)
        {
            allOk &= ApplyToRegistryHive(Registry.LocalMachine, basePath, payload, chromiumUrlBlocklist);
            allOk &= ApplyToRegistryHive(Registry.CurrentUser, basePath, payload, chromiumUrlBlocklist);
        }

        ApplyToFirefox(payload, firefoxUrlBlocklist);
        RefreshBrowsers();
        return allOk;
    }

    private static void RefreshBrowsers()
    {
        string[] browserNames = ["msedge", "brave", "chrome"];
        foreach (string name in browserNames)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(name);
                bool hasVisibleWindow = processes.Any(p => p.MainWindowHandle != nint.Zero);
                // If the browser has no visible windows open, kill the background lingering processes
                // (e.g. Edge Startup Boost) so the next user launch is a cold start that re-reads policies.
                if (!hasVisibleWindow && processes.Length > 0)
                {
                    foreach (Process p in processes)
                    {
                        try { p.Kill(); } catch { }
                    }
                }
            }
            catch
            {
            }
        }
    }

    private static bool ApplyToRegistryHive(RegistryKey root, string basePath, WebGuardPayload payload, List<string> urlBlocklist)
    {
        try
        {
            using RegistryKey? key = root.CreateSubKey(basePath);
            if (key is null) return false;

            bool isProtectionActive = payload.WebGuardEnabled || payload.BlockShortFormVideo;

            if (isProtectionActive)
            {
                if (payload.WebGuardEnabled)
                {
                    key.SetValue("DnsOverHttpsMode", "off", RegistryValueKind.String);
                    key.SetValue("BuiltInDnsClientEnabled", 0, RegistryValueKind.DWord);
                }
                else
                {
                    key.DeleteValue("DnsOverHttpsMode", false);
                    key.DeleteValue("BuiltInDnsClientEnabled", false);
                }

                if (payload.SafeSearchEnforced)
                {
                    key.SetValue("ForceGoogleSafeSearch", 1, RegistryValueKind.DWord);
                    key.SetValue("ForceYouTubeRestrict", 2, RegistryValueKind.DWord);
                }
                else
                {
                    key.DeleteValue("ForceGoogleSafeSearch", false);
                    key.DeleteValue("ForceYouTubeRestrict", false);
                }

                if (urlBlocklist.Count > 0)
                {
                    using RegistryKey blocklistKey = key.CreateSubKey("URLBlocklist");
                    // Clean previous values
                    foreach (string valueName in blocklistKey.GetValueNames())
                    {
                        blocklistKey.DeleteValue(valueName, false);
                    }
                    for (int i = 0; i < urlBlocklist.Count; i++)
                    {
                        blocklistKey.SetValue((i + 1).ToString(), urlBlocklist[i], RegistryValueKind.String);
                    }
                }
                else
                {
                    key.DeleteSubKeyTree("URLBlocklist", false);
                }

                if (payload.BlockShortFormVideo)
                {
                    using RegistryKey extKey = key.CreateSubKey("ExtensionInstallForcelist");
                    extKey.SetValue("1", "jiaopdjbehhjgokpphdfgmapkobbnmjp;https://clients2.google.com/service/update2/crx", RegistryValueKind.String);
                }
                else
                {
                    key.DeleteSubKeyTree("ExtensionInstallForcelist", false);
                }
            }
            else
            {
                key.DeleteValue("DnsOverHttpsMode", false);
                key.DeleteValue("BuiltInDnsClientEnabled", false);
                key.DeleteValue("ForceGoogleSafeSearch", false);
                key.DeleteValue("ForceYouTubeRestrict", false);
                key.DeleteSubKeyTree("URLBlocklist", false);
                key.DeleteSubKeyTree("ExtensionInstallForcelist", false);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void ApplyToFirefox(WebGuardPayload payload, List<string> urlBlocklist)
    {
        string[] hives = [@"Software\Policies\Mozilla\Firefox"];
        RegistryKey[] roots = [Registry.LocalMachine, Registry.CurrentUser];

        foreach (RegistryKey root in roots)
        {
            foreach (string basePath in hives)
            {
                try
                {
                    using RegistryKey? key = root.CreateSubKey(basePath);
                    if (key is null) continue;

                    bool isProtectionActive = payload.WebGuardEnabled || payload.BlockShortFormVideo;
                    if (isProtectionActive)
                    {
                        if (payload.WebGuardEnabled)
                        {
                            using RegistryKey dohKey = key.CreateSubKey("DNSOverHTTPS");
                            dohKey.SetValue("Enabled", 0, RegistryValueKind.DWord);
                            dohKey.SetValue("Locked", 1, RegistryValueKind.DWord);
                        }
                        else
                        {
                            key.DeleteSubKeyTree("DNSOverHTTPS", false);
                        }

                        if (urlBlocklist.Count > 0)
                        {
                            using RegistryKey filterKey = key.CreateSubKey(@"WebsiteFilter\Block");
                            foreach (string valueName in filterKey.GetValueNames())
                            {
                                filterKey.DeleteValue(valueName, false);
                            }
                            for (int i = 0; i < urlBlocklist.Count; i++)
                            {
                                filterKey.SetValue((i + 1).ToString(), urlBlocklist[i], RegistryValueKind.String);
                            }
                        }
                        else
                        {
                            key.DeleteSubKeyTree("WebsiteFilter", false);
                        }
                    }
                    else
                    {
                        key.DeleteSubKeyTree("DNSOverHTTPS", false);
                        key.DeleteSubKeyTree("WebsiteFilter", false);
                    }
                }
                catch
                {
                }
            }
        }
    }
}
