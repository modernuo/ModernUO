using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
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
    // A TextReader whose ReadLine() blocks until a line is fed, and returns null after Complete().
    private sealed class BlockingTextReader : TextReader
    {
        private readonly BlockingCollection<string> _lines = new();

        public void Feed(string line) => _lines.Add(line);
        public void Complete() => _lines.CompleteAdding();

        public override string ReadLine()
        {
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

        Assert.True(ran.Wait(TimeSpan.FromSeconds(2)), "Run() did not return on EOF");
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

        Assert.True(invokedWith.Task.Wait(TimeSpan.FromSeconds(2)), "command not dispatched");
        Assert.Equal("hello", invokedWith.Task.Result);
    }

    [Fact]
    public void Pending_prompt_receives_next_line()
    {
        var reader = new BlockingTextReader();
        var pump = new ConsoleInputPump(reader, _ => null);
        Task.Run(pump.Run);

        var prompt = Task.Run(pump.ReadLine);
        // Give the prompt a moment to register, then feed a line.
        Thread.Sleep(50);
        reader.Feed("the answer");

        Assert.True(prompt.Wait(TimeSpan.FromSeconds(2)), "prompt did not receive a line");
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
        Thread.Sleep(50);
        reader.Complete(); // EOF while prompt is waiting

        Assert.True(prompt.Wait(TimeSpan.FromSeconds(2)), "prompt hung on EOF");
        Assert.Null(prompt.Result);
        Assert.False(pump.Running);
    }
}
#pragma warning restore xUnit1031
