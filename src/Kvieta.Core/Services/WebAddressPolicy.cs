namespace Kvieta.Core.Services;

/// <summary>Host/path matching shared by browser enforcement and regression checks.</summary>
public static class WebAddressPolicy
{
    public readonly record struct BrowserObservation(nint Window, uint ProcessId, string Address, DateTimeOffset ObservedAt)
    {
        public bool Confirms(BrowserObservation previous) => Window != 0 && ProcessId != 0 &&
            Window == previous.Window && ProcessId == previous.ProcessId &&
            string.Equals(Address, previous.Address, StringComparison.Ordinal) &&
            ObservedAt >= previous.ObservedAt && ObservedAt - previous.ObservedAt <= TimeSpan.FromSeconds(2);
    }

    public static string BrowserStartPage(string? browserProcess) => browserProcess?.ToLowerInvariant() switch
    {
        "chrome" => "chrome://newtab/",
        "msedge" => "edge://newtab/",
        "firefox" => "about:newtab",
        _ => "about:blank"
    };

    public static Uri? Parse(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        string text = address.Trim();
        if (!text.Contains("://", StringComparison.Ordinal)) text = "https://" + text;
        return Uri.TryCreate(text, UriKind.Absolute, out Uri? uri) &&
               uri.Scheme is "http" or "https" && string.IsNullOrEmpty(uri.UserInfo) ? uri : null;
    }

    public static bool MatchesDomain(string? address, string domain) =>
        Parse(address) is { } uri && HostMatches(uri.IdnHost, domain.TrimEnd('.'));

    private static bool HostMatches(string host, string domain) =>
        host.TrimEnd('.').Equals(domain, StringComparison.OrdinalIgnoreCase) ||
        host.TrimEnd('.').EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);

    public static bool IsShortVideo(string? address)
    {
        if (Parse(address) is not { } uri) return false;
        string path = uri.AbsolutePath;
        bool PathIs(string prefix) => path.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
                                      path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);
        return HostMatches(uri.IdnHost, "tiktok.com") ||
               HostMatches(uri.IdnHost, "youtube.com") && PathIs("/shorts") ||
               HostMatches(uri.IdnHost, "instagram.com") && (PathIs("/reel") || PathIs("/reels")) ||
               HostMatches(uri.IdnHost, "facebook.com") && (PathIs("/reel") || PathIs("/reels"));
    }
}
