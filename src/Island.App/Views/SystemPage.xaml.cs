using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Island.App.ViewModels;
using Island.Core.Settings;

namespace Island.App.Views;

public partial class SystemPage : UserControl
{
    private readonly SystemViewModel model;
    private readonly Action<AppSettings> saveSettings;
    private readonly Func<AppSettings> settings;

    public SystemPage(SystemViewModel model, Action<AppSettings> saveSettings, Func<AppSettings> settings)
    {
        InitializeComponent();
        this.model = model;
        this.saveSettings = saveSettings;
        this.settings = settings;
        DataContext = model;

        model.PropertyChanged += OnModelPropertyChanged;
        UpdateCpuRing(model.CpuPercent);
    }

    private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(SystemViewModel.CpuPercent))
        {
            UpdateCpuRing(model.CpuPercent);
        }
    }

    private void UpdateCpuRing(double percent)
    {
        if (percent <= 0)
        {
            CpuRingPath.Data = null;
            return;
        }

        double p = Math.Clamp(percent, 0, 99.99) / 100.0;
        double angle = p * 2 * Math.PI;

        double cx = 20;
        double cy = 20;
        double r = 16;

        double startX = cx;
        double startY = cy - r;

        double endX = cx + r * Math.Sin(angle);
        double endY = cy - r * Math.Cos(angle);

        bool isLarge = p > 0.5;

        var figure = new PathFigure
        {
            StartPoint = new Point(startX, startY),
            IsClosed = false
        };
        figure.Segments.Add(new ArcSegment
        {
            Point = new Point(endX, endY),
            Size = new Size(r, r),
            IsLargeArc = isLarge,
            SweepDirection = SweepDirection.Clockwise
        });

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        CpuRingPath.Data = geometry;
    }

    private void SortCpuClick(object sender, RoutedEventArgs e) =>
        model.SetProcessSort("Cpu", saveSettings);

    private void SortMemoryClick(object sender, RoutedEventArgs e) =>
        model.SetProcessSort("Memory", saveSettings);
}
