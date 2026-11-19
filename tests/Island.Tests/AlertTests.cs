using System.Windows.Threading;
using Island.App.ViewModels;
using Island.Core.Alerts;
using Island.Core.Settings;
using Xunit;

namespace Island.Tests;

public sealed class AlertTests
{
    [Fact]
    public void AlertQueueEnqueuesAndProcessesFifo()
    {
        using var queue = new AlertQueue(TimeSpan.FromSeconds(5));
        Assert.Null(queue.ActiveAlert);
        Assert.Equal(0, queue.PendingCount);

        var alert1 = new IslandAlert("1", AlertType.Custom, "Alert 1", "\uE700");
        var alert2 = new IslandAlert("2", AlertType.Custom, "Alert 2", "\uE701");

        queue.Enqueue(alert1);
        Assert.Same(alert1, queue.ActiveAlert);
        Assert.Equal(0, queue.PendingCount);

        queue.Enqueue(alert2);
        Assert.Same(alert1, queue.ActiveAlert);
        Assert.Equal(1, queue.PendingCount);

        queue.DismissActive();
        Assert.Same(alert2, queue.ActiveAlert);
        Assert.Equal(0, queue.PendingCount);

        queue.DismissActive();
        Assert.Null(queue.ActiveAlert);
        Assert.Equal(0, queue.PendingCount);
    }

    [Fact]
    public void ContinuousOsdAlertsUpdateInPlace()
    {
        using var queue = new AlertQueue(TimeSpan.FromSeconds(5));

        var vol1 = new IslandAlert("vol", AlertType.Volume, "Volume 50%", "\uE767", Progress: 0.5);
        queue.Enqueue(vol1);
        Assert.Same(vol1, queue.ActiveAlert);
        Assert.Equal(0, queue.PendingCount);

        // Immediate subsequent volume change updates active alert in-place without queueing
        var vol2 = new IslandAlert("vol", AlertType.Volume, "Volume 60%", "\uE767", Progress: 0.6);
        queue.Enqueue(vol2);
        Assert.Same(vol2, queue.ActiveAlert);
        Assert.Equal(0, queue.PendingCount);

        var vol3 = new IslandAlert("vol", AlertType.Volume, "Volume 70%", "\uE767", Progress: 0.7);
        queue.Enqueue(vol3);
        Assert.Same(vol3, queue.ActiveAlert);
        Assert.Equal(0, queue.PendingCount);
    }

    [Fact]
    public void CriticalAlertPreemptsNormalAlert()
    {
        using var queue = new AlertQueue(TimeSpan.FromSeconds(5));

        var normal = new IslandAlert("normal", AlertType.Custom, "Normal Alert", "\uE700", AlertLevel.Info);
        queue.Enqueue(normal);
        Assert.Same(normal, queue.ActiveAlert);

        // Critical alert arrives: should preempt normal alert immediately
        var critical = new IslandAlert("crit", AlertType.BatteryCritical, "Battery Critical", "\uE83F", AlertLevel.Critical);
        queue.Enqueue(critical);

        Assert.Same(critical, queue.ActiveAlert);
        Assert.Equal(1, queue.PendingCount);

        // Dismissing critical alert resumes normal alert
        queue.DismissActive();
        Assert.Same(normal, queue.ActiveAlert);
        Assert.Equal(0, queue.PendingCount);
    }

    [Fact]
    public void AlertQueueClearResetsEverything()
    {
        using var queue = new AlertQueue(TimeSpan.FromSeconds(5));
        queue.Enqueue(new IslandAlert("1", AlertType.Custom, "Alert 1", "\uE700"));
        queue.Enqueue(new IslandAlert("2", AlertType.Custom, "Alert 2", "\uE701"));

        Assert.NotNull(queue.ActiveAlert);
        Assert.Equal(1, queue.PendingCount);

        queue.Clear();
        Assert.Null(queue.ActiveAlert);
        Assert.Equal(0, queue.PendingCount);
    }

    [Fact]
    public void AlertsViewModelReflectsActiveAlertProperties()
    {
        using var queue = new AlertQueue(TimeSpan.FromSeconds(5));
        var settings = new AppSettings();
        using var vm = new AlertsViewModel(Dispatcher.CurrentDispatcher, queue, settings);

        Assert.False(vm.HasActiveAlert);
        Assert.Equal("", vm.AlertTitle);

        var alert = new IslandAlert("crit", AlertType.BatteryCritical, "Battery 10%", "\uE83F", AlertLevel.Critical, Progress: 0.1);
        queue.Enqueue(alert);

        Assert.True(vm.HasActiveAlert);
        Assert.Equal("Battery 10%", vm.AlertTitle);
        Assert.Equal("\uE83F", vm.AlertGlyph);
        Assert.True(vm.HasProgress);
        Assert.Equal(0.1, vm.AlertProgress);
        Assert.True(vm.IsCritical);
        Assert.False(vm.IsWarning);
    }

    [Fact]
    public void AppSettingsNormalizesAlertThresholds()
    {
        var settings = new AppSettings
        {
            AlertBatteryLowThreshold = 999,
            AlertBatteryCriticalThreshold = -10,
            AlertTemperatureThreshold = 200,
            AlertDurationSeconds = 0
        };

        var norm = settings.Normalize();
        Assert.Equal(50, norm.AlertBatteryLowThreshold);
        Assert.Equal(1, norm.AlertBatteryCriticalThreshold);
        Assert.Equal(115, norm.AlertTemperatureThreshold);
        Assert.Equal(1, norm.AlertDurationSeconds);
    }
}
