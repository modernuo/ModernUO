using System;
using System.Buffers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;
using Server.Network;

namespace Benchmarks
{
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    public class BenchmarkPacketBroadcast
    {
        public static int SendUnicodeMessage(
            ArraySegment<byte>[] buffer,
            Serial serial, int graphic, MessageType type, int hue, int font, string lang, string name, string text
        )
        {
            name = name?.Trim() ?? "";
            text = text?.Trim() ?? "";
            lang = lang?.Trim() ?? "ENU";

            if (hue == 0)
            {
                hue = 0x3B2;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0xAE);
            writer.Write((ushort)(50 + text.Length * 2));
            writer.Write(serial);
            writer.Write((short)graphic);
            writer.Write((byte)type);
            writer.Write((short)hue);
            writer.Write((short)font);
            writer.WriteAscii(lang, 4);
            writer.WriteAscii(name, 30);
            writer.WriteBigUniNull(text);

            return writer.Position;
        }

        public static int CreateUnicodeMessage(
            Span<byte> buffer,
            Serial serial, int graphic, MessageType type, int hue, int font, string lang, string name, string text
        )
        {
            name = name?.Trim() ?? "";
            text = text?.Trim() ?? "";
            lang = lang?.Trim() ?? "ENU";

            if (hue == 0)
            {
                hue = 0x3B2;
            }

            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xAE);
            writer.Write((ushort)(50 + text.Length * 2));
            writer.Write(serial);
            writer.Write((short)graphic);
            writer.Write((byte)type);
            writer.Write((short)hue);
            writer.Write((short)font);
            writer.WriteAscii(lang, 4);
            writer.WriteAscii(name, 30);
            writer.WriteBigUniNull(text);

            return writer.Position;
        }

        private Pipe<byte>[] _pipes = new Pipe<byte>[25000];

        [IterationSetup]
        public void SetUp()
        {
            for (var i = 0; i < _pipes.Length; i++)
            {
                _pipes[i] = new Pipe<byte>(new byte[4096]);
            }
        }

        [IterationCleanup]
        public void CleanUp()
        {
            for (var i = 0; i < _pipes.Length; i++)
            {
                _pipes[i] = null;
            }
        }

        [Benchmark]
        public int TestCircularBuffer()
        {
            var text = "This is some really long text that we want to handle. It should take a little bit to encode this.";
            foreach (var pipe in _pipes)
            {
                var result = pipe.Writer.TryGetMemory();
                var length = SendUnicodeMessage(
                    result.Buffer,
                    Serial.MinusOne, -1, MessageType.Regular, 0x3B2, 3, "ENU", "System", text
                );
                pipe.Writer.Advance((uint)length);
            }

            return _pipes.Length;
        }

        [Benchmark]
        public int TestSpanWriterFromBuffer()
        {
            var text = "This is some really long text that we want to handle. It should take a little bit to encode this.";
            foreach (var pipe in _pipes)
            {
                var result = pipe.Writer.TryGetMemory();

                Span<byte> buffer = result.Buffer[0];

                var length = CreateUnicodeMessage(
                    buffer, Serial.MinusOne, -1, MessageType.Regular, 0x3B2, 3, "ENU", "System", text
                );
                pipe.Writer.Advance((uint)length);
            }

            return _pipes.Length;
        }

        [Benchmark]
        public int TestSpanWriter()
        {
            var text = "This is some really long text that we want to handle. It should take a little bit to encode this.";
            Span<byte> buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLength(text)];
            var length = CreateUnicodeMessage(
                buffer, Serial.MinusOne, -1, MessageType.Regular, 0x3B2, 3, "ENU", "System", text
            );

            buffer = buffer[..length];

            foreach (var pipe in _pipes)
            {
                var result = pipe.Writer.TryGetMemory();
                result.CopyFrom(buffer);
                pipe.Writer.Advance((uint)buffer.Length);
            }

            return _pipes.Length;
        }

        private static void SendUnicodeMessageWithSpan(Pipe<byte> pipe, string text)
        {
            Span<byte> buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLength(text)];
            var length = CreateUnicodeMessage(
                buffer, Serial.MinusOne, -1, MessageType.Regular, 0x3B2, 3, "ENU", "System", text
            );

            buffer = buffer[..length];
            var result = pipe.Writer.TryGetMemory();
            result.CopyFrom(buffer);
            pipe.Writer.Advance((uint)buffer.Length);
        }

        [Benchmark]
        public int TestSpanWriterLooped()
        {
            var text = "This is some really long text that we want to handle. It should take a little bit to encode this.";

            foreach (var pipe in _pipes)
            {
                SendUnicodeMessageWithSpan(pipe, text);
            }

            return _pipes.Length;
        }
    }
}
