using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Server;

public static class ConsoleInputHandler
{
    private static readonly AutoResetEvent _receivedUserInput = new(false);
    private static readonly AutoResetEvent _endUserInput = new(false);
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

    public static void Initialize()
    {
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

    private static async void ProcessConsoleInput()
    {
        var token = Core.ClosingTokenSource.Token;

        while (!token.IsCancellationRequested)
        {
            var input = Console.ReadLine()?.Trim();

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

            Action<string> action;
            lock (_inputCommands)
            {
                action = _inputCommands.GetValueOrDefault(command)?.Function;
            }

            action?.Invoke(splitInput.Length > 1 ? splitInput[1] : string.Empty);
        }
    }

    public static string ReadLine()
    {
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
