using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;

namespace Kvieta.Core.Services;

public static class StartupUpdate
{
    public const string PrepareTask = "Kvieta Prepare Update";
    public const string ApplyTask = "Kvieta Apply Update";
    public const string InstallTask = "Kvieta Install Update";
    public const string RegisterArgument = "--register-startup-updates";
    public const string RemoveArgument = "--remove-startup-updates";
    public const string PrepareArgument = "--prepare-startup-update";
    public const string ApplyArgument = "--apply-startup-update";
    public const string InstallArgument = "--install-startup-update";
    public const string WaitArgument = "--wait-for-startup-update";
    public const string ResumeArgument = "--resume-after-startup-update";

    public static readonly TimeSpan InstallationTimeout = TimeSpan.FromMinutes(4);
    public static string InstallDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Kvieta");
    public static string Executable => Path.Combine(InstallDirectory, "Kvieta.exe");
    public static string CacheDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Kvieta.UpdateCache");
    public static string RegistrationPath => Path.Combine(CacheDirectory, "enabled");
    public static string PendingPath => Path.Combine(CacheDirectory, "pending.version");
    private static string AttemptPath => Path.Combine(CacheDirectory, "attempt.json");
    public static string SetupPath(Version version) => Path.Combine(CacheDirectory, StableSetupCache.FileName(version.ToString(3)));

    public static bool Enabled => File.Exists(RegistrationPath);

    public sealed record Attempt(Version Version, DateTimeOffset StartedAt, int? ExitCode = null);

    public static Attempt? ReadAttempt()
    {
        try
        {
            using var stream = OpenStateFile(AttemptPath);
            return stream.Length <= 4096 ? JsonSerializer.Deserialize<Attempt>(stream) : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    public static Version? ReadyVersion(Version installed, DateTimeOffset now)
    {
        try
        {
            using var stream = OpenStateFile(PendingPath);
            if (stream.Length > 64) return null;
            using var reader = new StreamReader(stream);
            if (!Version.TryParse(reader.ReadToEnd(), out var version) || version.Build < 0 || version.Revision >= 0 ||
                version <= installed || !File.Exists(SetupPath(version))) return null;
            if (ReadAttempt() is { } attempt && attempt.Version == version)
            {
                if (attempt.ExitCode is 0 or 1641 or 3010) return null;
                if (now - attempt.StartedAt >= InstallationTimeout && now - attempt.StartedAt < TimeSpan.FromHours(1))
                    return null;
                if (attempt.ExitCode is not null && now - attempt.StartedAt < TimeSpan.FromHours(1))
                    return null;
            }
            return version;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static void WriteAttempt(Attempt attempt) => WriteAtomically(AttemptPath, JsonSerializer.Serialize(attempt));

    private static FileStream OpenStateFile(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);

    public static void WriteAtomically(string path, string content)
    {
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, content);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    [SupportedOSPlatform("windows")]
    public static object ConnectScheduler()
    {
        var type = Type.GetTypeFromProgID("Schedule.Service", throwOnError: true)!;
        dynamic scheduler = Activator.CreateInstance(type)!;
        scheduler.Connect();
        return scheduler;
    }

    [SupportedOSPlatform("windows")]
    public static Task<bool> RequestTaskAsync(string task) => Task.Run(() =>
    {
        try
        {
            dynamic scheduler = ConnectScheduler();
            dynamic registered = scheduler.GetFolder("\\").GetTask(task);
            registered.Run(null);
            return true;
        }
        catch (Exception exception) when (exception is COMException or UnauthorizedAccessException or
            InvalidOperationException or FileNotFoundException)
        {
            return false;
        }
    });
}
