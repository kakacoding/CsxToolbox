using System.Runtime.CompilerServices;

public static string GetScriptPath([CallerFilePath] string path = null) => path;
public static string GetScriptFileName([CallerFilePath] string path = null) => Path.GetFileName(path);
public static string GetScriptFolderPath([CallerFilePath] string path = null) => Path.GetDirectoryName(path);
public static string GetScriptFolderName([CallerFilePath] string path = null) => Path.GetFileName(Path.GetDirectoryName(path));
public static string GetFullPath(string path, [CallerFilePath] string cpath = null) => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(cpath), path));

public class Logger(string tag)
{
    private readonly string _tag = tag;

    public void Log(string msg)
    {
        WriteLine(string.IsNullOrEmpty(_tag) ? $"{msg}" : $"[{_tag}] {msg}");
    }
    public void LogWarning(string msg)
    {
        ForegroundColor = ConsoleColor.Yellow;
        WriteLine(string.IsNullOrEmpty(_tag) ? $"{msg}" : $"[{_tag}] {msg}");
        ResetColor();
    }
    public void LogError(string msg)
    {
        ForegroundColor = ConsoleColor.Red;
        WriteLine(string.IsNullOrEmpty(_tag) ? $"{msg}" : $"[{_tag}] {msg}");
        ResetColor();
    }
}

