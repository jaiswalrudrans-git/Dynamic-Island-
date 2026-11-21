using System.IO;
using Island.Core.Calendar;
using Windows.ApplicationModel.Appointments;

namespace Island.App.Features.CalendarWeather.Services;

public sealed class WindowsCalendarService
{
    private readonly Action<string>? log;

    public WindowsCalendarService(Action<string>? log = null)
    {
        this.log = log;
    }

    public async Task<IReadOnlyList<CalendarEvent>> GetAppointmentsAsync(DateTime start, TimeSpan duration)
    {
        var list = new List<CalendarEvent>();
        try
        {
            var store = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AllCalendarsReadOnly);
            if (store == null) return list;

            var startOffset = new DateTimeOffset(start);
            var appointments = await store.FindAppointmentsAsync(startOffset, duration);
            if (appointments == null) return list;

            foreach (var appt in appointments)
            {
                var apptStart = appt.StartTime.LocalDateTime;
                var apptEnd = apptStart.Add(appt.Duration);
                string title = string.IsNullOrWhiteSpace(appt.Subject) ? "Untitled Event" : appt.Subject;

                list.Add(new CalendarEvent(
                    Id: "win_" + (string.IsNullOrWhiteSpace(appt.LocalId) ? Guid.NewGuid().ToString("N") : appt.LocalId),
                    Title: title,
                    Start: apptStart,
                    End: apptEnd,
                    Description: appt.Details,
                    Location: appt.Location,
                    IsAllDay: appt.AllDay,
                    ReminderMinutes: (int)appt.Reminder.GetValueOrDefault(TimeSpan.Zero).TotalMinutes,
                    Source: "Laptop Calendar"
                ));
            }
        }
        catch (Exception ex)
        {
            log?.Invoke($"Windows Calendar query failed: {ex.Message}");
        }

        return list;
    }

    public void OpenLaptopCalendar()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-calendar:") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            log?.Invoke($"Could not launch ms-calendar: {ex.Message}");
        }
    }

    public void AddEventToLaptopCalendar(CalendarEvent ev)
    {
        try
        {
            string tempIcs = Path.Combine(Path.GetTempPath(), $"island_event_{Guid.NewGuid():N}.ics");
            string icsContent =
                "BEGIN:VCALENDAR\r\n" +
                "VERSION:2.0\r\n" +
                "PRODID:-//Island//Calendar 1.0//EN\r\n" +
                "BEGIN:VEVENT\r\n" +
                $"UID:{Guid.NewGuid():N}@island\r\n" +
                $"DTSTAMP:{DateTime.UtcNow:yyyyMMdd\\THHmmss\\Z}\r\n" +
                $"DTSTART:{ev.Start.ToUniversalTime():yyyyMMdd\\THHmmss\\Z}\r\n" +
                $"DTEND:{(ev.End ?? ev.Start.AddHours(1)).ToUniversalTime():yyyyMMdd\\THHmmss\\Z}\r\n" +
                $"SUMMARY:{ev.Title.Replace(",", "\\,").Replace(";", "\\;")}\r\n" +
                (string.IsNullOrWhiteSpace(ev.Location) ? "" : $"LOCATION:{ev.Location.Replace(",", "\\,").Replace(";", "\\;")}\r\n") +
                (string.IsNullOrWhiteSpace(ev.Description) ? "" : $"DESCRIPTION:{ev.Description.Replace(",", "\\,").Replace(";", "\\;")}\r\n") +
                "END:VEVENT\r\n" +
                "END:VCALENDAR\r\n";

            File.WriteAllText(tempIcs, icsContent);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(tempIcs) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            log?.Invoke($"Failed to open event in laptop calendar: {ex.Message}");
            OpenLaptopCalendar();
        }
    }
}
