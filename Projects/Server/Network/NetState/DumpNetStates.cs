/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DumpNetStates.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.IO;

namespace Server.Network;

public static class DumpNetStates
{
    public static void Initialize()
    {
        CommandSystem.Register("DumpNetStates", AccessLevel.Administrator, DumpNetStatesCommand);
    }

    public static void DumpNetStatesCommand(CommandEventArgs args)
    {
        using var file = new StreamWriter("netstatedump.csv");

        file.WriteLine("NetState, RecvTask, SendTask, ProtocolState, ParserState");

        foreach (var ns in TcpServer.Instances)
        {
            file.WriteLine($"{ns}, {ns._protocolState}, {ns._parserState}");
        }
    }
}
