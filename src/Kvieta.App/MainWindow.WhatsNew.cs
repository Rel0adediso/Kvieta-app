using System.Windows;
using Kvieta.App.Services;
using Kvieta.Core.Services;

namespace Kvieta.App;

public partial class MainWindow
{
    private ReleaseNotice? _releaseNotice;
    private static ReleaseIdentity CurrentRelease => new(BuildInfo.Version, BuildInfo.AssemblyVersion);

    private async Task LoadWhatsNewAsync()
    {
        try
        {
            _releaseNotice = await _displayPreferencesStore.ObserveReleaseAsync(CurrentRelease);
            WhatsNewBannerRange.Text = ReleaseNotesCatalog.FormatRange(_releaseNotice);
            WhatsNewBanner.Visibility = _releaseNotice.Unread ? Visibility.Visible : Visibility.Collapsed;
        }
        catch
        {
            // A presentation preference must never prevent normal startup.
            WhatsNewBanner.Visibility = Visibility.Collapsed;
        }
    }

    private async void WhatsNew_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ReleaseNotice notice = _releaseNotice ?? new ReleaseNotice(CurrentRelease, null, false);
            var window = new WhatsNewWindow(notice) { Owner = this };
            window.ShowDialog();
            await AcknowledgeWhatsNewAsync();
        }
        catch
        {
            _viewModel.StatusMessage = LocalizationService.Get("WhatsNewUnavailable");
        }
    }

    private async void DismissWhatsNew_Click(object sender, RoutedEventArgs e) => await AcknowledgeWhatsNewAsync();

    private async Task AcknowledgeWhatsNewAsync()
    {
        try
        {
            await _displayPreferencesStore.AcknowledgeReleaseAsync(CurrentRelease);
            WhatsNewBanner.Visibility = Visibility.Collapsed;
            if (_releaseNotice != null) _releaseNotice = _releaseNotice with { Unread = false };
        }
        catch { _viewModel.StatusMessage = LocalizationService.Get("WhatsNewUnavailable"); }
    }
}
