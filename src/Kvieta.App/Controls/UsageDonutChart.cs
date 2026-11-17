using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Kvieta.App.ViewModels;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace Kvieta.App.Controls;

public sealed class UsageDonutChart : FrameworkElement
{
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource),
        typeof(IEnumerable),
        typeof(UsageDonutChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, ItemsSourceChanged));

    public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(
        nameof(TrackBrush),
        typeof(Brush),
        typeof(UsageDonutChart),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
        nameof(StrokeThickness),
        typeof(double),
        typeof(UsageDonutChart),
        new FrameworkPropertyMetadata(16d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public Brush TrackBrush
    {
        get => (Brush)GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public UsageDonutChart()
    {
        Cursor = System.Windows.Input.Cursors.Hand;
    }

    protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Point pos = e.GetPosition(this);
        Point center = new(RenderSize.Width / 2, RenderSize.Height / 2);
        double dx = pos.X - center.X;
        double dy = pos.Y - center.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);
        double thickness = Math.Max(1, StrokeThickness);
        double radius = Math.Max(0, Math.Min(RenderSize.Width, RenderSize.Height) / 2 - thickness / 2);

        if (distance >= radius - thickness / 2 - 4 && distance <= radius + thickness / 2 + 8)
        {
            double angle = Math.Atan2(dy, dx) * 180 / Math.PI;
            double normalizedAngle = (angle + 90) % 360;
            if (normalizedAngle < 0) normalizedAngle += 360;

            int targetIndex = FindSegmentIndex(normalizedAngle);
            SetHighlightedIndex(targetIndex);
        }
        else
        {
            SetHighlightedIndex(-1);
        }
    }

    protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        SetHighlightedIndex(-1);
    }

    private int FindSegmentIndex(double normalizedAngle)
    {
        List<AppUsageHistoryRow> rows = ItemsSource?.OfType<AppUsageHistoryRow>()
            .Where(item => item.UsedSeconds > 0)
            .OrderByDescending(item => item.UsedSeconds)
            .Take(3)
            .ToList() ?? [];
        long total = (ItemsSource?.OfType<AppUsageHistoryRow>().Sum(i => i.UsedSeconds)) ?? 0;
        if (total <= 0) return -1;

        double currentAngle = 0;
        for (int i = 0; i < rows.Count; i++)
        {
            double sweep = rows[i].UsedSeconds * 360d / total;
            if (normalizedAngle >= currentAngle && normalizedAngle < currentAngle + sweep)
            {
                return i;
            }
            currentAngle += sweep;
        }
        return -1;
    }

    private void SetHighlightedIndex(int index)
    {
        List<AppUsageHistoryRow> rows = ItemsSource?.OfType<AppUsageHistoryRow>()
            .Where(item => item.UsedSeconds > 0)
            .OrderByDescending(item => item.UsedSeconds)
            .Take(3)
            .ToList() ?? [];

        for (int i = 0; i < rows.Count; i++)
        {
            rows[i].IsHighlighted = (i == index);
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        double thickness = Math.Max(1, StrokeThickness);
        Point center = new(RenderSize.Width / 2, RenderSize.Height / 2);
        double radius = Math.Max(0, Math.Min(RenderSize.Width, RenderSize.Height) / 2 - thickness / 2);
        if (radius <= 0)
        {
            return;
        }

        Pen track = new(TrackBrush, thickness);
        drawingContext.DrawEllipse(null, track, center, radius, radius);

        List<AppUsageHistoryRow> rows = ItemsSource?.OfType<AppUsageHistoryRow>()
            .Where(item => item.UsedSeconds > 0)
            .OrderByDescending(item => item.UsedSeconds)
            .ToList() ?? [];
        long total = rows.Sum(item => item.UsedSeconds);
        if (total <= 0)
        {
            return;
        }

        List<(long Seconds, Brush Brush, bool IsHighlighted)> segments = rows.Take(3)
            .Select(item => (item.UsedSeconds, item.FallbackBrush, item.IsHighlighted))
            .ToList();
        long remainder = total - segments.Sum(segment => segment.Seconds);
        if (remainder > 0)
        {
            segments.Add((remainder, TrackBrush, false));
        }

        bool anyHighlighted = segments.Any(s => s.IsHighlighted);
        double startAngle = -90;

        foreach ((long seconds, Brush brush, bool isHighlighted) in segments)
        {
            double sweep = seconds * 360d / total;
            double currentThickness = isHighlighted ? thickness + 4 : (anyHighlighted ? thickness - 1 : thickness);
            double currentOpacity = isHighlighted ? 1.0 : (anyHighlighted ? 0.38 : 1.0);

            Brush segmentBrush = brush.Clone();
            segmentBrush.Opacity = currentOpacity;
            Pen pen = new(segmentBrush, currentThickness);

            if (sweep >= 359.9)
            {
                drawingContext.DrawEllipse(null, pen, center, radius, radius);
            }
            else
            {
                double gap = Math.Min(2.4, sweep * 0.16);
                DrawArc(drawingContext, center, radius, startAngle + gap / 2, Math.Max(0, sweep - gap), pen);
            }

            startAngle += sweep;
        }
    }

    private static void DrawArc(DrawingContext drawingContext, Point center, double radius, double startAngle, double sweep, Pen pen)
    {
        if (sweep <= 0.01)
        {
            return;
        }

        Point start = PointOnCircle(center, radius, startAngle);
        Point end = PointOnCircle(center, radius, startAngle + sweep);
        StreamGeometry geometry = new();
        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(start, false, false);
            context.ArcTo(end, new Size(radius, radius), 0, sweep > 180, SweepDirection.Clockwise, true, false);
        }

        drawingContext.DrawGeometry(null, pen, geometry);
    }

    private static Point PointOnCircle(Point center, double radius, double angle)
    {
        double radians = angle * Math.PI / 180;
        return new Point(center.X + radius * Math.Cos(radians), center.Y + radius * Math.Sin(radians));
    }

    private static void ItemsSourceChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        UsageDonutChart chart = (UsageDonutChart)dependencyObject;
        if (args.OldValue is IEnumerable oldEnum)
        {
            chart.UnsubscribeItems(oldEnum);
        }

        if (args.NewValue is IEnumerable newEnum)
        {
            chart.SubscribeItems(newEnum);
        }

        chart.InvalidateVisual();
    }

    private void SubscribeItems(IEnumerable items)
    {
        if (items is INotifyCollectionChanged notify)
        {
            notify.CollectionChanged += CollectionChanged;
        }
        foreach (var item in items.OfType<INotifyPropertyChanged>())
        {
            item.PropertyChanged += ItemPropertyChanged;
        }
    }

    private void UnsubscribeItems(IEnumerable items)
    {
        if (items is INotifyCollectionChanged notify)
        {
            notify.CollectionChanged -= CollectionChanged;
        }
        foreach (var item in items.OfType<INotifyPropertyChanged>())
        {
            item.PropertyChanged -= ItemPropertyChanged;
        }
    }

    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (args.OldItems is not null)
        {
            foreach (var item in args.OldItems.OfType<INotifyPropertyChanged>())
            {
                item.PropertyChanged -= ItemPropertyChanged;
            }
        }
        if (args.NewItems is not null)
        {
            foreach (var item in args.NewItems.OfType<INotifyPropertyChanged>())
            {
                item.PropertyChanged += ItemPropertyChanged;
            }
        }
        InvalidateVisual();
    }

    private void ItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppUsageHistoryRow.IsHighlighted))
        {
            InvalidateVisual();
        }
    }
}
