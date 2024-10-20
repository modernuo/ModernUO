using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Server;

public static class BwtDecompress
{
    public static byte[] Decompress(Stream stream, int length)
    {
        var firstChar = (byte)stream.ReadByte();

        Span<ushort> table = GC.AllocateUninitializedArray<ushort>(256 * 256);
        Span<byte> output = GC.AllocateUninitializedArray<byte>(length);
        BuildTable(table, firstChar);

        var i = 0;
        while (stream.Position < stream.Length)
        {
            var currentValue = firstChar;
            var value = table[currentValue];
            if (currentValue > 0)
            {
                do
                {
                    table[currentValue] = table[currentValue - 1];
                } while (--currentValue > 0);
            }

            table[0] = value;

            output[i++] = (byte)value;
            firstChar = (byte)stream.ReadByte();
        }

        return InternalDecompress(output);
    }

    private static void BuildTable(Span<ushort> table, byte startValue)
    {
        var index = 0;
        var firstByte = startValue;
        byte secondByte = 0;
        for (var i = 0; i < 256 * 256; i++)
        {
            var val = (ushort)(firstByte + (secondByte << 8));
            table[index++] = val;

            firstByte++;
            if (firstByte == 0)
            {
                secondByte++;
            }
        }

        table.Sort();
    }

    private static byte[] InternalDecompress(Span<byte> input)
    {
        Span<byte> symbolTable = stackalloc byte[256];
        Span<byte> frequency = stackalloc byte[256];
        Span<int> partialInput = stackalloc int[256 * 3];

        for (var i = 0; i < 256; i++)
        {
            symbolTable[i] = (byte)i;
        }

        MemoryMarshal.Cast<byte, int>(input)[..256].CopyTo(partialInput);

        var sum = 0;
        for (var i = 0; i < 256; i++)
        {
            sum += partialInput[i];
        }

        var nonZeroCount = 256 - partialInput[..256].Count(0);

        Frequency(partialInput, frequency);

        for (int i = 0, m = 0; i < nonZeroCount; ++i)
        {
            var freq = frequency[i];
            symbolTable[input[m + 1024]] = freq;
            partialInput[freq + 256] = m + 1;
            m += partialInput[freq];
            partialInput[freq + 512] = m;
        }

        var val = symbolTable[0];
        var output = GC.AllocateUninitializedArray<byte>(sum);

        var count = 0;
        do
        {
            ref var firstValRef = ref partialInput[val + 256];
            output[count] = val;

            if (firstValRef >= partialInput[val + 512])
            {
                if (nonZeroCount-- > 0)
                {
                    ShiftLeftSimd(symbolTable, nonZeroCount);
                    val = symbolTable[0];
                }
            }
            else
            {
                var idx = input[firstValRef + 1024];
                firstValRef++;

                if (idx != 0)
                {
                    ShiftLeftSimd(symbolTable, idx);
                    symbolTable[idx] = val;
                    val = symbolTable[0];
                }
            }

            count++;
        } while (count < sum);

        return output;
    }

    private static void Frequency(Span<int> input, Span<byte> output)
    {
        Span<int> tmp = stackalloc int[256];
        input[..256].CopyTo(tmp);

        for (var i = 0; i < 256; i++)
        {
            uint value = 0;
            byte index = 0;

            for (var j = 0; j < 256; j++)
            {
                if (tmp[j] > value)
                {
                    index = (byte)j;
                    value = (uint)tmp[j];
                }
            }

            if (value == 0)
            {
                break;
            }

            output[i] = index;
            tmp[index] = 0;
        }
    }

    private static void ShiftLeftSimd(Span<byte> input, int max)
    {
        var i = 0;
        var vectorSize = Vector<byte>.Count;

        while (i + vectorSize <= max)
        {
            var vector = new Vector<byte>(input[(i + 1)..]);
            vector.CopyTo(input[i..]);
            i += vectorSize;
        }

        while (i < max)
        {
            input[i] = input[i + 1];
            i++;
        }
    }
}
