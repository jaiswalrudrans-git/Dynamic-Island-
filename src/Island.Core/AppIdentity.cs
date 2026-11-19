namespace Island.Core;

public static class AppIdentity
{
    public const string Name = "Island";

    public static string DataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Name);
}
