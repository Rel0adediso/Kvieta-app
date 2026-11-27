using Kvieta.App.Services;
using Kvieta.Core.Services;
using Kvieta.Core.Models;

internal static class WhatsNewChecks
{
    public static void RunUi()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var app = new Kvieta.App.App();
                app.InitializeComponent();
                app.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
                var notice = new ReleaseNotice(new("v1.0.0", "7.0.1"), new("Alpha 6.5", "6.5.0"), true);
                foreach (LanguagePreference language in new[] { LanguagePreference.Turkish, LanguagePreference.English })
                {
                    LocalizationService.SetLanguage(app, language);
                    foreach (string theme in new[] { "Dark", "Light" })
                    {
                        app.Resources.MergedDictionaries[0] = new System.Windows.ResourceDictionary
                        {
                            Source = new Uri($"/Kvieta.Presentation;component/Themes/{theme}Theme.xaml", UriKind.Relative)
                        };
                        var window = new Kvieta.App.WhatsNewWindow(notice);
                        var content = (System.Windows.FrameworkElement)window.Content;
                        content.Measure(new System.Windows.Size(380, 310));
                        content.Arrange(new System.Windows.Rect(0, 0, 380, 310));
                        content.UpdateLayout();
                        if (window.FindName("ReleaseSections") is not System.Windows.Controls.ItemsControl { HasItems: true } ||
                            window.FindName("VersionRange") is not System.Windows.Controls.TextBlock { Text: "Alpha 6.5 → v1.0.0" })
                            throw new InvalidOperationException("Release notes window did not render its content.");
                        var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(380, 310, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        bitmap.Render(content);
                        window.Close();
                    }
                }
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(30))) throw new TimeoutException("What's new window initialization hung.");
        if (failure != null) throw new InvalidOperationException("What's new window render failed.", failure);
        Console.WriteLine("What's new window rendered in both languages and themes.");
    }

    public static async Task RunAsync(string directory, Action<bool, string> check)
    {
        var store = new JsonDisplayPreferencesStore(Path.Combine(directory, "whats-new.json"));
        var first = new ReleaseIdentity("v1.0.1", "7.0.41");
        var second = new ReleaseIdentity("v1.0.3", "7.0.43");
        var third = new ReleaseIdentity("v1.0.5", "7.0.45");
        check(!(await store.ObserveReleaseAsync(first)).Unread, "Fresh installation reported an update.");
        var notice = await store.ObserveReleaseAsync(second);
        check(notice.Unread && notice.Previous == first, "Real upgrade was not noticed.");
        notice = await new JsonDisplayPreferencesStore(Path.Combine(directory, "whats-new.json")).ObserveReleaseAsync(third);
        check(notice.Unread && notice.Previous == first, "Skipped releases were lost.");
        var entries = new[] {
            new ReleaseNotesEntry("v1.0.2", ["Second"], ["İkinci"]),
            new ReleaseNotesEntry("v1.0.6", ["Future"], ["Gelecek"]),
            new ReleaseNotesEntry("v1.0.1", ["Old"], ["Eski"]),
            new ReleaseNotesEntry("v1.0.5", ["Fifth"], ["Beşinci"]),
            new ReleaseNotesEntry("v1.0.3", ["Third"], ["Üçüncü"])
        };
        check(ReleaseNotesCatalog.Select(entries, notice).Select(entry => entry.Version).SequenceEqual(["v1.0.5", "v1.0.3", "v1.0.2"]),
            "Upgrade changelog included old/future versions or was out of order.");
        check(ReleaseNotesCatalog.FormatRange(notice) == "v1.0.1 → v1.0.5", "Upgrade range is incorrect.");
        await store.AcknowledgeReleaseAsync(third);
        var read = await store.ObserveReleaseAsync(third);
        check(!read.Unread && read.Previous == first, "Acknowledged release did not remain accessible without the banner.");
        check(!(await store.ObserveReleaseAsync(second)).Unread, "Rollback reported a new update.");
        var fourth = new ReleaseIdentity("v1.0.5", "7.0.46");
        var buildNotice = await store.ObserveReleaseAsync(fourth);
        check(buildNotice.Unread && buildNotice.Previous == third, "Same-label stable build was not noticed.");
        check(ReleaseNotesCatalog.Select([new ReleaseNotesEntry("v1.0.5", ["Build"], ["Derleme"], "7.0.46")], buildNotice).Count == 1,
            "Same-label build notes were not shown.");
        var migration = new JsonDisplayPreferencesStore(Path.Combine(directory, "whats-new-migration.json"));
        var alpha = new ReleaseIdentity("Alpha 6.5", "6.5.0");
        await migration.RememberPreviousReleaseAsync(alpha);
        var migrated = await migration.ObserveReleaseAsync(new ReleaseIdentity("v1.0.0", "7.0.1"));
        check(migrated.Unread && migrated.Previous == alpha, "First upgrade from a legacy installer was not noticed.");
        check(ReleaseNotesCatalog.ForRelease(migrated).Count > 0, "Embedded V1 release notes are missing.");
        var fromPreview = new ReleaseNotice(new("v1.0.0", "7.0.1"), new("v1.0.0", "7.0.0"), true);
        check(ReleaseNotesCatalog.ForRelease(fromPreview).Count > 0, "The V1 preview upgrade did not show release notes.");
    }
}
