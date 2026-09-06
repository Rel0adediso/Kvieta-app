using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Kvieta.App.Services;
using Kvieta.Core.Models;
using Kvieta.Core.Services;
using Size = System.Windows.Size;

namespace Kvieta.App;

public partial class MainWindow
{
    private readonly JsonDisplayPreferencesStore _displayPreferencesStore = new();
    private bool _loadingDisplayPreferences;
    private sealed record GuideStep(string Title, string Text, int Page, FrameworkElement Target);
    private readonly List<GuideStep> _guideSteps = [];
    private int _guideIndex;
    private int _guideRenderRevision;
    private TaskCompletionSource? _guideCompletion;
    private IInputElement? _guidePreviousFocus;
    private bool _guideRhythmWasExpanded;
    private FrameworkElement? _guidePageContent;
    private Thickness _guidePageMargin;
    private bool IsGuideOpen => TourOverlay?.Visibility == Visibility.Visible;

    private async Task ShowFirstRunGuideAsync()
    {
        try
        {
            if (await _displayPreferencesStore.TryClaimGuideAsync())
            {
                QuickGuide_Click(this, new RoutedEventArgs());
                if (_guideCompletion is not null) await _guideCompletion.Task;
            }
        }
        catch (Exception exception) { _viewModel.StatusMessage = exception.Message; }
    }

    private async Task LoadDisplayPreferencesAsync()
    {
        _loadingDisplayPreferences = true;
        try
        {
            DisplayPreferences preferences = await _displayPreferencesStore.LoadAsync();
            foreach (ComboBoxItem item in ZoomSelector.Items)
                if (int.TryParse(item.Tag?.ToString(), out int zoom) && zoom == preferences.ZoomPercent)
                    ZoomSelector.SelectedItem = item;
            ApplyDisplayZoom(preferences.ZoomPercent);
        }
        catch { ApplyDisplayZoom(100); }
        finally { _loadingDisplayPreferences = false; }
    }

    private void ApplyDisplayZoom(int zoom)
    {
        ContentScale.ScaleX = ContentScale.ScaleY = zoom / 100d;
        if (ActualWidth / ContentScale.ScaleX < 960)
        {
            _viewModel.IsSidebarExpanded = false;
            SidebarColumn.Width = new GridLength(64);
            UpdateSidebarVisuals();
        }
        if (IsGuideOpen) Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(PositionGuide));
    }

    private async void Zoom_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingDisplayPreferences || _isInitializing || ContentScale is null ||
            ZoomSelector.SelectedItem is not ComboBoxItem item ||
            !int.TryParse(item.Tag?.ToString(), out int zoom)) return;
        ApplyDisplayZoom(zoom);
        try { await _displayPreferencesStore.SaveAsync(zoom); }
        catch (Exception exception) { _viewModel.StatusMessage = exception.Message; }
    }

    private void QuickGuide_Click(object sender, RoutedEventArgs e)
    {
        if (IsGuideOpen) return;
        bool english = LocalizationService.CurrentLanguage == LanguagePreference.English;
        string T(string tr, string en) => english ? en : tr;
        _guideSteps.Clear();
        _guideSteps.Add(new(T("Bugünün zamanı", "Your time today"),
            T("Kalan süre ve bugünkü kullanım burada. Yanında şu anki durumunu ve sıradaki planını görebilirsin.",
              "Find your remaining time and today's usage here, alongside your current status and next plan."), 0, TodayTimeCard));
        if (_viewModel.IsPersonalMode)
            _guideSteps.Add(new(T("Bir odak oturumu başlat", "Start a focus session"),
                T("Hazır sürelerden birini seç veya kendi süreni belirle. Son kullandığın süreyi de tekrar başlatabilirsin.",
                  "Choose a preset or your own duration. You can also repeat your last focus session."), 0, QuickFocusCard));
        if (_viewModel.HasScheduledPlan)
            _guideSteps.Add(new(T("Haftanı planla", "Plan your week"),
                T("Her gün için saat aralığını ve süre limitini seç. Değişikliklerini üstteki Kaydet ile uygula.",
                  "Choose allowed hours and a time limit for each day. Apply changes with Save at the top."), 1, MainTabs));
        _guideSteps.Add(new(T("Uygulamalarını tanı", "Explore your applications"),
            T("En çok kullandığın uygulamalar artık burada. Aşağıda bugünkü kullanımını kategoriler halinde inceleyebilirsin.",
              "Your most-used applications live here. Explore today's usage grouped into categories below."), 2, AppsUsageCard));
        if (_viewModel.HasRestrictions)
            _guideSteps.Add(new(T("Uygulama kuralları", "Application rules"),
                T("Kullanım kartından kural oluşturabilir veya .exe ekleyebilirsin. Limit ve davranış seçimini Kaydet ile uygula.",
                  "Create a rule from a usage card or add an .exe. Choose its limit and behavior, then Save."), 2, AppRulesCard));
        _guideSteps.Add(new(T("Ritmini incele", "Explore your rhythm"),
            T("Bu bölüm açılıp kapanır. Haftalık eğilimlerini, hedeflerini ve odak süreni burada görebilirsin; aşağıdaki günlerden birini seçerek geçmişini incele.",
              "Expand this section for weekly trends, goals and focus time. Select a day below to explore your history."), 3, RhythmExpander));
        _guideSteps.Add(new(T("Kendine göre ayarla", "Make it yours"),
            T("Dil, tema ve arayüz boyutu bu bölümde. Diğer ayarlar koruma, gizlilik ve bakım başlıklarında. Öğreticiyi ? düğmesiyle yeniden açabilirsin.",
              "Language, theme and interface size live here. Other settings are grouped under protection, privacy and maintenance. Reopen this tour with ?."), 4, AppearanceSection));
        _guideIndex = 0;
        _guidePreviousFocus = Keyboard.FocusedElement;
        _guideRhythmWasExpanded = RhythmExpander.IsExpanded;
        _guideCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TourOverlay.Visibility = Visibility.Visible;
        KeyboardNavigation.SetTabNavigation(TourOverlay, KeyboardNavigationMode.Cycle);
        RenderGuideStep();
    }

    private void RenderGuideStep()
    {
        RestoreGuidePageMargin();
        int revision = ++_guideRenderRevision;
        GuideStep step = _guideSteps[_guideIndex];
        _viewModel.SelectedPageIndex = step.Page;
        if (step.Page == 3) RhythmExpander.IsExpanded = true;
        if (step.Page == 4) AppearanceSection.IsExpanded = true;
        TourTitle.Text = step.Title;
        TourDescription.Text = step.Text;
        TourProgress.Text = $"{_guideIndex + 1:00} / {_guideSteps.Count:00}";
        TourBack.Content = LocalizationService.CurrentLanguage == LanguagePreference.English ? "Back" : "Geri";
        TourBack.IsEnabled = _guideIndex > 0;
        TourNext.Content = LocalizationService.CurrentLanguage == LanguagePreference.English
            ? _guideIndex == _guideSteps.Count - 1 ? "Finish" : "Next"
            : _guideIndex == _guideSteps.Count - 1 ? "Bitir" : "İleri";
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            if (!IsGuideOpen || revision != _guideRenderRevision) return;
            if (MainTabs.SelectedItem is TabItem { Content: ScrollViewer viewer } && viewer.Content is FrameworkElement content)
            {
                _guidePageContent = content;
                _guidePageMargin = content.Margin;
                // Reserve space so even the final section can be presented near the top.
                content.Margin = new Thickness(content.Margin.Left, content.Margin.Top, content.Margin.Right,
                    content.Margin.Bottom + viewer.ActualHeight);
                TourHost.UpdateLayout();
                if (step.Target != MainTabs)
                    viewer.ScrollToVerticalOffset(viewer.VerticalOffset + step.Target.TransformToAncestor(viewer).Transform(new System.Windows.Point()).Y - 16);
                else viewer.ScrollToTop();
            }
            TourHost.UpdateLayout();
            PositionGuide();
            TourNext.Focus();
        }));
    }

    private void PositionGuide()
    {
        if (!IsGuideOpen || _guideSteps.Count == 0 || TourHost.ActualWidth < 1) return;
        FrameworkElement target = _guideSteps[_guideIndex].Target;
        if (target.ActualWidth < 1) return;
        Rect bounds = target.TransformToAncestor(TourHost).TransformBounds(new Rect(target.RenderSize));
        Rect viewport = MainTabs.TransformToAncestor(TourHost).TransformBounds(new Rect(MainTabs.RenderSize));
        bounds.Intersect(viewport);
        if (bounds.IsEmpty) return;
        double w = TourHost.ActualWidth, h = TourHost.ActualHeight;
        TourCard.Width = Math.Min(350, w - 32);
        TourCard.Measure(new Size(TourCard.Width, double.PositiveInfinity));
        double cardHeight = TourCard.DesiredSize.Height;
        // Keep complete cards visible when there is room; long sections use their heading.
        if (bounds.Height + cardHeight + 40 > h - bounds.Top && bounds.Top < cardHeight + 76)
            bounds.Height = Math.Min(bounds.Height, 150);
        bounds.Inflate(4, 4);
        TourShade.Data = new CombinedGeometry(GeometryCombineMode.Exclude,
            new RectangleGeometry(new Rect(0, 0, w, h)), new RectangleGeometry(bounds, 12, 12));
        Canvas.SetLeft(TourHighlight, bounds.Left); Canvas.SetTop(TourHighlight, bounds.Top);
        TourHighlight.Width = bounds.Width; TourHighlight.Height = bounds.Height;
        double top = bounds.Bottom + 14;
        if (top + cardHeight > h - 16) top = Math.Max(62, bounds.Top - cardHeight - 14);
        Canvas.SetLeft(TourCard, Math.Clamp(bounds.Left, 16, Math.Max(16, w - TourCard.Width - 16)));
        Canvas.SetTop(TourCard, Math.Clamp(top, 62, Math.Max(62, h - cardHeight - 16)));
    }

    private void TourOverlay_SizeChanged(object sender, SizeChangedEventArgs e) => PositionGuide();
    private void TourBack_Click(object sender, RoutedEventArgs e) { if (_guideIndex > 0) { _guideIndex--; RenderGuideStep(); } }
    private void TourNext_Click(object sender, RoutedEventArgs e)
    {
        if (_guideIndex == _guideSteps.Count - 1) EndGuide();
        else { _guideIndex++; RenderGuideStep(); }
    }
    private void TourSkip_Click(object sender, RoutedEventArgs e) => EndGuide();
    private void EndGuide()
    {
        if (!IsGuideOpen) return;
        TourOverlay.Visibility = Visibility.Collapsed;
        _guideRenderRevision++;
        RestoreGuidePageMargin();
        RhythmExpander.IsExpanded = _guideRhythmWasExpanded;
        _guideCompletion?.TrySetResult();
        if (_guidePreviousFocus is UIElement element && element.IsVisible) element.Focus();
        else MainTabs.Focus();
    }

    private void RestoreGuidePageMargin()
    {
        if (_guidePageContent is not null) _guidePageContent.Margin = _guidePageMargin;
        _guidePageContent = null;
    }
}
