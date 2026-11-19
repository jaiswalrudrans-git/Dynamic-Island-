using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Island.Core.Tasks;

namespace Island.App.ViewModels;

public sealed record TaskItemUi(
    string Id,
    string Title,
    bool IsCompleted,
    TaskPriority Priority,
    string PriorityText,
    Brush PriorityBrush,
    string? DueText,
    bool IsOverdue,
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
        set => SetProperty(ref newTaskTitle, value);
    }

    public TaskPriority NewTaskPriority
    {
        get => newTaskPriority;
        set => SetProperty(ref newTaskPriority, value);
    }

    public string Filter
    {
        get => filter;
        set
        {
            if (SetProperty(ref filter, value))
            {
                OnPropertyChanged(nameof(FilteredTasks));
            }
        }
    }

    public bool HasTasks => store.Items.Count > 0;

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
                    TaskPriority.High => new SolidColorBrush(Color.FromRgb(255, 99, 132)),
                    TaskPriority.Low => new SolidColorBrush(Color.FromRgb(100, 181, 246)),
                    _ => new SolidColorBrush(Color.FromRgb(255, 193, 7))
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
        store.ItemsChanged += OnItemsChanged;
    }

    private void OnItemsChanged()
    {
        if (disposed) return;
        dispatcher.InvokeAsync(() =>
        {
            OnPropertyChanged(nameof(HasTasks));
            OnPropertyChanged(nameof(FilteredTasks));
        });
    }

    public void AddTask()
    {
        if (string.IsNullOrWhiteSpace(NewTaskTitle)) return;
        store.Add(NewTaskTitle, NewTaskPriority);
        NewTaskTitle = "";
    }

    public void ToggleComplete(string id) => store.ToggleComplete(id);

    public void DeleteTask(string id) => store.Delete(id);

    public void MoveUp(string id) => store.Move(id, -1);

    public void MoveDown(string id) => store.Move(id, 1);

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        store.ItemsChanged -= OnItemsChanged;
    }
}
