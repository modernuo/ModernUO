/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: LogFactory.cs                                                   *
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
using Serilog;

namespace Server.Logging;

public static class LogFactory
{
    private static readonly Serilog.ILogger serilogLogger = new LoggerConfiguration()
        .WriteTo.Async(a => a.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}"
        ))
#if DEBUG
        .MinimumLevel.Debug()
#endif
        .CreateLogger();

    public static ILogger GetLogger(Type declaringType) => new SerilogLogger(serilogLogger.ForContext(declaringType));
}
