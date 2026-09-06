using Kvieta.Core.Services;

namespace Kvieta.App.Services;

internal static class UserNoticeBus
{
    public static UserNoticeCoordinator Current { get; } = new();
}
