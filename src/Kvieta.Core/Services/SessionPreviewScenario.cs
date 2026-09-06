using Kvieta.Core.Models;

namespace Kvieta.Core.Services;

public sealed record SessionPreviewScenario(
    bool IsSynthetic,
    int WarningMinutes,
    SessionState FinalState,
    IReadOnlyList<string> ExampleActionIds)
{
    public static SessionPreviewScenario Create() => new(
        true,
        5,
        SessionState.TimeExpired,
        ["request-time", "save-work"]);
}
