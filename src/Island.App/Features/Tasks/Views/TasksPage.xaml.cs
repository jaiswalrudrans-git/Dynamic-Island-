using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Island.App.Features.Tasks.ViewModels;
using Island.Core.Tasks;

namespace Island.App.Features.Tasks.Views;

public partial class TasksPage : UserControl
{
    private readonly TasksViewModel model;
    private readonly Action<bool> setTypingFocus;

    public TasksPage(TasksViewModel model, Action<bool> setTypingFocus)
    {
        InitializeComponent();
        this.model = model;
        this.setTypingFocus = setTypingFocus;
        DataContext = model;
    }

    private void TaskInputPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        setTypingFocus(true);
        TaskInput.Focus();
        Keyboard.Focus(TaskInput);
    }

    private void TaskInputGotFocus(object sender, RoutedEventArgs e) => setTypingFocus(true);
    private void TaskInputLostFocus(object sender, RoutedEventArgs e) => setTypingFocus(false);

    private void TaskInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            model.AddTask();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            setTypingFocus(false);
            Keyboard.ClearFocus();
            e.Handled = true;
        }
    }

    private void AddTaskClick(object sender, RoutedEventArgs e)
    {
        model.AddTask();
        TaskInput.Focus();
        Keyboard.Focus(TaskInput);
    }

    private void CyclePriorityClick(object sender, RoutedEventArgs e)
    {
        model.NewTaskPriority = model.NewTaskPriority switch
        {
            TaskPriority.Low => TaskPriority.Medium,
            TaskPriority.Medium => TaskPriority.High,
            _ => TaskPriority.Low
        };
    }

    private void FilterAllClick(object sender, RoutedEventArgs e) => model.Filter = "All";
    private void FilterActiveClick(object sender, RoutedEventArgs e) => model.Filter = "Active";
    private void FilterDoneClick(object sender, RoutedEventArgs e) => model.Filter = "Done";

    private void ToggleClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id }) model.ToggleComplete(id);
    }

    private void DeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id }) model.DeleteTask(id);
    }

    private void MoveUpClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id }) model.MoveUp(id);
    }

    private void MoveDownClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id }) model.MoveDown(id);
    }
}
