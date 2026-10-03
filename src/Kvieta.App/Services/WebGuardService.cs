using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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

    public static string CleanDomain(string input)
    {
        string cleaned = input.Trim().ToLowerInvariant();
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

        return cleaned;
    }
}
