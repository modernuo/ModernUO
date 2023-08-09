/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerilogLogger.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Runtime.CompilerServices;

namespace Server.Logging;

public class SerilogLogger : ILogger
{
    private readonly Serilog.ILogger serilogLogger;

    public SerilogLogger(Serilog.ILogger serilogLogger) =>
        this.serilogLogger = serilogLogger;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Debug(string message, params object[] args) =>
        serilogLogger.Debug(message, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Debug(Exception exception, string message, params object[] args) =>
        serilogLogger.Debug(exception, message, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Information(string message, params object[] args) =>
        serilogLogger.Information(message, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Information(Exception exception, string message, params object[] args) =>
        serilogLogger.Information(exception, message, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Warning(string message, params object[] args) =>
        serilogLogger.Warning(message, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Warning(Exception exception, string message, params object[] args) =>
        serilogLogger.Warning(exception, message, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Error(string message, params object[] args) =>
        serilogLogger.Error(message, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Error(Exception exception, string message, params object[] args) =>
        serilogLogger.Error(exception, message, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Fatal(string message, params object[] args) =>
        serilogLogger.Fatal(message, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Fatal(Exception exception, string message, params object[] args) =>
        serilogLogger.Fatal(exception, message, args);
}
