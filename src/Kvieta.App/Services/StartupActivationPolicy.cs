namespace Kvieta.App.Services;

public static class StartupActivationPolicy
{
    public static string GetInstanceChannel(bool guardianSession, bool directSessionRequested) =>
        guardianSession ? "GuardianSession" : directSessionRequested ? "DirectSession" : "ControlCenter";

    public static bool ShouldSignalControlCenter(bool guardianSession, bool directSessionRequested) =>
        !guardianSession && !directSessionRequested;

    public static bool ShouldRegisterUserStartup(bool startWithWindows, bool requiresGuardian) =>
        startWithWindows && !requiresGuardian;

    public static bool ShouldDeferDirectSessionToGuardian(
        bool guardianSession,
        bool directSessionRequested,
        bool requiresGuardian,
        bool guardianAvailable) =>
        !guardianSession && directSessionRequested && requiresGuardian && guardianAvailable;
}
