using System.Runtime.InteropServices;
using Spectre.Console;

namespace BuildTool.Platform;

/// <summary>
/// Adapts output to the Windows console host (conhost), which is what every shell gets on Windows
/// Server and on Windows 10 without Windows Terminal. conhost renders VT colors but has no font
/// fallback, so emoji and braille spinners outside Consolas come out as boxes, and the truecolor logo
/// assumes a black background that Windows PowerShell paints blue.
/// </summary>
public static partial class TerminalProfile
{
    public static bool IsLegacyConsole { get; } = DetectLegacyConsole();

    public static void Configure()
    {
        if (!IsLegacyConsole)
        {
            return;
        }

        // Falls back to ASCII borders and spinners.
        AnsiConsole.Profile.Capabilities.Unicode = false;

        foreach (var (tag, replacement) in LegacyEmoji)
        {
            Emoji.Remap(tag, replacement);
        }
    }

    // Every emoji shortcode the tool's markup uses. A new shortcode needs an entry here or it renders
    // as a box in conhost. Status icons get a Consolas glyph; decorative ones render as nothing.
    private static readonly (string Tag, string Replacement)[] LegacyEmoji =
    [
        ("check_mark", "√"),
        ("check_mark_button", "√"),
        ("cross_mark", "×"),
        ("warning", "!"),
        ("left_arrow", "<"),
        ("hammer", ""),
        ("stethoscope", ""),
        ("wrench", ""),
        ("clipboard", ""),
        ("light_bulb", "")
    ];

    // Environment variables cannot tell: WT_SESSION is missing when Windows 11 hands a double-clicked
    // exe to Windows Terminal, and is inherited by a real conhost window started from inside one.
    // The console window class can: conhost owns a visible ConsoleWindowClass window, while anything
    // behind a pseudoconsole (Windows Terminal, VS Code, JetBrains) gets a hidden PseudoConsoleWindow.
    private static unsafe bool DetectLegacyConsole()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        var hwnd = GetConsoleWindow();
        if (hwnd == 0)
        {
            return false;
        }

        var buffer = stackalloc char[64];
        var length = GetClassNameW(hwnd, buffer, 64);
        return new ReadOnlySpan<char>(buffer, length).SequenceEqual("ConsoleWindowClass");
    }

    [LibraryImport("kernel32.dll")]
    private static partial nint GetConsoleWindow();

    [LibraryImport("user32.dll")]
    private static unsafe partial int GetClassNameW(nint hWnd, char* lpClassName, int nMaxCount);
}
