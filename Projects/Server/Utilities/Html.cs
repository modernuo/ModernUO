using System.Runtime.CompilerServices;

namespace Server.Utilities;

public static class Html
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Color(this string text, int color) => $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Color(this string text, int color, int size) => $"<BASEFONT COLOR=#{color:X6} SIZE={size}>{text}</BASEFONT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Color(this string text, string color) => $"<BASEFONT COLOR={color}>{text}</BASEFONT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Color(this string text, string color, int size) => $"<BASEFONT COLOR={color} SIZE={size}>{text}</BASEFONT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Center(this string text) => $"<CENTER>{text}</CENTER>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Center(this string text, int color) => $"<BASEFONT COLOR=#{color:X6}><CENTER>{text}</CENTER></BASEFONT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Center(this string text, int color, int size) => $"<BASEFONT COLOR=#{color:X6} SIZE={size}><CENTER>{text}</CENTER></BASEFONT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Center(this string text, string color) => $"<BASEFONT COLOR={color}><CENTER>{text}</CENTER></BASEFONT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Center(this string text, string color, int size) => $"<BASEFONT COLOR={color} SIZE={size}><CENTER>{text}</CENTER></BASEFONT>";
}
