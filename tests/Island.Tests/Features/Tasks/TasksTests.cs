using Island.Core.Tasks;
using Xunit;

namespace Island.Tests;

public sealed class TasksTests : TestFolder
{
    [Fact]
    public void TaskStore_AddAndRetrieve_MaintainsOrderAndPriority()
    {
        using var store = new TaskStore(Folder, msg => Messages.Add(msg));
        store.Add("Buy milk", TaskPriority.Low);
        store.Add("Review PR", TaskPriority.High);
        store.Add("Write docs", TaskPriority.Medium);

        var items = store.Items;
        Assert.Equal(3, items.Count);
        Assert.Equal("Buy milk", items[0].Title);
        Assert.Equal(TaskPriority.Low, items[0].Priority);
        Assert.Equal("Review PR", items[1].Title);
        Assert.Equal(TaskPriority.High, items[1].Priority);
        Assert.Equal("Write docs", items[2].Title);
    }

    [Fact]
    public void TaskStore_ToggleCompleteAndReopen()
    {
        using var store = new TaskStore(Folder);
        store.Add("Task 1");
        var id = store.Items[0].Id;
        Assert.False(store.Items[0].IsCompleted);

        store.ToggleComplete(id);
        Assert.True(store.Items[0].IsCompleted);

        store.ToggleComplete(id);
        Assert.False(store.Items[0].IsCompleted);
    }

    [Fact]
    public void TaskStore_Delete_RemovesItem()
    {
        using var store = new TaskStore(Folder);
        store.Add("Task 1");
        store.Add("Task 2");
        var id = store.Items[0].Id;

        store.Delete(id);
        Assert.Single(store.Items);
        Assert.Equal("Task 2", store.Items[0].Title);
    }

    [Fact]
    public void TaskStore_Move_ReordersItemsSafely()
    {
        using var store = new TaskStore(Folder);
        store.Add("A");
        store.Add("B");
        store.Add("C");

        var bId = store.Items[1].Id;
        store.Move(bId, -1); // Move B up to index 0

        var items = store.Items;
        Assert.Equal("B", items[0].Title);
        Assert.Equal("A", items[1].Title);
        Assert.Equal("C", items[2].Title);

        // Clamping check: Move B up again does nothing
        store.Move(bId, -1);
        Assert.Equal("B", store.Items[0].Title);
    }

    [Fact]
    public void TaskStore_PersistsToDiskAndReloads()
    {
        using (var store1 = new TaskStore(Folder))
        {
            store1.Add("Persistent Task", TaskPriority.High);
        }

        using (var store2 = new TaskStore(Folder))
        {
            Assert.Single(store2.Items);
            Assert.Equal("Persistent Task", store2.Items[0].Title);
            Assert.Equal(TaskPriority.High, store2.Items[0].Priority);
        }
    }

    [Fact]
    public void TaskStore_Snooze_UpdatesReminderTime()
    {
        var now = new DateTime(2026, 11, 19, 12, 0, 0);
        using var store = new TaskStore(Folder, clock: () => now);
        store.Add("Snooze Task", reminderTime: now.AddMinutes(5));
        var id = store.Items[0].Id;

        store.Snooze(id, TimeSpan.FromMinutes(10));
        Assert.Equal(now.AddMinutes(10), store.Items[0].ReminderTime);
    }
}
