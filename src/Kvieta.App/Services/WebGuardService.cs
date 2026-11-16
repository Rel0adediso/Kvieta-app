using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Kvieta.Core.Models;

namespace Kvieta.App.Services;

public static class WebGuardService
{
    private const string BlockStart = "# >>> KVIETA WEB GUARD >>>";
    private const string BlockEnd = "# <<< KVIETA WEB GUARD <<<";

    [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache", SetLastError = true)]
    private static extern int DnsFlushResolverCache();

    private static string GetHostsPath()
    {
        string systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        return Path.Combine(systemRoot, "System32", "drivers", "etc", "hosts");
    }

    public static bool Apply(ControlSettings settings)
    {
        if (!OperatingSystem.IsWindows()) return false;

        ApplyBrowserDohAndSafeSearch(settings.WebGuardEnabled, settings.SafeSearchEnforced);

        string hostsPath = GetHostsPath();
        if (!File.Exists(hostsPath)) return false;

        try
        {
            string content = File.ReadAllText(hostsPath, Encoding.UTF8);
            string sanitized = RemoveKvietaBlock(content);

            if (settings.WebGuardEnabled && (settings.BlockedWebDomains.Count > 0 || settings.SafeSearchEnforced))
            {
                var sb = new StringBuilder(sanitized.TrimEnd());
                sb.AppendLine();
                sb.AppendLine(BlockStart);

                if (settings.SafeSearchEnforced)
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

                if (settings.BlockedWebDomains.Count > 0)
                {
                    sb.AppendLine("# Blocked Domains");
                    foreach (string rawDomain in settings.BlockedWebDomains)
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

        // Strip any CRLF or null bytes to prevent hosts file line injection
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

    public static void ApplyBrowserDohAndSafeSearch(bool webGuardEnabled, bool safeSearchEnforced)
    {
        if (!OperatingSystem.IsWindows()) return;

        try
        {
            string[] browserPolicies = [@"Software\Policies\Google\Chrome", @"Software\Policies\Microsoft\Edge"];
            foreach (string basePath in browserPolicies)
            {
                using RegistryKey? key = Registry.LocalMachine.CreateSubKey(basePath);
                if (key is null) continue;

                if (webGuardEnabled)
                {
                    // Disable DoH so browser cannot bypass local hosts file
                    key.SetValue("DnsOverHttpsMode", "off", RegistryValueKind.String);
                    key.SetValue("BuiltInDnsClientEnabled", 0, RegistryValueKind.DWord);

                    if (safeSearchEnforced)
                    {
                        key.SetValue("ForceGoogleSafeSearch", 1, RegistryValueKind.DWord);
                        key.SetValue("ForceYouTubeRestrict", 2, RegistryValueKind.DWord);
                    }
                    else
                    {
                        key.DeleteValue("ForceGoogleSafeSearch", false);
                        key.DeleteValue("ForceYouTubeRestrict", false);
                    }
                }
                else
                {
                    key.DeleteValue("DnsOverHttpsMode", false);
                    key.DeleteValue("BuiltInDnsClientEnabled", false);
                    key.DeleteValue("ForceGoogleSafeSearch", false);
                    key.DeleteValue("ForceYouTubeRestrict", false);
                }
            }
        }
        catch
        {
            // Silently ignore if non-elevated; applied when elevated or via Guardian
        }
    }
}
