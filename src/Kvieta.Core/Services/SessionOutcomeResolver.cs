using Kvieta.Core.Models;

namespace Kvieta.Core.Services;

public enum SessionOutcomeKind
{
    None,
    FocusCompleted,
    FocusEndedEarly,
    DailyLimitReached,
    PlanEnded,
    ApplicationLimitReached
}

public sealed record SessionOutcome(
    string EventId,
    SessionOutcomeKind FocusOutcome,
    SessionOutcomeKind AccessOutcome,
    bool CanContinueFocus)
{
    public bool HasFocusOutcome => FocusOutcome is SessionOutcomeKind.FocusCompleted or SessionOutcomeKind.FocusEndedEarly;
    public bool HasAccessBoundary => AccessOutcome is not SessionOutcomeKind.None;
}

public static class SessionOutcomeResolver
{
    public static SessionOutcome Resolve(
        string eventSourceId,
        bool focusCompleted,
        bool focusEndedEarly,
        SessionState state,
        bool applicationLimitReached = false,
        bool outsideScheduleIsPlanEnd = true)
    {
        string safeSource = string.IsNullOrWhiteSpace(eventSourceId) ? "session" : eventSourceId.Trim();
        SessionOutcomeKind focus = focusCompleted
            ? SessionOutcomeKind.FocusCompleted
            : focusEndedEarly
                ? SessionOutcomeKind.FocusEndedEarly
                : SessionOutcomeKind.None;
        SessionOutcomeKind access = applicationLimitReached
            ? SessionOutcomeKind.ApplicationLimitReached
            : state == SessionState.TimeExpired
                ? SessionOutcomeKind.DailyLimitReached
                : state == SessionState.OutsideSchedule && outsideScheduleIsPlanEnd
                    ? SessionOutcomeKind.PlanEnded
                    : SessionOutcomeKind.None;
        string eventId = $"{safeSource}:{focus}:{access}";
        return new SessionOutcome(
            eventId,
            focus,
            access,
            focus is SessionOutcomeKind.FocusCompleted or SessionOutcomeKind.FocusEndedEarly &&
            access == SessionOutcomeKind.None);
    }
}
