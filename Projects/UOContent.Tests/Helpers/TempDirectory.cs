using System;
using System.IO;

namespace Server.Tests;

/// <summary>
/// A uniquely named directory under the system temp path, deleted with its contents on dispose so a
/// failing assertion cannot leave files behind.
/// </summary>
internal sealed class TempDirectory : IDisposable
{
    public TempDirectory(string prefix = "modernuo-test")
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string Combine(string relativePath) => System.IO.Path.Combine(Path, relativePath);

    /// <summary>Writes <paramref name="content"/> to <paramref name="relativePath"/> and returns its full path.</summary>
    public string WriteFile(string relativePath, string content)
    {
        var path = Combine(relativePath);
        File.WriteAllText(path, content);
        return path;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }
        catch (IOException)
        {
            // A handle still open on Windows; the OS temp cleaner reclaims it.
        }
    }
}
