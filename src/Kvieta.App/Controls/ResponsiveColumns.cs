using System.Windows;
using System.Windows.Controls;
using Panel = System.Windows.Controls.Panel;
using Size = System.Windows.Size;

namespace Kvieta.App.Controls;

/// <summary>Reflows cards using the available (already scaled) viewport width.</summary>
public sealed class ResponsiveColumns : Panel
{
    public static readonly DependencyProperty MinimumColumnWidthProperty = DependencyProperty.Register(
        nameof(MinimumColumnWidth), typeof(double), typeof(ResponsiveColumns),
        new FrameworkPropertyMetadata(240d, FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static readonly DependencyProperty MaximumColumnsProperty = DependencyProperty.Register(
        nameof(MaximumColumns), typeof(int), typeof(ResponsiveColumns),
        new FrameworkPropertyMetadata(3, FrameworkPropertyMetadataOptions.AffectsMeasure));
    public double MinimumColumnWidth { get => (double)GetValue(MinimumColumnWidthProperty); set => SetValue(MinimumColumnWidthProperty, value); }
    public int MaximumColumns { get => (int)GetValue(MaximumColumnsProperty); set => SetValue(MaximumColumnsProperty, value); }
    private const double Gap = 12;
    private UIElement[] VisibleChildren => InternalChildren.Cast<UIElement>().Where(c => c.Visibility != Visibility.Collapsed).ToArray();
    private int Columns(double width) => Math.Max(1, Math.Min(Math.Max(1, MaximumColumns),
        double.IsInfinity(width) ? 1 : (int)((width + Gap) / (Math.Max(1, MinimumColumnWidth) + Gap))));

    protected override Size MeasureOverride(Size availableSize)
    {
        var children = VisibleChildren;
        int columns = Columns(availableSize.Width);
        double width = double.IsInfinity(availableSize.Width) ? MinimumColumnWidth : availableSize.Width;
        double cell = Math.Max(0, (width - Gap * (columns - 1)) / columns);
        foreach (UIElement child in children) child.Measure(new Size(cell, double.PositiveInfinity));
        double height = 0;
        for (int i = 0; i < children.Length; i += columns)
            height += children.Skip(i).Take(columns).Max(c => c.DesiredSize.Height) + (i == 0 ? 0 : Gap);
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var children = VisibleChildren;
        int columns = Columns(finalSize.Width);
        double cell = Math.Max(0, (finalSize.Width - Gap * (columns - 1)) / columns);
        double y = 0;
        for (int i = 0; i < children.Length; i += columns)
        {
            double height = children.Skip(i).Take(columns).Max(c => c.DesiredSize.Height);
            for (int j = 0; j < columns && i + j < children.Length; j++)
                children[i + j].Arrange(new Rect(j * (cell + Gap), y, cell, height));
            y += height + Gap;
        }
        return finalSize;
    }
}
