using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Island.Core.Settings;

namespace Island.App.ViewModels;

public sealed class ClockViewModel : ObservableObject
{
    private string time = "";
    public string Time { get => time; private set => SetProperty(ref time, value); }
    private string date = "";
    public string Date { get => date; private set => SetProperty(ref date, value); }

    public TimeSpan Refresh(DateTime now, AppSettings settings)
    {
        var hour = settings.Use24HourClock ? "HH" : "hh";
        Time = now.ToString(hour + (settings.ShowSeconds ? ":mm:ss" : ":mm"), CultureInfo.InvariantCulture);
        Date = now.ToString("ddd, MMM d", CultureInfo.CurrentCulture);
        long period = settings.ShowSeconds ? TimeSpan.TicksPerSecond : TimeSpan.TicksPerMinute;
        return TimeSpan.FromTicks(period - now.Ticks % period);
    }
}
