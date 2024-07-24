/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ConsoleInputHandler.cs                                          *
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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Server.Logging;

namespace Server;

public static class ConsoleInputHandler
{
    private static readonly AutoResetEvent _receivedUserInput = new(false);
    private static readonly AutoResetEvent _endUserInput = new(false);
    private static bool _initialized;
    private static bool _expectUserInput;
    private static readonly Dictionary<string, ConsoleCommand> _inputCommands = new();
    private static string[] _commandDescriptions;
    private static string _input;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegisterCommand(string command, string description, Action<string> function) =>
        RegisterCommand([command], description, function);

    // Note: Functions will be executed on a background thread!!
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegisterCommand(string[] commands, string description, Action<string> function)
    {
        if (commands is { Length: > 0 } && function != null)
        {
            var consoleCommand = new ConsoleCommand(commands, description, function);
            lock (_inputCommands)
            {
                for (var i = 0; i < commands.Length; i++)
                {
                    var command = commands[i].ToLower();
                    _inputCommands[command] = consoleCommand;
                }
            }

            _commandDescriptions = null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool UnregisterInputCommand(string command)
    {
        lock (_inputCommands)
        {
            var removed = _inputCommands.Remove(command);
            _commandDescriptions = null;
            return removed;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Action<string> GetInputCommand(string command)
    {
        lock (_inputCommands)
        {
            var action = _inputCommands.GetValueOrDefault(command)?.Function;
            _commandDescriptions = null;
            return action;
        }
    }

    public static void Configure()
    {
        RegisterCommand(["help", "?"], "Displays this help screen.", DisplayHelp);
    }

    [CallPriority(0)]
    public static void Initialize()
    {
        _initialized = true;

        new Thread(ProcessConsoleInput)
        {
            IsBackground = true,
            Name = "Console Input Handler"
        }.Start();
    }

    private static string[] GetHelpDescriptions()
    {
        if (_commandDescriptions != null)
        {
            return _commandDescriptions;
        }

        HashSet<ConsoleCommand> commands;
        lock (_inputCommands)
        {
            commands = _inputCommands.Values.ToHashSet();
        }

        var longestCommand = 0;
        var commandTuples = new (string Command, string Arguments)[commands.Count];

        var index = 0;
        foreach (var command in commands)
        {
            var commandAliases = string.Join("|", command.Commands);
            longestCommand = Math.Max(longestCommand, commandAliases.Length);
            commandTuples[index++] = (commandAliases, command.Description);
        }

        Array.Sort(commandTuples, (a, b) => a.Command.CompareOrdinal(b.Command));

        _commandDescriptions = new string[commandTuples.Length];

        for (var i = 0; i < commandTuples.Length; i++)
        {
            var (commandAliases, description) = commandTuples[i];
            _commandDescriptions[i] = $"{commandAliases.PadRight(longestCommand + 1)} - {description}";
        }

        return _commandDescriptions;
    }

    private static void DisplayHelp(string arguments)
    {
        var commandDescriptions = GetHelpDescriptions();
        if (commandDescriptions == null || commandDescriptions.Length == 0)
        {
            Console.WriteLine("No console commands registered.");
            return;
        }

        Console.WriteLine("Available Commands:");
        for (var i = 0; i < _commandDescriptions.Length; i++)
        {
            Console.WriteLine(_commandDescriptions[i]);
        }
    }

    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ConsoleInputHandler));

    private static async void ProcessConsoleInput()
    {
        while (!Core.Closing)
        {
            string input;
            try
            {
                input = Console.ReadLine()?.Trim();
            }
            catch
            {
                logger.Warning("Console commands have been disabled due to an error.");
                _initialized = false;
                return;
            }

            if (Volatile.Read(ref _expectUserInput))
            {
                _input = input;
                _receivedUserInput.Set();
                _endUserInput.WaitOne();
                continue;
            }

            if (string.IsNullOrEmpty(input))
            {
                continue;
            }

            var splitInput = input.Split(' ', 2);
            var command = splitInput[0].ToLower();

            try
            {
                Action<string> action;
                lock (_inputCommands)
                {
                    action = _inputCommands.GetValueOrDefault(command)?.Function;
                }

                action?.Invoke(splitInput.Length > 1 ? splitInput[1] : string.Empty);
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to execute console command: {Command}", input);
            }
        }
    }

    public static string ReadLine()
    {
        if (!_initialized)
        {
            return Console.ReadLine();
        }

        Volatile.Write(ref _expectUserInput, true);
        _receivedUserInput.WaitOne();
        var line = _input;
        Volatile.Write(ref _expectUserInput, false);
        _endUserInput.Set();

        return line;
    }

    private class ConsoleCommand(string[] commands, string description, Action<string> function)
    {
        public readonly string[] Commands = commands;
        public readonly string Description = description;
        public readonly Action<string> Function = function;
    }
}
