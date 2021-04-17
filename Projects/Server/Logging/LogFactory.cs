/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
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
using System.Collections.Generic;
using Serilog;

namespace Server.Logging
{
    public static class LogFactory
    {
        private static readonly Serilog.ILogger serilogLogger = new LoggerConfiguration()
            .WriteTo.Async(a => a.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}"
            ))
            .CreateLogger();

        private static readonly Dictionary<Type, ILogger> m_Loggers = new();

        public static ILogger GetLogger(Type declaringType)
        {
            if (m_Loggers.ContainsKey(declaringType))
            {
                return m_Loggers[declaringType];
            }

            return m_Loggers[declaringType] = CreateLogger(declaringType);
        }

        private static ILogger CreateLogger(Type declaringType) => new SerilogLogger(serilogLogger.ForContext(declaringType));
    }
}
