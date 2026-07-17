using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Server;
using Xunit;

namespace Server.Tests;

// These tests exercise the pump's cross-thread rendezvous directly, so they block on
// bounded-timeout Task.Wait() calls by design rather than using async/await.
#pragma warning disable xUnit1031
public class ConsoleInputPumpTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    // Deterministic synchronization: spin until a real condition holds (the pump's
    // rendezvous state or the reader's progress) instead of sleeping a fixed interval,
    // which races on slow/loaded CI runners.
    private static void WaitUntil(Func<bool> condition, string message) =>
        Assert.True(SpinWait.SpinUntil(condition, Timeout), message);

    // A TextReader whose ReadLine() blocks until a line is fed, and returns null after Complete().
    private sealed class BlockingTextReader : TextReader
    {
        private readonly BlockingCollection<string> _lines = new();
        private int _readCalls;

        // Number of times ReadLine() has been entered — lets a test wait until the pump's
        // reader loop is blocked waiting for the next line (e.g. after consuming a command).
        public int ReadCallCount => Volatile.Read(ref _readCalls);

        public void Feed(string line) => _lines.Add(line);
        public void Complete() => _lines.CompleteAdding();

        public override string ReadLine()
        {
            Interlocked.Increment(ref _readCalls);
            try
            {
                return _lines.Take();
            }
            catch (InvalidOperationException)
            {
                return null; // CompleteAdding + empty => EOF
            }
        }
    }

    [Fact]
    public void Eof_ends_loop_without_spinning()
    {
        var pump = new ConsoleInputPump(new StringReader(""), _ => null);

        var ran = Task.Run(pump.Run);

        Assert.True(ran.Wait(Timeout), "Run() did not return on EOF");
        Assert.False(pump.Running);
    }

    [Fact]
    public void Recognized_line_dispatches_command()
    {
        var invokedWith = new TaskCompletionSource<string>();
        Action<string> onPing = arg => invokedWith.TrySetResult(arg);

        var pump = new ConsoleInputPump(
            new StringReader("ping hello\n"),
            cmd => cmd == "ping" ? onPing : null
        );

        Task.Run(pump.Run);

        Assert.True(invokedWith.Task.Wait(Timeout), "command not dispatched");
        Assert.Equal("hello", invokedWith.Task.Result);
    }

    [Fact]
    public void Pending_prompt_receives_next_line()
    {
        var reader = new BlockingTextReader();
        var pump = new ConsoleInputPump(reader, _ => null);
        Task.Run(pump.Run);

        // Register the prompt, then wait until it is actually pending before feeding a
        // line — otherwise the line could be consumed as a command before registration.
        var prompt = Task.Run(pump.ReadLine);
        WaitUntil(() => pump.HasPendingPrompt, "prompt did not register");
        reader.Feed("the answer");

        Assert.True(prompt.Wait(Timeout), "prompt did not receive a line");
        Assert.Equal("the answer", prompt.Result);

        reader.Complete();
    }

    [Fact]
    public void Eof_while_prompt_pending_completes_prompt_with_null()
    {
        var reader = new BlockingTextReader();
        var pump = new ConsoleInputPump(reader, _ => null);
        Task.Run(pump.Run);

        var prompt = Task.Run(pump.ReadLine);
        WaitUntil(() => pump.HasPendingPrompt, "prompt did not register");
        reader.Complete(); // EOF while prompt is waiting

        Assert.True(prompt.Wait(Timeout), "prompt hung on EOF");
        Assert.Null(prompt.Result);
        Assert.False(pump.Running);
    }

    [Fact]
    public void Throwing_lookup_does_not_hang_pending_prompt()
    {
        var reader = new BlockingTextReader();
        var pump = new ConsoleInputPump(reader, _ => throw new InvalidOperationException("boom"));
        Task.Run(pump.Run);

        // Wait until the reader loop is blocked on its first read, then feed a bad command.
        WaitUntil(() => reader.ReadCallCount >= 1, "reader did not start");
        reader.Feed("badcommand");

        // Wait until the loop has consumed that command (throwing lookup swallowed) and is
        // blocked waiting for the NEXT line — a second ReadLine() entry. This guarantees
        // the command was dispatched (not delivered to a prompt) before we register one.
        WaitUntil(() => reader.ReadCallCount >= 2, "bad command was not consumed");

        var prompt = Task.Run(pump.ReadLine);
        WaitUntil(() => pump.HasPendingPrompt, "prompt did not register");
        reader.Complete(); // EOF while prompt is waiting

        Assert.True(prompt.Wait(Timeout), "prompt hung after throwing lookup");
        Assert.Null(prompt.Result);
        Assert.False(pump.Running);
    }
}
#pragma warning restore xUnit1031
