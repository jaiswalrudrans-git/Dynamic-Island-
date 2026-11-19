using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Island.Core.Settings;

// The UI thread owns this observable store. The file lock serializes edits across processes.
public sealed class SettingsStore : ObservableObject
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string directory;
    private readonly Action<string> log;
    private string SettingsPath => Path.Combine(directory, "settings.json");
    private AppSettings current = new();
    public AppSettings Current { get => current; private set => SetProperty(ref current, value); }

    public SettingsStore(string directory, Action<string> log)
    {
        this.directory = directory;
        this.log = log;
    }

    public void Load()
    {
        AppSettings loaded;
        using (AcquireLock())
        {
            loaded = ReadOrInitialize(out _);
        }
        Current = loaded;
    }

    public void Update(Func<AppSettings, AppSettings> edit)
    {
        AppSettings updated;
        using (AcquireLock())
        {
            var previous = ReadOrInitialize(out bool newerSchema);
            if (newerSchema)
                throw new InvalidOperationException("A newer application version owns these settings.");

            updated = edit(previous).Normalize();
            if (updated.SchemaVersion != AppSettings.CurrentSchema)
                throw new ArgumentException("Unsupported settings version.", nameof(edit));

            Write(updated);
        }
        Current = updated;
    }

    private FileStream AcquireLock()
    {
        Directory.CreateDirectory(directory);
        return new FileStream(Path.Combine(directory, "settings.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
    }

    private AppSettings ReadOrInitialize(out bool newerSchema)
    {
        newerSchema = false;
        try
        {
            using var stream = File.OpenRead(SettingsPath);
            using var document = JsonDocument.Parse(stream);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new JsonException("Expected a settings object.");

            if (root.TryGetProperty("schemaVersion", out var version))
            {
                if (version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out int schema) || schema < 1)
                    throw new JsonException("Invalid schema version.");
                if (schema > AppSettings.CurrentSchema)
                {
                    newerSchema = true;
                    log("Settings use a newer schema. Using session defaults and preserving the file.");
                    return new();
                }
            }
            return root.Deserialize<AppSettings>(Json)!.Normalize();
        }
        catch (FileNotFoundException)
        {
            // First launch.
        }
        catch (JsonException)
        {
            string backup = Path.Combine(directory, $"settings.invalid-{Guid.NewGuid():N}.json");
            File.Move(SettingsPath, backup);
            log($"Recovered invalid settings. Original saved as {Path.GetFileName(backup)}.");
        }

        var defaults = new AppSettings();
        Write(defaults);
        return defaults;
    }

    private void Write(AppSettings settings)
    {
        string temp = Path.Combine(directory, $"settings-{Guid.NewGuid():N}.tmp");
        try
        {
            using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(output, settings, Json);
                output.Flush(flushToDisk: true);
            }
            File.Move(temp, SettingsPath, overwrite: true);
        }
        finally
        {
            File.Delete(temp);
        }
    }
}
