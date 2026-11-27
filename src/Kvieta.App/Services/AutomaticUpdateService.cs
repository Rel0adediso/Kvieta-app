using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using Kvieta.Core.Services;

namespace Kvieta.App.Services;

internal static class AutomaticUpdateService
{
    private const string SystemAccount = "SYSTEM";
    private const string AdministratorsOnly = "D:P(A;;FA;;;SY)(A;;FA;;;BA)";
    private const string UsersMayRun = "D:P(A;;FA;;;SY)(A;;FA;;;BA)(A;;FRFX;;;BU)";
    private static string LastCheckPath => Path.Combine(StartupUpdate.CacheDirectory, "last-check");
    private static Version CurrentVersion => Version.Parse(BuildInfo.AssemblyVersion);

    public static bool Enabled => CanUseInstalledUpdater() && StartupUpdate.Enabled;

    private static bool IsInstalledExecutable() =>
        string.Equals(Environment.ProcessPath, StartupUpdate.Executable, StringComparison.OrdinalIgnoreCase);

    private static bool CanUseInstalledUpdater() => IsInstalledExecutable() && !BuildInfo.IsDevelopmentBuild &&
        BuildInfo.StablePublicKey is { Length: > 0 } &&
        ProtectionServiceManager.IsInstallerManaged &&
        ProtectionServiceManager.RegisteredPackageKind == "community" &&
        ProtectionServiceManager.ReadRegisteredVersionForIdentity() == CurrentVersion;

    public static bool TryLaunchPreparedUpdate()
    {
        if (!Enabled || !Environment.UserInteractive) return false;
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            if (identity.IsSystem) return false;
            if (StartupUpdate.ReadyVersion(CurrentVersion, DateTimeOffset.UtcNow) is not { } target) return false;

            var launch = new ProcessStartInfo(StartupUpdate.SetupPath(target)) { UseShellExecute = false };
            launch.ArgumentList.Add(StartupUpdate.WaitArgument);
            using var waiter = Process.Start(launch);
            return waiter != null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    internal static async Task<int> RunWorkerAsync(string command)
    {
        if (!IsInstalledExecutable()) return 5;
        using var identity = WindowsIdentity.GetCurrent();
        bool elevated = identity.IsSystem || new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        try
        {
            switch (command)
            {
                case StartupUpdate.RegisterArgument when elevated:
                    if (!CanUseInstalledUpdater()) return 2;
                    RegisterTasks();
                    return 0;
                case StartupUpdate.RemoveArgument when elevated:
                    ProtectionServiceManager.ConfigureSafeBootRegistration(false);
                    RemoveTasks();
                    return 0;
                case StartupUpdate.PrepareArgument when identity.IsSystem && Enabled:
                    await PrepareAsync();
                    return 0;
                case StartupUpdate.ApplyArgument when identity.IsSystem && Enabled:
                    await ApplyAsync();
                    return 0;
                default:
                    return 5;
            }
        }
        catch (Exception exception)
        {
            Trace.TraceError("Kvieta automatic update task {0}: {1}", command, exception.Message);
            return 1;
        }
    }

    private static async Task PrepareAsync()
    {
        if (!DirectoryIsTrusted() || DateTime.UtcNow - File.GetLastWriteTimeUtc(LastCheckPath) < TimeSpan.FromMinutes(15))
            return;
        StartupUpdate.WriteAtomically(LastCheckPath, DateTimeOffset.UtcNow.ToString("O"));
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        var package = await UpdateCheckService.ReadStablePackageAsync(BuildInfo.StablePublicKey!, timeout.Token);
        if (package == null) return;
        var version = Version.Parse(package.Version);
        if (version <= CurrentVersion) return;
        if (package.GuardianRevision != BuildInfo.GuardianRevision)
        {
            File.Delete(StartupUpdate.PendingPath);
            return;
        }
        if (StartupUpdate.ReadyVersion(CurrentVersion, DateTimeOffset.UtcNow) is { } ready && ready >= version) return;
        await UpdateCheckService.StageStartupUpdateAsync(package, timeout.Token);
        StartupUpdate.WriteAtomically(StartupUpdate.PendingPath, package.Version);
    }

    private static async Task ApplyAsync()
    {
        if (!DirectoryIsTrusted() || StartupUpdate.ReadyVersion(CurrentVersion, DateTimeOffset.UtcNow) is not { } target)
            return;
        if (StartupUpdate.ReadAttempt() is { ExitCode: null } running &&
            DateTimeOffset.UtcNow - running.StartedAt < StartupUpdate.InstallationTimeout) return;

        var attempt = new StartupUpdate.Attempt(target, DateTimeOffset.UtcNow);
        StartupUpdate.WriteAttempt(attempt);
        try
        {
            string expected = StartupUpdate.SetupPath(target);
            string metadata = expected + ".json";
            if (new FileInfo(metadata).Length > 1024 * 1024)
                throw new InvalidDataException("Automatic update feed entry is too large.");
            var candidate = StableUpdatePackage.Verify(await File.ReadAllBytesAsync(metadata), BuildInfo.StablePublicKey!);
            using var locked = File.OpenRead(expected);
            if (candidate.Version != target.ToString(3) ||
                !candidate.CanApplySetup(locked, BuildInfo.StablePublicKey!, CurrentVersion, BuildInfo.GuardianRevision))
                throw new InvalidDataException("Automatic update Setup failed validation.");

            dynamic scheduler = StartupUpdate.ConnectScheduler();
            dynamic task = RegisterTask(scheduler, StartupUpdate.InstallTask, expected, StartupUpdate.InstallArgument, AdministratorsOnly);
            task.Run(null);
        }
        catch
        {
            StartupUpdate.WriteAttempt(attempt with { ExitCode = 1603 });
            throw;
        }
    }

    private static void RegisterTasks()
    {
        var directory = System.IO.Directory.CreateDirectory(StartupUpdate.CacheDirectory);
        if (!DirectoryIsTrusted()) throw new IOException("The automatic update directory is a reparse point.");
        var permissions = new DirectorySecurity();
        permissions.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        foreach (var (sid, rights) in new[]
        {
            (WellKnownSidType.LocalSystemSid, FileSystemRights.FullControl),
            (WellKnownSidType.BuiltinAdministratorsSid, FileSystemRights.FullControl),
            (WellKnownSidType.BuiltinUsersSid, FileSystemRights.ReadAndExecute)
        })
        {
            permissions.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(sid, null), rights,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        }
        directory.SetAccessControl(permissions);

        dynamic scheduler = StartupUpdate.ConnectScheduler();
        RegisterTask(scheduler, StartupUpdate.PrepareTask, StartupUpdate.Executable, StartupUpdate.PrepareArgument, UsersMayRun, periodic: true);
        RegisterTask(scheduler, StartupUpdate.ApplyTask, StartupUpdate.Executable, StartupUpdate.ApplyArgument, UsersMayRun);
        StartupUpdate.WriteAtomically(StartupUpdate.RegistrationPath, CurrentVersion.ToString(3));
    }

    private static void RemoveTasks()
    {
        if (DirectoryIsTrusted()) File.Delete(StartupUpdate.RegistrationPath);
        dynamic scheduler = StartupUpdate.ConnectScheduler();
        dynamic folder = scheduler.GetFolder("\\");
        foreach (string name in new[] { StartupUpdate.PrepareTask, StartupUpdate.ApplyTask, StartupUpdate.InstallTask })
        {
            try { folder.DeleteTask(name, 0); }
            catch (Exception exception) when (exception is FileNotFoundException ||
                exception is COMException { HResult: unchecked((int)0x80070002) })
            {
            }
        }
        if (!DirectoryIsTrusted()) return;
        foreach (string file in Directory.EnumerateFiles(StartupUpdate.CacheDirectory))
        {
            try { File.Delete(file); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        if (!Directory.EnumerateFileSystemEntries(StartupUpdate.CacheDirectory).Any())
            Directory.Delete(StartupUpdate.CacheDirectory);
    }

    private static bool DirectoryIsTrusted() => Directory.Exists(StartupUpdate.CacheDirectory) &&
        !File.GetAttributes(StartupUpdate.CacheDirectory).HasFlag(FileAttributes.ReparsePoint);

    private static dynamic RegisterTask(dynamic scheduler, string name, string executable, string arguments,
        string permissions, bool periodic = false)
    {
        dynamic task = scheduler.NewTask(0);
        task.RegistrationInfo.Description = "Kvieta signed stable updates";
        task.Settings.DisallowStartIfOnBatteries = false;
        task.Settings.StopIfGoingOnBatteries = false;
        task.Settings.StartWhenAvailable = true;
        task.Settings.ExecutionTimeLimit = "PT25M";
        task.Settings.MultipleInstances = 2; // Ignore new instances.
        if (periodic)
        {
            dynamic trigger = task.Triggers.Create(1); // TASK_TRIGGER_TIME
            trigger.StartBoundary = DateTime.Now.AddMinutes(1).ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
            trigger.Repetition.Interval = "PT6H";
        }
        dynamic action = task.Actions.Create(0); // TASK_ACTION_EXEC
        action.Path = executable;
        action.Arguments = arguments;
        action.WorkingDirectory = Path.GetDirectoryName(executable);
        dynamic root = scheduler.GetFolder("\\");
        return root.RegisterTaskDefinition(name, task, 6, SystemAccount, null, 5, permissions);
    }
}
