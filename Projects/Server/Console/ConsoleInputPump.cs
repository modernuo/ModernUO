using System;
using System.IO;
using System.Threading;

namespace Server;

/// <summary>
/// Owns a console input stream. Each line read is atomically either delivered to a
/// waiting <see cref="ReadLine"/> caller (a prompt) or dispatched as a command via the
/// supplied lookup. EOF ends the loop instead of spinning. Correct-by-construction:
/// the prompt-vs-command decision is made under a single lock at the moment a line is read.
/// </summary>
internal sealed class ConsoleInputPump
{
    private readonly TextReader _input;
    private readonly Func<string, Action<string>> _lookup;
    private readonly object _gate = new();
    private readonly AutoResetEvent _promptDelivered = new(false);

    private bool _promptPending;
    private string _promptResult;
    private volatile bool _running = true;

    public ConsoleInputPump(TextReader input, Func<string, Action<string>> lookup)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
    }

    public bool Running => _running;

    public void Stop()
    {
        _running = false;
        ReleasePendingPrompt(null);
    }

    public void Run()
    {
        while (_running && !Core.Closing)
        {
            string line;
            try
            {
                line = _input.ReadLine();
            }
            catch
            {
                break;
            }

            if (line == null)
            {
                break; // EOF — never spin
            }

            bool isCommand;
            lock (_gate)
            {
                if (_promptPending)
                {
                    _promptResult = line;
                    _promptPending = false;
                    _promptDelivered.Set();
                    isCommand = false;
                }
                else
                {
                    isCommand = true;
                }
            }

            if (!isCommand)
            {
                continue;
            }

            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            var split = trimmed.Split(' ', 2);
            var action = _lookup(split[0].ToLower());
            if (action == null)
            {
                continue;
            }

            try
            {
                action(split.Length > 1 ? split[1] : string.Empty);
            }
            catch
            {
                // Command handlers log their own failures; never let one kill the loop.
            }
        }

        _running = false;
        ReleasePendingPrompt(null);
    }

    public string ReadLine()
    {
        lock (_gate)
        {
            if (!_running)
            {
                return null;
            }

            _promptResult = null;
            _promptPending = true;
        }

        _promptDelivered.WaitOne();

        lock (_gate)
        {
            return _promptResult;
        }
    }

    private void ReleasePendingPrompt(string result)
    {
        lock (_gate)
        {
            if (_promptPending)
            {
                _promptResult = result;
                _promptPending = false;
                _promptDelivered.Set();
            }
        }
    }
}
