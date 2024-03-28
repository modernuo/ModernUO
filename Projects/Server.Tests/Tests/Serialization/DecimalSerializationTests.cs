using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Xunit;

namespace Server.Tests;

public class DecimalSerializationTests
{
    private static void Write(byte[] buffer, decimal value)
    {
        Span<int> bytes = stackalloc int[sizeof(decimal) / 4];
        decimal.GetBits(value, bytes);

        MemoryMarshal.Cast<int, byte>(bytes).CopyTo(buffer.AsSpan());
    }

    private static int ReadInt(ReadOnlySpan<byte> buffer) => BinaryPrimitives.ReadInt32LittleEndian(buffer);

    private static decimal ReadDecimal(ReadOnlySpan<byte> buffer) => new(stackalloc int[4] { ReadInt(buffer), ReadInt(buffer[4..]), ReadInt(buffer[8..]), ReadInt(buffer[12..]) });

    public static TheoryData<decimal> Data =>
        new()
        {
            123.46m,
            0.0256m,
            10000m,
            2m,
            0.0001m,
            0.0000000000000000000000000001m
        };

    [Theory]
    [MemberData(nameof(Data))]
    public void TestSerializeDecimal(decimal value)
    {
        // Arrange
        byte[] buffer = new byte[sizeof(decimal)];

        // Act
        Write(buffer, value);

        // Assert
        Assert.Equal(value, ReadDecimal(buffer));
    }
}
