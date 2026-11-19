namespace Island.Core.Tasks;

public enum TaskPriority
{
    Low,
    Medium,
    High
}

public enum ReminderRepeat
{
    None,
    Daily,
    Weekdays,
    Weekly
}

public sealed record TaskItem(
    string Id,
    string Title,
    bool IsCompleted = false,
    TaskPriority Priority = TaskPriority.Medium,
    DateTime? DueDate = null,
    DateTime? ReminderTime = null,
    ReminderRepeat Repeat = ReminderRepeat.None,
    int Order = 0
)
{
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
