using System.IO;
using System.Text.Json;
using Island.Core.Settings;
using Xunit;

namespace Island.Tests;

public sealed class SettingsTests : TestFolder
{
    private string SettingsFile => Path.Combine(Folder, "settings.json");

    [Fact]
    public void FirstLaunchCreatesDefaultsAndEditsPersistAcrossInstances()
    {
        var writer = new SettingsStore(Folder, Messages.Add);
        writer.Load();
        Assert.Equal("Dark", writer.Current.Theme);
        Assert.True(writer.Current.Use24HourClock);
        Assert.True(File.Exists(SettingsFile));

        bool savedBeforeNotification = false;
        writer.PropertyChanged += (_, e) =>
        {
            var reader = new SettingsStore(Folder, Messages.Add);
            reader.Load();
            savedBeforeNotification = e.PropertyName == "Current" && reader.Current == writer.Current;
        };
        writer.Update(s => s with { Theme = "Dracula", ShowSeconds = true });
        Assert.True(savedBeforeNotification);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("""{"schemaVersion":0}""")]
    public void CorruptInputIsPreservedAndReplacedWithUsableDefaults(string input)
    {
        File.WriteAllText(SettingsFile, input);
        var store = new SettingsStore(Folder, Messages.Add);
        store.Load();
        Assert.Equal(input, File.ReadAllText(Assert.Single(Directory.GetFiles(Folder, "settings.invalid-*.json"))));
        Assert.Equal(new AppSettings(), store.Current);
        Assert.Contains("schemaVersion", File.ReadAllText(SettingsFile));
        Assert.Single(Messages);
    }

    [Fact]
    public void NewerSchemaIsPreservedEvenIfItsFieldTypesHaveChanged()
    {
        const string future = """{"schemaVersion":2,"theme":{"id":"dark"}}""";
        File.WriteAllText(SettingsFile, future);
        var store = new SettingsStore(Folder, Messages.Add);
        store.Load();
        Assert.Equal(new AppSettings(), store.Current);
        Assert.Throws<InvalidOperationException>(() => store.Update(s => s with { ShowSeconds = true }));
        Assert.Equal(future, File.ReadAllText(SettingsFile));
    }

    [Fact]
    public void PartialSettingsReceiveDefaultsAndUnrecognizedFieldsSurviveEdits()
    {
        File.WriteAllText(SettingsFile, """{"theme":"  ","animationSpeed":20,"extra":42}""");
        var store = new SettingsStore(Folder, Messages.Add);
        store.Load();
        Assert.Equal(2, store.Current.AnimationSpeed);
        Assert.Equal("Dark", store.Current.Theme);
        Assert.True(store.Current.Use24HourClock);
        store.Update(s => s with { AnimationSpeed = 0.1 });
        using var json = JsonDocument.Parse(File.ReadAllText(SettingsFile));
        Assert.Equal(42, json.RootElement.GetProperty("extra").GetInt32());
        Assert.Equal(.5, json.RootElement.GetProperty("animationSpeed").GetDouble());
    }

    [Fact]
    public void SeparateWritersKeepEachOthersCommittedChanges()
    {
        var first = new SettingsStore(Folder, Messages.Add);
        var second = new SettingsStore(Folder, Messages.Add);
        first.Load();
        second.Load();
        first.Update(s => s with { Use24HourClock = false });
        second.Update(s => s with { ReduceMotion = true });
        first.Load();
        Assert.False(first.Current.Use24HourClock);
        Assert.True(first.Current.ReduceMotion);
    }
}
