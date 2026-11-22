using System.Windows;
using Kvieta.App.Services;
using Kvieta.Core.Services;

namespace Kvieta.App;

public partial class WhatsNewWindow : Window
{
    public WhatsNewWindow(ReleaseNotice notice)
    {
        InitializeComponent();
        VersionRange.Text = ReleaseNotesCatalog.FormatRange(notice);
        IReadOnlyList<ReleaseNotesEntry> entries = ReleaseNotesCatalog.ForRelease(notice);
        ReleaseSections.ItemsSource = entries;
        NoNotes.Visibility = entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Done_Click(object sender, RoutedEventArgs e) => Close();
}
