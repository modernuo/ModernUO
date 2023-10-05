using System;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

public class PipeTests
{
    [Fact]
    public void TestSizeMatchesPageSize()
    {
        var pageSize = (uint)Environment.SystemPageSize;
        using var pipe = new Pipe(128);

        // Available memory should be in increments of system page size minus one.
        Assert.Equal(pageSize, pipe.Size);
        Assert.Equal(pageSize - 1, (uint)pipe.Writer.AvailableToWrite().Length);
    }

    [Fact]
    public void TestWriteReadsWrap()
    {
        var pageSize = (uint)Environment.SystemPageSize;
        using var pipe = new Pipe(pageSize);

        var span = pipe.Writer.AvailableToWrite();

        for (var i = 0; i < span.Length; i++)
        {
            span[i] = (byte)(i % 256);
        }

        pipe.Writer.Advance((uint)(span.Length - 10));

        var readBytes = pipe.Reader.AvailableToRead();

        // Make a sequence from what we expect.
        Span<byte> seq = new byte[readBytes.Length];
        for (var i = 0; i < readBytes.Length; i++)
        {
            seq[i] = (byte)(i % 256);
        }

        AssertThat.Equal(readBytes, seq);

        // Advance by half. Expected writer length should be half + 10
        pipe.Reader.Advance(pageSize / 2);

        span = pipe.Writer.AvailableToWrite();

        var halfStart = pageSize / 2 + 10;

        Assert.Equal(halfStart, (uint)span.Length);

        seq = new byte[20];

        for (var i = 0; i < 10; i++)
        {
            seq[i] = (byte)(0xF5 + i);
        }

        // The last element, the sentinel, is excluded.
        // The wrap around values start at 11
        for (var i = 11; i < 20; i++)
        {
            seq[i] = (byte)(i - 11);
        }

        // Test the uncommitted overwritten memory and shifted offset to make sure the ring is working
        AssertThat.Equal(span[..20], seq[..20]);
    }

    [Fact]
    public void TestWriteReadMatches()
    {
        var pipe = new Pipe(16);

        var reader = pipe.Reader;
        var writer = pipe.Writer;

        for (uint i = 0; i < 16; i++)
        {
            writer.Advance(i);

            Assert.Equal((int)i, reader.AvailableToRead().Length);
            reader.Advance(i);
        }
    }
}
