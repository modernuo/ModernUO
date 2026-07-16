using System;
using System.IO;
using System.Threading;
using Server.Logging;

namespace Server;

/// <summary>
/// Owns a console input stream. Each line read is atomically either delivered to a
/// waiting <see cref="ReadLine"/> caller (a prompt) or dispatched as a command via the
/// supplied lookup. EOF ends the loop instead of spinning. Correct-by-construction:
/// the prompt-vs-command decision is made under a single lock at the moment a line is read.
/// Command handlers dispatched by <see cref="Run"/> execute on the reader thread and
/// must never call <see cref="ReadLine"/> — doing so would deadlock the pump (the reader
/// thread would be blocked waiting on itself to read the next line).
/// </summary>
internal sealed class ConsoleInputPump
{
    private readonly TextReader _input;
    private readonly Func<string, Action<string>> _lookup;
    private readonly Server.Logging.ILogger _logger;
    private readonly object _gate = new();
    private readonly AutoResetEvent _promptDelivered = new(false);

    private bool _promptPending;
    private string _promptResult;
    private volatile bool _running = true;

    public ConsoleInputPump(TextReader input, Func<string, Action<string>> lookup, Server.Logging.ILogger logger = null)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
        _logger = logger;
    }

    public bool Running => _running;

    // Test-observable: true while a ReadLine() caller is registered and waiting for the
    // next line. Lets tests synchronize on the rendezvous state instead of sleeping.
    internal bool HasPendingPrompt
    {
        get
        {
            lock (_gate)
            {
                return _promptPending;
            }
        }
    }

    public void Run()
    {
        try
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
                    _logger?.Warning("Console commands have been disabled due to an error.");
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

                try
                {
                    var action = _lookup(split[0].ToLower());
                    action?.Invoke(split.Length > 1 ? split[1] : string.Empty);
                }
                catch (Exception e)
                {
                    _logger?.Error(e, "Failed to execute console command: {Command}", line);
                }
            }
        }
        finally
        {
            _running = false;
            ReleasePendingPrompt(null);
        }
    }

    /// <summary>
    /// Blocks the calling thread until the next console line is available, or until the
    /// pump stops (returning <c>null</c>). Intended for a single, sequential caller at a
    /// time — ModernUO's console prompts run one after another during startup/steps.
    /// Concurrent callers are not supported: a second caller overlapping with a pending
    /// prompt will race with it for the next line. Must not be called from the reader
    /// thread (i.e. from within a command handler dispatched by <see cref="Run"/>), as
    /// that would deadlock the pump.
    /// </summary>
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
