using Spectre.Console;

namespace BuildTool;

/// <summary>
/// Static branding assets for the BuildTool CLI.
/// Colors sourced from ModernUO brand palette (docs/packets/template.html).
/// </summary>
public static class Branding
{
    // ModernUO brand gold palette
    public static readonly Color Gold = new(213, 191, 116);       // #d5bf74 - primary brand gold
    public static readonly Color GoldLight = new(223, 198, 136);  // #dfc688 - interactive elements
    public static readonly Color GoldMuted = new(232, 206, 161);  // #e8cea1 - body text
    public static readonly Color GoldDim = new(162, 145, 71);     // #a29147 - dimmed gold

    // Functional colors
    public static readonly Color Success = new(34, 197, 94);      // #22c55e - green
    public static readonly Color Info = new(59, 130, 246);        // #3b82f6 - blue

    // Styles
    public static readonly Style GoldStyle = new(Gold);
    public static readonly Style HighlightStyle = new(GoldLight);
    public static readonly Style DimGoldStyle = new(GoldDim);

    /// <summary>
    /// The ModernUO ASCII art logo (ANSI Shadow font).
    /// Rendered inside a Spectre.Console Panel for proper box alignment.
    /// </summary>
    public const string Logo = """
        ███╗   ███╗ ██████╗ ██████╗ ███████╗██████╗ ███╗   ██╗██╗   ██╗ ██████╗
        ████╗ ████║██╔═══██╗██╔══██╗██╔════╝██╔══██╗████╗  ██║██║   ██║██╔═══██╗
        ██╔████╔██║██║   ██║██║  ██║█████╗  ██████╔╝██╔██╗ ██║██║   ██║██║   ██║
        ██║╚██╔╝██║██║   ██║██║  ██║██╔══╝  ██╔══██╗██║╚██╗██║██║   ██║██║   ██║
        ██║ ╚═╝ ██║╚██████╔╝██████╔╝███████╗██║  ██║██║ ╚████║╚██████╔╝╚██████╔╝
        ╚═╝     ╚═╝ ╚═════╝ ╚═════╝ ╚══════╝╚═╝  ╚═╝╚═╝  ╚═══╝ ╚═════╝  ╚═════╝
        """;

    /// <summary>
    /// Subtitle shown below the banner.
    /// </summary>
    public const string Subtitle = "Ultima Online Server Emulator";
}
