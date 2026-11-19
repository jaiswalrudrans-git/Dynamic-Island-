namespace Island.Core.Calendar;

public sealed record CalendarEvent(
    string Id,
    string Title,
    DateTime Start,
    DateTime? End = null,
    bool IsAllDay = false,
    string? Location = null,
    string? Description = null,
    string Source = "Local",
    int? ReminderMinutes = 15,
    bool HasFiredReminder = false
)
{
    public DateTime EffectiveEnd => End ?? (IsAllDay ? Start.Date.AddDays(1) : Start.AddHours(1));

    public bool OccursOn(DateTime date)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);
        return Start < dayEnd && EffectiveEnd > dayStart;
    }
}
