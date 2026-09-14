using System.Collections;
using System.Collections.Specialized;
using System.Windows;
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

        List<(long Seconds, Brush Brush)> segments = rows.Take(3)
            .Select(item => (item.UsedSeconds, item.FallbackBrush))
            .ToList();
        long remainder = total - segments.Sum(segment => segment.Seconds);
        if (remainder > 0)
        {
            segments.Add((remainder, TrackBrush));
        }

        double startAngle = -90;
        foreach ((long seconds, Brush brush) in segments)
        {
            double sweep = seconds * 360d / total;
            if (sweep >= 359.9)
            {
                drawingContext.DrawEllipse(null, new Pen(brush, thickness), center, radius, radius);
            }
            else
            {
                double gap = Math.Min(2.4, sweep * 0.16);
                DrawArc(drawingContext, center, radius, startAngle + gap / 2, Math.Max(0, sweep - gap), new Pen(brush, thickness));
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
        if (args.OldValue is INotifyCollectionChanged oldCollection)
        {
            oldCollection.CollectionChanged -= chart.CollectionChanged;
        }

        if (args.NewValue is INotifyCollectionChanged newCollection)
        {
            newCollection.CollectionChanged += chart.CollectionChanged;
        }

        chart.InvalidateVisual();
    }

    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs args) => InvalidateVisual();
}
