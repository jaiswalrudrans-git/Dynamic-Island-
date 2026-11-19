using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Island.Core.Settings;

namespace Island.App.ViewModels;

public sealed class ClockViewModel : ObservableObject
{
    private string time = "";
    public string Time { get => time; private set => SetProperty(ref time, value); }

    public TimeSpan Refresh(DateTime now, AppSettings settings)
    {
        var hour = settings.Use24HourClock ? "HH" : "hh";
        Time = now.ToString(hour + (settings.ShowSeconds ? ":mm:ss" : ":mm"), CultureInfo.InvariantCulture);
        long period = settings.ShowSeconds ? TimeSpan.TicksPerSecond : TimeSpan.TicksPerMinute;
        return TimeSpan.FromTicks(period - now.Ticks % period);
    }
}
