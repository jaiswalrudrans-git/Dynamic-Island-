using System.Media;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Island.Core.Alerts;
using Island.Core.Settings;

namespace Island.App.ViewModels;

public sealed class AlertsViewModel : ObservableObject, IDisposable
{
    private readonly Dispatcher dispatcher;
    private readonly AlertQueue queue;
    private readonly Action<string>? log;
    private AppSettings settings;
    private bool disposed;

    public bool HasActiveAlert => queue.ActiveAlert != null;
    public string AlertTitle => queue.ActiveAlert?.Title ?? "";
    public string AlertGlyph => queue.ActiveAlert?.Glyph ?? "";
    public double AlertProgress => queue.ActiveAlert?.Progress.HasValue == true ? Math.Clamp(queue.ActiveAlert.Progress.Value, 0, 1) : 0;
    public bool HasProgress => queue.ActiveAlert?.Progress.HasValue == true;
    public bool IsWarning => queue.ActiveAlert?.Level == AlertLevel.Warning;
    public bool IsCritical => queue.ActiveAlert?.Level == AlertLevel.Critical;

    public Brush AlertAccentBrush
    {
        get
        {
            if (IsCritical) return new SolidColorBrush(Color.FromRgb(255, 99, 132));
            if (IsWarning) return new SolidColorBrush(Color.FromRgb(255, 193, 7));
            return new SolidColorBrush(Color.FromRgb(155, 197, 255));
        }
    }

    public event Action? ActiveAlertChanged;

    public AlertsViewModel(Dispatcher dispatcher, AlertQueue queue, AppSettings settings, Action<string>? log = null)
    {
        this.dispatcher = dispatcher;
        this.queue = queue;
        this.settings = settings;
        this.log = log;

        queue.ActiveAlertChanged += OnActiveAlertChanged;
    }

    public void Configure(AppSettings newSettings)
    {
        settings = newSettings;
    }

    private void OnActiveAlertChanged(IslandAlert? alert)
    {
        if (disposed) return;
        dispatcher.InvokeAsync(() =>
        {
            OnPropertyChanged(string.Empty);
            if (alert != null && alert.PlaySound && settings.AlertSoundEnabled)
            {
                try
                {
                    if (alert.Level == AlertLevel.Critical) SystemSounds.Exclamation.Play();
                    else SystemSounds.Asterisk.Play();
                }
                catch { }
            }
            ActiveAlertChanged?.Invoke();
        }, DispatcherPriority.Background);
    }

    public void DismissActive() => queue.DismissActive();

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        queue.ActiveAlertChanged -= OnActiveAlertChanged;
    }
}
