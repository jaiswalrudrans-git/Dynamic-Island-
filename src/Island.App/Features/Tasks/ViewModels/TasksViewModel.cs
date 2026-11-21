using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Island.Core.Tasks;

namespace Island.App.Features.Tasks.ViewModels;

public sealed record TaskItemUi(
    string Id,
    string Title,
    bool IsCompleted,
    TaskPriority Priority,
    string PriorityText,
    Brush PriorityBrush,
    string? DueText,
    bool IsOverdue,
    bool HasReminder,
    int Order
);

public sealed class TasksViewModel : ObservableObject, IDisposable
{
    private readonly Dispatcher dispatcher;
    private readonly TaskStore store;
    private string newTaskTitle = "";
    private TaskPriority newTaskPriority = TaskPriority.Medium;
    private string filter = "All"; // "All", "Active", "Done"
    private bool disposed;

    public string NewTaskTitle
    {
        get => newTaskTitle;
        set
        {
            if (SetProperty(ref newTaskTitle, value))
            {
                OnPropertyChanged(nameof(HasInput));
            }
        }
    }

    public TaskPriority NewTaskPriority
    {
        get => newTaskPriority;
        set
        {
            if (SetProperty(ref newTaskPriority, value))
            {
                OnPropertyChanged(nameof(NewTaskPriorityBrush));
                OnPropertyChanged(nameof(NewTaskPriorityText));
            }
        }
    }

    public Brush NewTaskPriorityBrush => newTaskPriority switch
    {
        TaskPriority.High => new SolidColorBrush(Color.FromRgb(255, 82, 82)),
        TaskPriority.Low => new SolidColorBrush(Color.FromRgb(66, 165, 245)),
        _ => new SolidColorBrush(Color.FromRgb(255, 167, 38))
    };

    public string NewTaskPriorityText => newTaskPriority switch
    {
        TaskPriority.High => "High",
        TaskPriority.Low => "Low",
        _ => "Med"
    };

    public bool HasInput => !string.IsNullOrWhiteSpace(newTaskTitle);

    public string Filter
    {
        get => filter;
        set
        {
            if (SetProperty(ref filter, value))
            {
                NotifyFilterChanges();
            }
        }
    }

    public bool IsAllFilter => filter == "All";
    public bool IsActiveFilter => filter == "Active";
    public bool IsDoneFilter => filter == "Done";

    public int AllCount => store.Items.Count;
    public int ActiveCount => store.Items.Count(t => !t.IsCompleted);
    public int DoneCount => store.Items.Count(t => t.IsCompleted);

    public string AllFilterText => $"All ({AllCount})";
    public string ActiveFilterText => $"Active ({ActiveCount})";
    public string DoneFilterText => $"Done ({DoneCount})";

    public bool HasTasks => store.Items.Count > 0;
    public bool HasFilteredTasks => FilteredTasks.Count > 0;
    public bool IsEmpty => FilteredTasks.Count == 0;

    public event Action? TasksCountChanged;

    public IReadOnlyList<TaskItemUi> FilteredTasks
    {
        get
        {
            var raw = store.Items;
            var list = new List<TaskItemUi>();
            var now = DateTime.Now;

            foreach (var t in raw)
            {
                if (filter == "Active" && t.IsCompleted) continue;
                if (filter == "Done" && !t.IsCompleted) continue;

                string prioText = t.Priority switch
                {
                    TaskPriority.High => "High",
                    TaskPriority.Low => "Low",
                    _ => "Med"
                };

                Brush prioBrush = t.Priority switch
                {
                    TaskPriority.High => new SolidColorBrush(Color.FromRgb(255, 82, 82)),
                    TaskPriority.Low => new SolidColorBrush(Color.FromRgb(66, 165, 245)),
                    _ => new SolidColorBrush(Color.FromRgb(255, 167, 38))
                };

                string? dueText = null;
                bool isOverdue = false;
                if (t.DueDate.HasValue)
                {
                    var d = t.DueDate.Value;
                    if (d.Date == now.Date) dueText = "Today";
                    else if (d.Date == now.Date.AddDays(1)) dueText = "Tomorrow";
                    else dueText = d.ToString("MMM d");

                    isOverdue = d < now && !t.IsCompleted;
                }

                list.Add(new TaskItemUi(
                    t.Id,
                    t.Title,
                    t.IsCompleted,
                    t.Priority,
                    prioText,
                    prioBrush,
                    dueText,
                    isOverdue,
                    t.ReminderTime.HasValue,
                    t.Order
                ));
            }
            return list;
        }
    }

    public TasksViewModel(Dispatcher dispatcher, TaskStore store)
    {
        this.dispatcher = dispatcher;
        this.store = store;
        store.ItemsChanged += OnTasksChanged;
    }

    public void AddTask()
    {
        if (string.IsNullOrWhiteSpace(newTaskTitle)) return;
        store.Add(newTaskTitle.Trim(), newTaskPriority);
        NewTaskTitle = "";
        TasksCountChanged?.Invoke();
    }

    public void ToggleComplete(string id)
    {
        store.ToggleComplete(id);
        TasksCountChanged?.Invoke();
    }

    public void DeleteTask(string id)
    {
        store.Delete(id);
        TasksCountChanged?.Invoke();
    }

    public void MoveUp(string id) => store.Move(id, -1);
    public void MoveDown(string id) => store.Move(id, 1);

    public void CyclePriority()
    {
        NewTaskPriority = newTaskPriority switch
        {
            TaskPriority.Low => TaskPriority.Medium,
            TaskPriority.Medium => TaskPriority.High,
            _ => TaskPriority.Low
        };
    }

    private void OnTasksChanged()
    {
        if (disposed) return;
        dispatcher.InvokeAsync(() =>
        {
            NotifyFilterChanges();
            OnPropertyChanged(nameof(HasTasks));
            OnPropertyChanged(nameof(AllCount));
            OnPropertyChanged(nameof(ActiveCount));
            OnPropertyChanged(nameof(DoneCount));
            OnPropertyChanged(nameof(AllFilterText));
            OnPropertyChanged(nameof(ActiveFilterText));
            OnPropertyChanged(nameof(DoneFilterText));
            TasksCountChanged?.Invoke();
        });
    }

    private void NotifyFilterChanges()
    {
        OnPropertyChanged(nameof(FilteredTasks));
        OnPropertyChanged(nameof(HasFilteredTasks));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsAllFilter));
        OnPropertyChanged(nameof(IsActiveFilter));
        OnPropertyChanged(nameof(IsDoneFilter));
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        store.ItemsChanged -= OnTasksChanged;
    }
}
