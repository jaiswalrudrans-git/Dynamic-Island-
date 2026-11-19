using System.IO;

namespace Island.Tests;

public abstract class TestFolder : IDisposable
{
    protected string Folder { get; } = Directory.CreateTempSubdirectory("island-test-").FullName;
    protected List<string> Messages { get; } = [];

    public void Dispose() => Directory.Delete(Folder, recursive: true);
}
