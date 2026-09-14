using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Kvieta.App.Controls;
using Kvieta.App.ViewModels;
using Kvieta.Core.Models;
using Kvieta.Core.Services;

internal static class TodayDashboardPreview
{
    public static void Render()
    {
        Exception? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                string output = Path.GetFullPath("artifacts/today-preview");
                Directory.CreateDirectory(output);
                string fixture = Path.Combine(Path.GetTempPath(), "Kvieta-TodayPreview-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(fixture);
                var settingsStore = new JsonSettingsStore(Path.Combine(fixture, "settings.json"));
                var usageStore = new JsonUsageStore(Path.Combine(fixture, "usage.json"));
                var settings = new ControlSettings { SetupCompleted = true, Mode = UsageMode.Personal, Language = LanguagePreference.Turkish };
                settingsStore.SaveAsync(settings).GetAwaiter().GetResult();
                var ledger = new UsageLedger
                {
                    LocalDay = DateOnly.FromDateTime(DateTime.Today),
                    UsedSeconds = 8640,
                    AwarenessUsedSeconds = 10200,
                    AwarenessMeasurementAvailable = true,
                    ForegroundAppUsedSeconds = new(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Code.exe"] = 4800,
                        ["chrome.exe"] = 3180,
                        ["Spotify.exe"] = 1680,
                        ["explorer.exe"] = 540
                    },
                    AwarenessHourlyUsedSeconds = new() { [8] = 900, [9] = 1500, [10] = 2880, [11] = 1200, [12] = 180, [13] = 1200, [14] = 1920, [15] = 420 }
                };
                usageStore.SaveAsync(ledger).GetAwaiter().GetResult();
                var application = new Kvieta.App.App();
                application.InitializeComponent();
                var viewModel = new MainViewModel(settingsStore, usageStore);
                viewModel.InitializeAsync().GetAwaiter().GetResult();
                foreach (string theme in new[] { "Light", "Dark" })
                {
                    application.Resources.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri($"/Kvieta;component/Themes/{theme}Theme.xaml", UriKind.Relative)
                    });
                    foreach (int width in new[] { 980, 380 })
                    {
                        var dashboard = new TodayDashboard { DataContext = viewModel };
                        var surface = new Border { Padding = new Thickness(24), Child = dashboard };
                        surface.SetResourceReference(Border.BackgroundProperty, "BackgroundBrush");
                        surface.Measure(new Size(width, double.PositiveInfinity));
                        int height = (int)Math.Ceiling(surface.DesiredSize.Height);
                        surface.Arrange(new Rect(0, 0, width, height));
                        surface.UpdateLayout();
                        surface.InvalidateMeasure();
                        surface.Measure(new Size(width, double.PositiveInfinity));
                        height = (int)Math.Ceiling(surface.DesiredSize.Height);
                        surface.Arrange(new Rect(0, 0, width, height));
                        surface.UpdateLayout();
                        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                        bitmap.Render(surface);
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmap));
                        using var file = File.Create(Path.Combine(output, $"today-{theme.ToLowerInvariant()}-{width}.png"));
                        encoder.Save(file);
                    }
                }
                application.Shutdown();
                Console.WriteLine($"Today previews: {output}");
            }
            catch (Exception exception) { failure = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null) throw new InvalidOperationException("Today preview failed.", failure);
    }
}
