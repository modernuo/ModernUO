using System;

namespace Server;

/// <summary>
/// Thrown when interactive console input is required but the server is running
/// headless (stdin is not a TTY). Rides the fatal-shutdown path.
/// </summary>
public sealed class HeadlessConsoleInputException : Exception
{
    public HeadlessConsoleInputException(string context)
        : base(
            $"Interactive console input required but the server is headless (stdin is not a TTY). " +
            $"Pre-supply the required configuration/save data. Prompt: {context}."
        )
    {
    }
}
