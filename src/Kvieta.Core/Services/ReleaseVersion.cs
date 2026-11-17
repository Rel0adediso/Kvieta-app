using System.Text.RegularExpressions;

namespace Kvieta.Core.Services;

public static class ReleaseVersion
{
    public static bool IsNewer(string current, string remote)
    {
        if (!TryParse(current, out Version? left, out string[] leftPre) || !TryParse(remote, out Version? right, out string[] rightPre)) return false;
        int result = right!.CompareTo(left);
        if (result != 0) return result > 0;
        if (leftPre.Length == 0 || rightPre.Length == 0) return leftPre.Length > 0 && rightPre.Length == 0;
        for (int i = 0; i < Math.Min(leftPre.Length, rightPre.Length); i++)
        {
            bool ln = int.TryParse(leftPre[i], out int l), rn = int.TryParse(rightPre[i], out int r);
            result = ln && rn ? r.CompareTo(l) : ln != rn ? (rn ? -1 : 1) : string.CompareOrdinal(rightPre[i], leftPre[i]);
            if (result != 0) return result > 0;
        }
        return rightPre.Length > leftPre.Length;
    }

    private static bool TryParse(string text, out Version? version, out string[] prerelease)
    {
        version = null;
        prerelease = [];
        if (string.IsNullOrWhiteSpace(text)) return false;
        string value = Regex.Replace(text.Trim(), @"^kvieta[-_. ]?", "", RegexOptions.IgnoreCase).Split('+')[0];
        Match legacy = Regex.Match(value, @"^alpha[-_. ]?(\d+(?:\.\d+)*)$", RegexOptions.IgnoreCase);
        if (legacy.Success) value = "1.0.0-alpha." + legacy.Groups[1].Value;
        value = value.TrimStart('v', 'V');
        Match match = Regex.Match(value, @"^(\d+)\.(\d+)(?:\.(\d+))?(?:-([0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*))?$", RegexOptions.CultureInvariant);
        if (!match.Success || !Version.TryParse($"{match.Groups[1].Value}.{match.Groups[2].Value}.{(match.Groups[3].Success ? match.Groups[3].Value : "0")}", out version)) return false;
        prerelease = match.Groups[4].Success ? match.Groups[4].Value.ToLowerInvariant().Split('.') : [];
        return true;
    }
}
