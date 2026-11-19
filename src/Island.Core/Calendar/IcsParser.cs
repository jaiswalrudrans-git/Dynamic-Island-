using System.Globalization;
using System.Text.RegularExpressions;

namespace Island.Core.Calendar;

public static class IcsParser
{
    public static IReadOnlyList<CalendarEvent> Parse(string icsText, string sourceName = "ICS")
    {
        if (string.IsNullOrWhiteSpace(icsText)) return Array.Empty<CalendarEvent>();

        var events = new List<CalendarEvent>();
        var unfolded = UnfoldLines(icsText);

        bool inVEvent = false;
        string? uid = null;
        string? summary = null;
        DateTime? dtStart = null;
        DateTime? dtEnd = null;
        bool isAllDay = false;
        string? location = null;
        string? description = null;

        foreach (var rawLine in unfolded)
        {
            var line = rawLine.Trim();
            if (line.Equals("BEGIN:VEVENT", StringComparison.OrdinalIgnoreCase))
            {
                inVEvent = true;
                uid = null;
                summary = null;
                dtStart = null;
                dtEnd = null;
                isAllDay = false;
                location = null;
                description = null;
                continue;
            }

            if (line.Equals("END:VEVENT", StringComparison.OrdinalIgnoreCase))
            {
                if (inVEvent && dtStart.HasValue)
                {
                    events.Add(new CalendarEvent(
                        Id: uid ?? Guid.NewGuid().ToString("N"),
                        Title: summary ?? "Untitled Event",
                        Start: dtStart.Value,
                        End: dtEnd,
                        IsAllDay: isAllDay,
                        Location: location,
                        Description: description,
                        Source: sourceName
                    ));
                }
                inVEvent = false;
                continue;
            }

            if (!inVEvent) continue;

            int colon = line.IndexOf(':');
            if (colon <= 0) continue;

            string propPart = line[..colon];
            string valuePart = line[(colon + 1)..];

            string propName = propPart;
            int semi = propPart.IndexOf(';');
            if (semi > 0)
            {
                propName = propPart[..semi];
            }
            propName = propName.Trim().ToUpperInvariant();

            switch (propName)
            {
                case "UID":
                    uid = valuePart.Trim();
                    break;
                case "SUMMARY":
                    summary = Unescape(valuePart.Trim());
                    break;
                case "LOCATION":
                    location = Unescape(valuePart.Trim());
                    break;
                case "DESCRIPTION":
                    description = Unescape(valuePart.Trim());
                    break;
                case "DTSTART":
                    if (TryParseIcsDate(propPart, valuePart, out var startParsed, out bool allDayStart))
                    {
                        dtStart = startParsed;
                        isAllDay = allDayStart;
                    }
                    break;
                case "DTEND":
                    if (TryParseIcsDate(propPart, valuePart, out var endParsed, out _))
                    {
                        dtEnd = endParsed;
                    }
                    break;
            }
        }

        return events;
    }

    private static List<string> UnfoldLines(string text)
    {
        var result = new List<string>();
        using var reader = new StringReader(text);
        string? current = null;
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            if (line.Length == 0) continue;

            if (line.StartsWith(" ") || line.StartsWith("\t"))
            {
                if (current != null)
                {
                    current += line[1..];
                }
            }
            else
            {
                if (current != null) result.Add(current);
                current = line;
            }
        }

        if (current != null) result.Add(current);
        return result;
    }

    private static bool TryParseIcsDate(string propPart, string valuePart, out DateTime date, out bool isAllDay)
    {
        date = default;
        isAllDay = false;
        string val = valuePart.Trim();

        if (propPart.Contains("VALUE=DATE", StringComparison.OrdinalIgnoreCase) || val.Length == 8)
        {
            if (DateTime.TryParseExact(val, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            {
                date = d.Date;
                isAllDay = true;
                return true;
            }
        }

        string[] formats =
        [
            "yyyyMMdd'T'HHmmss'Z'",
            "yyyyMMdd'T'HHmmss",
            "yyyyMMdd'T'HHmm'Z'",
            "yyyyMMdd'T'HHmm"
        ];

        DateTimeStyles styles = val.EndsWith("Z", StringComparison.OrdinalIgnoreCase)
            ? DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal
            : DateTimeStyles.None;

        if (DateTime.TryParseExact(val, formats, CultureInfo.InvariantCulture, styles, out var dt))
        {
            date = dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;
            isAllDay = false;
            return true;
        }

        if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fallback))
        {
            date = fallback;
            return true;
        }

        return false;
    }

    private static string Unescape(string text)
    {
        return text
            .Replace(@"\,", ",")
            .Replace(@"\;", ";")
            .Replace(@"\n", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace(@"\\", @"\");
    }
}
