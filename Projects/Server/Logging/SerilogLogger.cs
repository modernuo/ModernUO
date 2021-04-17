/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
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

namespace Server.Logging
{
    public class SerilogLogger : ILogger
    {
        private readonly Serilog.ILogger serilogLogger;

        public SerilogLogger(Serilog.ILogger serilogLogger) =>
            this.serilogLogger = serilogLogger;

        public void Debug(string message, params object[] args) =>
            serilogLogger.Debug(message, args);

        public void Debug(Exception exception, string message, params object[] args) =>
            serilogLogger.Debug(exception, message, args);

        public void Information(string message, params object[] args) =>
            serilogLogger.Information(message, args);

        public void Information(Exception exception, string message, params object[] args) =>
            serilogLogger.Information(exception, message, args);

        public void Warning(string message, params object[] args) =>
            serilogLogger.Warning(message, args);

        public void Warning(Exception exception, string message, params object[] args) =>
            serilogLogger.Information(exception, message, args);

        public void Error(string message, params object[] args) =>
            serilogLogger.Error(message, args);

        public void Error(Exception exception, string message, params object[] args) =>
            serilogLogger.Error(exception, message, args);

        public void Fatal(string message, params object[] args) =>
            serilogLogger.Fatal(message, args);

        public void Fatal(Exception exception, string message, params object[] args) =>
            serilogLogger.Fatal(exception, message, args);
    }
}
