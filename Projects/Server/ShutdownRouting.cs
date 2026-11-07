/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ShutdownRouting.cs                                              *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server;

internal enum LoopLifecycle
{
    /// <summary>Startup: the loop thread is loading, nothing drains the loop context yet.</summary>
    NotStarted,

    /// <summary>The loop is iterating and drains posted work.</summary>
    Running,

    /// <summary>The loop has stopped iterating and its thread owns the rest of shutdown.</summary>
    ShuttingDown
}

internal enum ProcessExitRoute
{
    /// <summary>Run the shutdown handlers on the calling thread.</summary>
    RunHere,

    /// <summary>Ask the loop to shut down and wait for it to finish on its own thread.</summary>
    WaitForLoop
}

/// <summary>
/// Decides which thread runs shutdown handling. Those handlers touch network and game
/// state, so they must run on the loop thread, but the runtime raises the events that trigger them
/// on whatever thread exits.
/// </summary>
internal static class ShutdownRouting
{
    /// <summary>
    /// Whether to cancel the runtime's default termination for SIGTERM and shut down through the loop.
    /// During startup nothing would notice the request until the world finished loading, so the default
    /// (an immediate exit) stands.
    /// </summary>
    public static bool ShouldInterceptTerminate(LoopLifecycle lifecycle) => lifecycle != LoopLifecycle.NotStarted;

    // The loop thread must never wait on itself.
    public static ProcessExitRoute RouteProcessExit(bool onLoopThread, LoopLifecycle lifecycle) =>
        onLoopThread || lifecycle == LoopLifecycle.NotStarted ? ProcessExitRoute.RunHere : ProcessExitRoute.WaitForLoop;
}
