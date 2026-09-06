namespace Kvieta.Core.Services;

public enum UserNoticePriority
{
    WeeklySuggestion = 0,
    RhythmCelebration = 1,
    FocusOutcome = 2,
    TimeWarning = 3,
    AccessBoundary = 4,
    CriticalHealth = 5
}

public enum UserNoticeDecision
{
    Present,
    Deferred,
    Suppressed
}

public sealed record UserNotice(
    string EventId,
    string MergeKey,
    UserNoticePriority Priority,
    DateTimeOffset ExpiresAtUtc);

public sealed class UserNoticeCoordinator
{
    private readonly Dictionary<string, UserNotice> _latestByMergeKey = new(StringComparer.Ordinal);
    private readonly HashSet<string> _presentedEventIds = new(StringComparer.Ordinal);

    public UserNoticeDecision Evaluate(UserNotice notice, DateTimeOffset now, bool deferLowPriority)
    {
        ArgumentNullException.ThrowIfNull(notice);
        if (notice.ExpiresAtUtc <= now || _presentedEventIds.Contains(notice.EventId))
        {
            return UserNoticeDecision.Suppressed;
        }

        if (_latestByMergeKey.TryGetValue(notice.MergeKey, out UserNotice? current) &&
            current.ExpiresAtUtc > now && current.Priority > notice.Priority)
        {
            return UserNoticeDecision.Suppressed;
        }

        _latestByMergeKey[notice.MergeKey] = notice;
        if (deferLowPriority && notice.Priority < UserNoticePriority.AccessBoundary)
        {
            return UserNoticeDecision.Deferred;
        }

        _presentedEventIds.Add(notice.EventId);
        return UserNoticeDecision.Present;
    }
}
