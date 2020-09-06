/***************************************************************************
 *                                Commands.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using Server.Network;

namespace Server
{
    public delegate void CommandEventHandler(CommandEventArgs e);

    public class CommandEventArgs : EventArgs
    {
        public CommandEventArgs(Mobile mobile, string command, string argString, string[] arguments)
        {
            Mobile = mobile;
            Command = command;
            ArgString = argString;
            Arguments = arguments;
        }

        public Mobile Mobile { get; }

        public string Command { get; }

        public string ArgString { get; }

        public string[] Arguments { get; }

        public int Length => Arguments.Length;

        public string GetString(int index)
        {
            if (index < 0 || index >= Arguments.Length)
                return "";

            return Arguments[index];
        }

        public int GetInt32(int index)
        {
            if (index < 0 || index >= Arguments.Length)
                return 0;

            return Utility.ToInt32(Arguments[index]);
        }

        public uint GetUInt32(int index)
        {
            if (index < 0 || index >= Arguments.Length)
                return 0;

            return Utility.ToUInt32(Arguments[index]);
        }

        public bool GetBoolean(int index)
        {
            if (index < 0 || index >= Arguments.Length)
                return false;

            return Utility.ToBoolean(Arguments[index]);
        }

        public double GetDouble(int index)
        {
            if (index < 0 || index >= Arguments.Length)
                return 0.0;

            return Utility.ToDouble(Arguments[index]);
        }

        public TimeSpan GetTimeSpan(int index)
        {
            if (index < 0 || index >= Arguments.Length)
                return TimeSpan.Zero;

            return Utility.ToTimeSpan(Arguments[index]);
        }
    }

    public static partial class EventSink
    {
        public static event Action<CommandEventArgs> Command;
        public static void InvokeCommand(CommandEventArgs e) => Command?.Invoke(e);
    }

    public class CommandEntry : IComparable<CommandEntry>
    {
        public CommandEntry(string command, CommandEventHandler handler, AccessLevel accessLevel)
        {
            Command = command;
            Handler = handler;
            AccessLevel = accessLevel;
        }

        public string Command { get; }

        public CommandEventHandler Handler { get; }

        public AccessLevel AccessLevel { get; }

        public int CompareTo(CommandEntry e) => e == null ? 1 : Command.CompareTo(e.Command);
    }

    public static class CommandSystem
    {
        public static string Prefix { get; set; } = "[";

        public static Dictionary<string, CommandEntry> Entries { get; } =
            new Dictionary<string, CommandEntry>(StringComparer.OrdinalIgnoreCase);

        public static AccessLevel BadCommandIgnoreLevel { get; set; } = AccessLevel.Player;

        public static string[] Split(string value)
        {
            var array = value.ToCharArray();
            var list = new List<string>();

            var start = 0;

            while (start < array.Length)
            {
                var c = array[start];

                if (c == '"')
                {
                    ++start;
                    var end = start;

                    while (end < array.Length)
                        if (array[end] != '"' || array[end - 1] == '\\')
                            ++end;
                        else
                            break;

                    list.Add(value.Substring(start, end - start));

                    start = end + 2;
                }
                else if (c != ' ')
                {
                    var end = start;

                    while (end < array.Length)
                        if (array[end] != ' ')
                            ++end;
                        else
                            break;

                    list.Add(value.Substring(start, end - start));

                    start = end + 1;
                }
                else
                {
                    ++start;
                }
            }

            return list.ToArray();
        }

        public static void Register(string command, AccessLevel access, CommandEventHandler handler)
        {
            Entries[command] = new CommandEntry(command, handler, access);
        }

        public static bool Handle(Mobile from, string text, MessageType type = MessageType.Regular)
        {
            if (!text.StartsWith(Prefix) && type != MessageType.Command)
                return false;

            if (type != MessageType.Command)
                text = text.Substring(Prefix.Length);

            var indexOf = text.IndexOf(' ');

            string command;
            string[] args;
            string argString;

            if (indexOf >= 0)
            {
                argString = text.Substring(indexOf + 1);

                command = text.Substring(0, indexOf);
                args = Split(argString);
            }
            else
            {
                argString = "";
                command = text.ToLower();
                args = Array.Empty<string>();
            }

            Entries.TryGetValue(command, out var entry);

            if (entry != null)
            {
                if (from.AccessLevel >= entry.AccessLevel)
                {
                    if (entry.Handler != null)
                    {
                        var e = new CommandEventArgs(from, command, argString, args);
                        entry.Handler(e);
                        EventSink.InvokeCommand(e);
                    }
                }
                else
                {
                    if (from.AccessLevel <= BadCommandIgnoreLevel)
                        return false;

                    from.SendMessage("You do not have access to that command.");
                }
            }
            else
            {
                if (from.AccessLevel <= BadCommandIgnoreLevel)
                    return false;

                from.SendMessage("That is not a valid command.");
            }

            return true;
        }
    }
}
