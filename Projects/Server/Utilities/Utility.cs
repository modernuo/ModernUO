using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using Server.Buffers;
using Server.Collections;
using Server.Random;
using Server.Text;

namespace Server;

public static partial class Utility
{
    private static Dictionary<IPAddress, IPAddress> _ipAddressTable;

    private static readonly Stack<ConsoleColor> m_ConsoleColors = new();

    public static void Separate(StringBuilder sb, string value, string separator)
    {
        if (sb.Length > 0)
        {
            sb.Append(separator);
        }

        sb.Append(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Intern(this string str) => str?.Length > 0 ? string.Intern(str) : str;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Intern(ref string str)
    {
        str = Intern(str);
    }

    public static IPAddress Intern(IPAddress ipAddress)
    {
        if (ipAddress == null)
        {
            return null;
        }

        if (ipAddress.IsIPv4MappedToIPv6)
        {
            ipAddress = ipAddress.MapToIPv4();
        }

        _ipAddressTable ??= new Dictionary<IPAddress, IPAddress>();

        if (!_ipAddressTable.TryGetValue(ipAddress, out var interned))
        {
            interned = ipAddress;
            _ipAddressTable[ipAddress] = interned;
        }

        return interned;
    }

    public static void Intern(ref IPAddress ipAddress)
    {
        ipAddress = Intern(ipAddress);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ApplyCidrMask(ref ulong high, ref ulong low, int prefixLength, bool isMax)
    {
        // This should never happen, a 0 CIDR is not valid
        if (prefixLength == 0)
        {
            high = low = ~0UL;
            return;
        }

        if (prefixLength == 128)
        {
            return;
        }

        if (prefixLength == 64)
        {
            low = 0;
            return;
        }

        if (prefixLength < 64)
        {
            int bitsToFlip = 64 - prefixLength;
            ulong highMask = isMax ? ~0UL >> bitsToFlip : ~0UL << (bitsToFlip + 1);

            high = isMax ? high | highMask : high & highMask;
            low = isMax ? ~0UL : 0UL;
        }
        else
        {
            int bitsToFlip = 128 - prefixLength;
            ulong lowMask = isMax ? ~0UL >> (64 - bitsToFlip) : ~0UL << bitsToFlip;

            low = isMax ? low | lowMask : low & lowMask;
        }
    }

    // Converts an IPAddress to a UInt128 in IPv6 format
    public static UInt128 ToUInt128(this IPAddress ip)
    {
        if (ip.AddressFamily == AddressFamily.InterNetwork && !ip.IsIPv4MappedToIPv6)
        {
            Span<byte> integer = stackalloc byte[4];
            return !ip.TryWriteBytes(integer, out _)
                ? (UInt128)0
                : new UInt128(0, 0xFFFF00000000UL | BinaryPrimitives.ReadUInt32BigEndian(integer));
        }

        Span<byte> bytes = stackalloc byte[16];
        if (!ip.TryWriteBytes(bytes, out _))
        {
            return 0;
        }

        ulong high = BinaryPrimitives.ReadUInt64BigEndian(bytes[..8]);
        ulong low = BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(8, 8));

        return new UInt128(high, low);
    }

    // Converts a UInt128 in IPv6 format to an IPAddress
    public static IPAddress ToIpAddress(this UInt128 value, bool mapToIpv6 = false)
    {
        // IPv4 mapped IPv6 address
        if (!mapToIpv6 && value >= 0xFFFF00000000UL && value <= 0xFFFFFFFFFFFFUL)
        {
            var newAddress = IPAddress.HostToNetworkOrder((int)value);
            return new IPAddress(unchecked((uint)newAddress));
        }

        Span<byte> bytes = stackalloc byte[16]; // 128 bits for IPv6 address
        ((IBinaryInteger<UInt128>)value).WriteBigEndian(bytes);

        return new IPAddress(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt128 CreateCidrAddress(ReadOnlySpan<byte> bytes, int prefixLength, bool isMax)
    {
        ulong high = BinaryPrimitives.ReadUInt64BigEndian(bytes[..8]);
        ulong low = BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(8, 8));

        if (prefixLength < 128)
        {
            ApplyCidrMask(ref high, ref low, prefixLength, isMax);
        }

        return new UInt128(high, low);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteMappedIPv6To(this IPAddress ipAddress, Span<byte> destination)
    {
        if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
        {
            ipAddress.TryWriteBytes(destination, out _);
            return;
        }

        destination[..8].Clear(); // Local init is off
        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(8, 4), 0xFFFF);
        ipAddress.TryWriteBytes(destination.Slice(12, 4), out _);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MatchClassC(this IPAddress ip1, IPAddress ip2) => ip1.MatchCidr(24, ip2);

    public static bool MatchCidr(this IPAddress cidrAddress, int prefixLength, IPAddress address)
    {
        Span<byte> cidrBytes = stackalloc byte[16];
        cidrAddress.WriteMappedIPv6To(cidrBytes);

        if (cidrAddress.AddressFamily != AddressFamily.InterNetworkV6)
        {
            prefixLength += 96; // 32 -> 128
        }

        var min = CreateCidrAddress(cidrBytes, prefixLength, false);
        var max = CreateCidrAddress(cidrBytes, prefixLength, true);
        var ip = address.ToUInt128();

        return ip >= min && ip <= max;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FixHtml(this string str) => ((ReadOnlySpan<char>)str).FixHtml();

    public static string FixHtml(this ReadOnlySpan<char> str)
    {
        if (str.IsNullOrWhiteSpace())
        {
            return str.ToString();
        }

        var chars = STArrayPool<char>.Shared.Rent(str.Length);
        var span = chars.AsSpan(0, str.Length);
        str.CopyTo(span);

        FixHtml(span);

        var fixedStr = span.ToString();
        STArrayPool<char>.Shared.Return(chars);
        return fixedStr;
    }

    public static void FixHtml(this Span<char> chars)
    {
        if (chars.Length == 0)
        {
            return;
        }

        ReadOnlySpan<char> invalid = ['<', '>', '#'];
        ReadOnlySpan<char> replacement = ['(', ')', '-'];

        chars.ReplaceAny(invalid, replacement);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PooledArraySpanFormattable FixHtmlFormattable(this string str) =>
        ((ReadOnlySpan<char>)str).FixHtmlFormattable();

    public static PooledArraySpanFormattable FixHtmlFormattable(this ReadOnlySpan<char> str)
    {
        var chars = STArrayPool<char>.Shared.Rent(str.Length);
        var span = chars.AsSpan(0, str.Length);
        str.CopyTo(span);

        var formattable = new PooledArraySpanFormattable(chars, str.Length);

        if (!str.IsNullOrWhiteSpace())
        {
            FixHtml(span);
        }

        return formattable;
    }

    public static Direction GetDirection(Point3D from, Point3D to) => GetDirection(from.X, from.Y, to.X, to.Y);

    public static Direction GetDirection(Point2D from, Point2D to) => GetDirection(from.X, from.Y, to.X, to.Y);

    public static Direction GetDirection(int fromX, int fromY, int toX, int toY)
    {
        var dx = toX - fromX;
        var dy = toY - fromY;

        var adx = Abs(dx);
        var ady = Abs(dy);

        if (adx >= ady * 3)
        {
            return dx > 0 ? Direction.East : Direction.West;
        }

        if (ady >= adx * 3)
        {
            return dy > 0 ? Direction.South : Direction.North;
        }

        if (dx > 0)
        {
            return dy > 0 ? Direction.Down : Direction.Right;
        }

        return dy > 0 ? Direction.Left : Direction.Up;
    }

    public static object GetArrayCap(Array array, int index, object emptyValue = null) =>
        array.Length > 0 ? array.GetValue(Math.Clamp(index, 0, array.Length - 1)) : emptyValue;

    public static void FixPoints(ref Point3D top, ref Point3D bottom)
    {
        if (bottom.m_X < top.m_X)
        {
            (top.m_X, bottom.m_X) = (bottom.m_X, top.m_X);
        }

        if (bottom.m_Y < top.m_Y)
        {
            (top.m_Y, bottom.m_Y) = (bottom.m_Y, top.m_Y);
        }

        if (bottom.m_Z < top.m_Z)
        {
            (top.m_Z, bottom.m_Z) = (bottom.m_Z, top.m_Z);
        }
    }

    public static void FormatBuffer(this TextWriter op, ReadOnlySpan<byte> data)
    {
        op.WriteLine("        0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F");
        op.WriteLine("       -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --");

        var totalLength = data.Length;
        if (totalLength <= 0)
        {
            op.WriteLine("0000   ");
            return;
        }

        Span<byte> lineBytes = stackalloc byte[16];
        Span<char> lineChars = stackalloc char[47];
        for (var i = 0; i < totalLength; i += 16)
        {
            var length = Math.Min(data.Length - i, 16);
            data.Slice(i, length).CopyTo(lineBytes);

            var charsWritten = ((ReadOnlySpan<byte>)lineBytes[..length]).ToSpacedHexString(lineChars);

            op.Write("{0:X4}   ", i);
            op.Write(lineChars[..charsWritten]);
            op.WriteLine();
        }
    }

    public static void PushColor(ConsoleColor color)
    {
        try
        {
            m_ConsoleColors.Push(Console.ForegroundColor);
            Console.ForegroundColor = color;
        }
        catch
        {
            // ignored
        }
    }

    public static void PopColor()
    {
        try
        {
            Console.ForegroundColor = m_ConsoleColors.Pop();
        }
        catch
        {
            // ignored
        }
    }

    public static bool NumberBetween(double num, int bound1, int bound2, double allowance)
    {
        if (bound1 > bound2)
        {
            (bound1, bound2) = (bound2, bound1);
        }

        return num < bound2 + allowance && num > bound1 - allowance;
    }

    public static void AssignRandomHair(Mobile m, int hue)
    {
        m.HairItemID = m.Race.RandomHair(m);
        m.HairHue = hue;
    }

    public static void AssignRandomHair(Mobile m, bool randomHue = true)
    {
        m.HairItemID = m.Race.RandomHair(m);

        if (randomHue)
        {
            m.HairHue = m.Race.RandomHairHue();
        }
    }

    public static void AssignRandomFacialHair(Mobile m, int hue)
    {
        m.FacialHairItemID = m.Race.RandomFacialHair(m);
        m.FacialHairHue = hue;
    }

    public static void AssignRandomFacialHair(Mobile m, bool randomHue = true)
    {
        m.FacialHairItemID = m.Race.RandomFacialHair(m);

        if (randomHue)
        {
            m.FacialHairHue = m.Race.RandomHairHue();
        }
    }

    // Using this instead of Linq Cast<> means we can ditch the yield and enforce contravariance
    public static HashSet<TOutput> SafeConvertSet<TInput, TOutput>(this IEnumerable<TInput> coll)
        where TOutput : TInput => coll.SafeConvert<HashSet<TOutput>, TInput, TOutput>();

    public static List<TOutput> SafeConvertList<TInput, TOutput>(this IEnumerable<TInput> coll)
        where TOutput : TInput => coll.SafeConvert<List<TOutput>, TInput, TOutput>();

    public static TColl SafeConvert<TColl, TInput, TOutput>(this IEnumerable<TInput> coll)
        where TOutput : TInput where TColl : ICollection<TOutput>, new()
    {
        var outputList = new TColl();

        foreach (var entry in coll)
        {
            if (entry is TOutput outEntry)
            {
                outputList.Add(outEntry);
            }
        }

        return outputList;
    }

    // ToArray returns an array containing the contents of the List.
    // This requires copying the List, which is an O(n) operation.
    public static List<R> ToList<T, R>(this PooledRefList<T> poolList) where T : R
    {
        var size = poolList.Count;
        var items = poolList._items;

        var list = new List<R>(size);
        if (size == 0)
        {
            return list;
        }

        for (var i = 0; i < size; i++)
        {
            list.Add(items[i]);
        }

        return list;
    }

    public static bool ToBoolean(string value) =>
        bool.TryParse(value, out var b)
            ? b
            : value.InsensitiveEquals("enabled") || value.InsensitiveEquals("on");

    public static double ToDouble(string value)
    {
#pragma warning disable CA1806 // Do not ignore method results
        double.TryParse(value, out var d);
#pragma warning restore CA1806 // Do not ignore method results

        return d;
    }

    public static TimeSpan ToTimeSpan(string value)
    {
#pragma warning disable CA1806 // Do not ignore method results
        TimeSpan.TryParse(value, out var t);
#pragma warning restore CA1806 // Do not ignore method results

        return t;
    }

    public static int ToInt32(ReadOnlySpan<char> value)
    {
        int i;

#pragma warning disable CA1806 // Do not ignore method results
        if (value.StartsWithOrdinal("0x"))
        {
            int.TryParse(value[2..], NumberStyles.HexNumber, null, out i);
        }
        else
        {
            int.TryParse(value, out i);
        }
#pragma warning restore CA1806 // Do not ignore method results

        return i;
    }

    public static uint ToUInt32(ReadOnlySpan<char> value)
    {
        uint i;

#pragma warning disable CA1806 // Do not ignore method results
        if (value.InsensitiveStartsWith("0x"))
        {
            uint.TryParse(value[2..], NumberStyles.HexNumber, null, out i);
        }
        else
        {
            uint.TryParse(value, out i);
        }
#pragma warning restore CA1806 // Do not ignore method results

        return i;
    }

    public static bool ToInt32(ReadOnlySpan<char> value, out int i) =>
        value.InsensitiveStartsWith("0x")
            ? int.TryParse(value[2..], NumberStyles.HexNumber, null, out i)
            : int.TryParse(value, out i);

    public static bool ToUInt32(ReadOnlySpan<char> value, out uint i) =>
        value.InsensitiveStartsWith("0x")
            ? uint.TryParse(value[2..], NumberStyles.HexNumber, null, out i)
            : uint.TryParse(value, out i);

    public static int GetXMLInt32(string intString, int defaultValue)
    {
        try
        {
            return XmlConvert.ToInt32(intString);
        }
        catch
        {
            return int.TryParse(intString, out var val) ? val : defaultValue;
        }
    }

    public static uint GetXMLUInt32(string uintString, uint defaultValue)
    {
        try
        {
            return XmlConvert.ToUInt32(uintString);
        }
        catch
        {
            return uint.TryParse(uintString, out var val) ? val : defaultValue;
        }
    }

    public static DateTime GetXMLDateTime(string dateTimeString, DateTime defaultValue)
    {
        try
        {
            return XmlConvert.ToDateTime(dateTimeString, XmlDateTimeSerializationMode.Utc);
        }
        catch
        {
            return DateTime.TryParse(dateTimeString, out var d) ? d : defaultValue;
        }
    }

    public static TimeSpan GetXMLTimeSpan(string timeSpanString, TimeSpan defaultValue)
    {
        try
        {
            return XmlConvert.ToTimeSpan(timeSpanString);
        }
        catch
        {
            return defaultValue;
        }
    }

    public static string GetAttribute(XmlElement node, string attributeName, string defaultValue = null) =>
        node?.Attributes[attributeName]?.Value ?? defaultValue;

    public static string GetText(XmlElement node, string defaultValue) => node?.InnerText ?? defaultValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InRange(int p1X, int p1Y, int p2X, int p2Y, int range) =>
        p1X >= p2X - range
        && p1X <= p2X + range
        && p1Y >= p2Y - range
        && p1Y <= p2Y + range;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InRange(Point2D p1, Point2D p2, int range) =>
        InRange(p1.m_X, p1.m_Y, p2.m_X, p2.m_Y, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InUpdateRange(Point2D p1, Point2D p2) => InRange(p1, p2, Core.GlobalUpdateRange);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InRange(Point3D p1, Point3D p2, int range) =>
        InRange(p1.m_X, p1.m_Y, p2.m_X, p2.m_Y, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InUpdateRange(Point3D p1, Point3D p2) => InRange(p1, p2, Core.GlobalUpdateRange);

    // Optimized method for handling 50% random chances in succession up to a maximum
    public static int CoinFlips(int amount, int maximum)
    {
        var heads = 0;
        while (amount > 0)
        {
            // Range is 2^amount exclusively, maximum of 62 bits can be used
            ulong num = amount >= 62
                ? (ulong)BuiltInRng.NextLong()
                : (ulong)BuiltInRng.Next(1L << amount);

            heads += BitOperations.PopCount(num);

            if (heads >= maximum)
            {
                return maximum;
            }

            // 64 bits minus sign bit and exclusive maximum leaves 62 bits
            amount -= 62;
        }

        return heads;
    }

    public static int CoinFlips(int amount)
    {
        var heads = 0;
        while (amount > 0)
        {
            // Range is 2^amount exclusively, maximum of 62 bits can be used
            ulong num = amount >= 62
                ? (ulong)BuiltInRng.NextLong()
                : (ulong)BuiltInRng.Next(1L << amount);

            heads += BitOperations.PopCount(num);

            // 64 bits minus sign bit and exclusive maximum leaves 62 bits
            amount -= 62;
        }

        return heads;
    }

    public static int Dice(int amount, int sides, int bonus)
    {
        if (amount <= 0 || sides <= 0)
        {
            return 0;
        }

        int total;

        if (sides == 2)
        {
            total = CoinFlips(amount);
        }
        else
        {
            total = 0;
            for (var i = 0; i < amount; ++i)
            {
                total += BuiltInRng.Next(1, sides);
            }
        }

        return total + bonus;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Shuffle<T>(this T[] array) => BuiltInRng.Generator.Shuffle(array);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Shuffle<T>(this List<T> list) => BuiltInRng.Generator.Shuffle(CollectionsMarshal.AsSpan(list));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Shuffle<T>(this Span<T> span) => BuiltInRng.Generator.Shuffle(span);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Shuffle<T>(this PooledRefList<T> list) =>
        BuiltInRng.Generator.Shuffle(list._items.AsSpan(0, list.Count));

    /**
     * Gets a random sample from the source list.
     * Not meant for unbounded lists. Does not shuffle or modify source.
     */
    public static T[] RandomSample<T>(this T[] source, int count)
    {
        if (count <= 0)
        {
            return Array.Empty<T>();
        }

        var length = source.Length;
        Span<bool> list = stackalloc bool[length];
        list.Clear();

        var sampleList = new T[count];

        var i = 0;
        do
        {
            var rand = Random(length);
            if (!list[rand])
            {
                list[rand] = true;
                sampleList[i++] = source[rand];
            }
        } while (i < count);

        return sampleList;
    }

    public static List<T> RandomSample<T>(this List<T> source, int count)
    {
        if (count <= 0)
        {
            return [];
        }

        var length = source.Count;
        Span<bool> list = stackalloc bool[length];
        list.Clear();

        var sampleList = new List<T>(count);

        var i = 0;
        do
        {
            var rand = Random(length);
            if (!list[rand])
            {
                list[rand] = true;
                sampleList[i++] = source[rand];
            }
        } while (i < count);

        return sampleList;
    }

    public static void RandomSample<T>(this T[] source, int count, List<T> dest)
    {
        if (count <= 0)
        {
            return;
        }

        var length = source.Length;
        Span<bool> list = stackalloc bool[length];
        list.Clear();

        var i = 0;
        do
        {
            var rand = Random(length);
            if (!list[rand])
            {
                list[rand] = true;
                dest.Add(source[rand]);
            }
        } while (++i < count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T RandomList<T>(params ReadOnlySpan<T> list) => list.RandomElement();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T RandomElement<T>(this ReadOnlySpan<T> list) => list.Length == 0 ? default : list[Random(list.Length)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T RandomElement<T>(this T[] list) => list.RandomElement(default);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T RandomElement<T>(this IList<T> list) => list.RandomElement(default);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WeightedValue<T> RandomWeightedElement<T>(this IList<WeightedValue<T>> weightedValues)
    {
        var totalWeight = 0;
        for (var i = 0; i < weightedValues.Count; i++)
        {
            totalWeight += weightedValues[i].Weight;
        }

        return RandomWeightedElement(weightedValues, totalWeight);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WeightedValue<T> RandomWeightedElement<T>(this ReadOnlySpan<WeightedValue<T>> weightedValues, int totalWeight)
    {
        var random = Random(totalWeight);

        for (var i = 0; i < weightedValues.Length; i++)
        {
            var weightedValue = weightedValues[i];
            random -= weightedValue.Weight;

            if (random <= 0)
            {
                return weightedValue;
            }
        }

        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WeightedValue<T> RandomWeightedElement<T>(this ReadOnlySpan<WeightedValue<T>> weightedValues)
    {
        var totalWeight = 0;
        for (var i = 0; i < weightedValues.Length; i++)
        {
            totalWeight += weightedValues[i].Weight;
        }

        return RandomWeightedElement(weightedValues, totalWeight);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WeightedValue<T> RandomWeightedElement<T>(this IList<WeightedValue<T>> weightedValues, int totalWeight)
    {
        var random = Random(totalWeight);

        for (var i = 0; i < weightedValues.Count; i++)
        {
            var weightedValue = weightedValues[i];
            random -= weightedValue.Weight;

            if (random <= 0)
            {
                return weightedValue;
            }
        }

        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WeightedValue<T> RandomWeightedElement<T>(this PooledRefList<WeightedValue<T>> weightedValues)
    {
        var totalWeight = 0;
        for (var i = 0; i < weightedValues.Count; i++)
        {
            totalWeight += weightedValues[i].Weight;
        }

        return RandomWeightedElement(weightedValues, totalWeight);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WeightedValue<T> RandomWeightedElement<T>(
        this PooledRefList<WeightedValue<T>> weightedValues,
        int totalWeight
    )
    {
        var random = Random(totalWeight);

        for (var i = 0; i < weightedValues.Count; i++)
        {
            var weightedValue = weightedValues[i];
            random -= weightedValue.Weight;

            if (random <= 0)
            {
                return weightedValue;
            }
        }

        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T TakeRandomElement<T>(this IList<T> list)
    {
        if (list.Count == 0)
        {
            return default;
        }

        var index = Random(list.Count);
        var value = list[index];
        list.RemoveAt(index);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T RandomElement<T>(this T[] list, T valueIfZero) =>
        list.Length == 0 ? valueIfZero : list[Random(list.Length)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T RandomElement<T>(this IList<T> list, T valueIfZero) =>
        list.Count == 0 ? valueIfZero : list[Random(list.Count)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RandomBool() => BuiltInRng.Next(2) == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan RandomMinMax(TimeSpan min, TimeSpan max) => new(RandomMinMax(min.Ticks, max.Ticks));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double RandomMinMax(double min, double max)
    {
        if (min > max)
        {
            (min, max) = (max, min);
        }
        else if (min == max)
        {
            return min;
        }

        return min + BuiltInRng.NextDouble() * (max - min);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int RandomMinMax(int min, int max)
    {
        if (min > max)
        {
            (min, max) = (max, min);
        }
        else if (min == max)
        {
            return min;
        }

        return min + BuiltInRng.Next(max - min + 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long RandomMinMax(long min, long max)
    {
        if (min > max)
        {
            (min, max) = (max, min);
        }
        else if (min == max)
        {
            return min;
        }

        return min + BuiltInRng.Next(max - min + 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Random(int from, int count) => BuiltInRng.Next(from, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Random(int count) => count < 0 ? -BuiltInRng.Next(-count) : BuiltInRng.Next(count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Random(long from, long count) => BuiltInRng.Next(from, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Random(long count) => count < 0 ? -BuiltInRng.Next(-count) : BuiltInRng.Next(count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RandomBytes(Span<byte> buffer) => BuiltInRng.NextBytes(buffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double RandomDouble() => BuiltInRng.NextDouble();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point3D RandomPointIn(Rectangle2D rect, Map map)
    {
        var x = Random(rect.X, rect.Width);
        var y = Random(rect.Y, rect.Height);

        return new Point3D(x, y, map.GetAverageZ(x, y));
    }

    /// <summary>
    ///     Random pink, blue, green, orange, red or yellow hue
    /// </summary>
    public static int RandomNondyedHue()
    {
        return Random(6) switch
        {
            0 => RandomPinkHue(),
            1 => RandomBlueHue(),
            2 => RandomGreenHue(),
            3 => RandomOrangeHue(),
            4 => RandomRedHue(),
            5 => RandomYellowHue(),
            _ => 0
        };
    }

    /// <summary>
    ///     Random hue in the range 1201-1254
    /// </summary>
    public static int RandomPinkHue() => Random(1201, 54);

    /// <summary>
    ///     Random hue in the range 1301-1354
    /// </summary>
    public static int RandomBlueHue() => Random(1301, 54);

    /// <summary>
    ///     Random hue in the range 1401-1454
    /// </summary>
    public static int RandomGreenHue() => Random(1401, 54);

    /// <summary>
    ///     Random hue in the range 1501-1554
    /// </summary>
    public static int RandomOrangeHue() => Random(1501, 54);

    /// <summary>
    ///     Random hue in the range 1601-1654
    /// </summary>
    public static int RandomRedHue() => Random(1601, 54);

    /// <summary>
    ///     Random hue in the range 1701-1754
    /// </summary>
    public static int RandomYellowHue() => Random(1701, 54);

    /// <summary>
    ///     Random hue in the range 1801-1908
    /// </summary>
    public static int RandomNeutralHue() => Random(1801, 108);

    /// <summary>
    ///     Random hue in the range 2001-2018
    /// </summary>
    public static int RandomSnakeHue() => Random(2001, 18);

    /// <summary>
    ///     Random hue in the range 2101-2130
    /// </summary>
    public static int RandomBirdHue() => Random(2101, 30);

    /// <summary>
    ///     Random hue in the range 2201-2224
    /// </summary>
    public static int RandomSlimeHue() => Random(2201, 24);

    /// <summary>
    ///     Random hue in the range 2301-2318
    /// </summary>
    public static int RandomAnimalHue() => Random(2301, 18);

    /// <summary>
    ///     Random hue in the range 2401-2430
    /// </summary>
    public static int RandomMetalHue() => Random(2401, 30);

    public static int ClipDyedHue(int hue) => hue < 2 ? 2 :
        hue > 1001 ? 1001 : hue;

    /// <summary>
    ///     Random hue in the range 2-1001
    /// </summary>
    public static int RandomDyedHue() => Random(2, 1000);

    /// <summary>
    ///     Random hue from 0x62, 0x71, 0x03, 0x0D, 0x13, 0x1C, 0x21, 0x30, 0x37, 0x3A, 0x44, 0x59
    /// </summary>
    public static int RandomBrightHue() =>
        RandomDouble() < 0.1
            ? RandomList(0x62, 0x71)
            : RandomList(0x03, 0x0D, 0x13, 0x1C, 0x21, 0x30, 0x37, 0x3A, 0x44, 0x59);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T> =>
        val.CompareTo(min) < 0 ? min :
        val.CompareTo(max) > 0 ? max : val;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Min<T>(T val, T min) where T : IComparable<T> => val.CompareTo(min) < 0 ? val : min;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Max<T>(T val, T max) where T : IComparable<T> => val.CompareTo(max) > 0 ? val : max;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Tidy<T>(this List<T> list) where T : ISerializable
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var entry = list[i];
            if (entry?.Deleted != false)
            {
                list.RemoveAt(i);
            }
        }

        list.TrimExcess();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Tidy<T>(this HashSet<T> set) where T : ISerializable
    {
        set.RemoveWhere(entry => entry?.Deleted != false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Tidy<K, V>(this Dictionary<K, V> dictionary)
    {
        var serializable = typeof(ISerializable);
        var serializableKey = typeof(K).IsAssignableTo(serializable);
        var serializableValue = typeof(V).IsAssignableTo(serializable);

        if (!serializableKey && !serializableValue)
        {
            return;
        }

        using var queue = PooledRefQueue<K>.Create();
        foreach (var (key, value) in dictionary)
        {
            if (serializableKey)
            {
                if (key == null || ((ISerializable)key).Deleted)
                {
                    queue.Enqueue(key);
                }
            }
            else
            {
                if (value == null || ((ISerializable)value).Deleted)
                {
                    queue.Enqueue(key);
                }
            }
        }

        while (queue.Count > 0)
        {
            dictionary.Remove(queue.Dequeue());
        }

        dictionary.TrimExcess();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NumberOfSetBits(this ulong i)
    {
        i -= (i >> 1) & 0x5555555555555555UL;
        i = (i & 0x3333333333333333UL) + ((i >> 2) & 0x3333333333333333UL);
        return (int)(unchecked(((i + (i >> 4)) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >> 56);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Abs(this int value)
    {
        int mask = value >> 31;
        return (value + mask) ^ mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetTimeStamp() => Core.Now.ToTimeStamp();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToTimeStamp(this DateTime dt) => dt.ToString("yyyy-MM-dd-HH-mm-ss");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<T>(ref List<T> list, T value)
    {
        list ??= [];
        list.Add(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<T>(ref HashSet<T> set, T value)
    {
        set ??= [];
        set.Add(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<K, V>(ref Dictionary<K, V> dict, K key, V value)
    {
        dict ??= new Dictionary<K, V>();
        dict.Add(key, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Remove<T>(ref List<T> list, T value)
    {
        if (list?.Remove(value) != true)
        {
            return false;
        }

        if (list.Count == 0)
        {
            list = null;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Remove<T>(ref HashSet<T> set, T value)
    {
        if (set?.Remove(value) != true)
        {
            return false;
        }

        if (set.Count == 0)
        {
            set = null;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Remove<K, V>(ref Dictionary<K, V> dict, K key)
    {
        if (dict?.Remove(key) != true)
        {
            return false;
        }

        if (dict.Count == 0)
        {
            dict = null;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Remove<K, V>(ref Dictionary<K, V> dict, K key, out V value)
    {
        if (dict?.Remove(key, out value) != true)
        {
            value = default;
            return false;
        }

        if (dict.Count == 0)
        {
            dict = null;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Replace<T>(ref List<T> list, T oldValue, T newValue)
    {
        if (oldValue != null && newValue != null)
        {
            var index = list?.IndexOf(oldValue) ?? -1;

            if (index >= 0)
            {
                list![index] = newValue;
            }
            else
            {
                Add(ref list, newValue);
            }
        }
        else if (oldValue != null)
        {
            Remove(ref list, oldValue);
        }
        else if (newValue != null)
        {
            Add(ref list, newValue);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Replace<K, V>(ref Dictionary<K, V> dict, K key, V oldValue, V newValue)
    {
        if (newValue != null)
        {
            Add(ref dict, key, newValue);
        }
        else if (oldValue != null)
        {
            Remove(ref dict, key);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear<T>(ref List<T> list)
    {
        list.Clear();
        list = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear<T>(ref HashSet<T> set)
    {
        set.Clear();
        set = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear<K, V>(ref Dictionary<K, V> dict)
    {
        dict.Clear();
        dict = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DateToComponents(
        DateTime date,
        out int year,
        out int month,
        out int day,
        out DayOfWeek dayOfWeek,
        out int hour,
        out int min,
        out int sec
    )
    {
        year = date.Year;
        month = date.Month;
        day = date.Day;
        dayOfWeek = date.DayOfWeek;
        hour = date.Hour;
        min = date.Minute;
        sec = date.Second;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] Combine<T>(this IList<T> source, params ReadOnlySpan<IList<T>> arrays) =>
        source.Combine(false, arrays);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] CombinePooled<T>(this IList<T> source, params ReadOnlySpan<IList<T>> arrays) =>
        source.Combine(true, arrays);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] Combine<T>(this IList<T> source, bool pooled, params ReadOnlySpan<IList<T>> arrays)
    {
        var totalLength = source.Count;
        foreach (var arr in arrays)
        {
            totalLength += arr.Count;
        }

        if (totalLength == 0)
        {
            return Array.Empty<T>();
        }

        var combined = pooled ? STArrayPool<T>.Shared.Rent(totalLength) : new T[totalLength];

        source.CopyTo(combined, 0);
        var position = source.Count;
        foreach (var arr in arrays)
        {
            arr.CopyTo(combined, position);
            position += arr.Count;
        }

        return combined;
    }

    public static Point3D GetValidLocation(Map map, Point3D center, int range, int retries = 10)
    {
        if (map == null)
        {
            return center;
        }

        var loc = new Point3D(center.Z, center.Y, center.Z);

        for (var i = 0; i < retries; i++)
        {
            loc.X = center.X + (Random(range * 2 + 1) - range);
            loc.Y = center.Y + (Random(range * 2 + 1) - range);
            loc.Z = center.Z;

            if (map.CanSpawnMobile(loc))
            {
                return loc;
            }

            loc.Z = map.GetAverageZ(loc.X, loc.Y);

            if (map.CanSpawnMobile(loc))
            {
                return loc;
            }
        }

        return center;
    }

    public static Point3D GetValidLocationInLOS(Map map, Mobile from, int range, int retries = 10)
    {
        if (map == null)
        {
            return from.Location;
        }

        var center = from.Location;
        var loc = center;

        for (var i = 0; i < retries; i++)
        {
            loc.X = center.X + (Random(range * 2 + 1) - range);
            loc.Y = center.Y + (Random(range * 2 + 1) - range);
            loc.Z = center.Z;

            if (map.CanSpawnMobile(loc) && map.LineOfSight(from, loc))
            {
                return loc;
            }

            loc.Z = map.GetAverageZ(loc.X, loc.Y);

            if (map.CanSpawnMobile(loc) && map.LineOfSight(from, loc))
            {
                return loc;
            }
        }

        return center;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetUnderlyingNumericBitLength(this TypeCode typeCode) =>
        typeCode switch
        {
            TypeCode.Byte   => 8,
            TypeCode.SByte  => 8,
            TypeCode.Int16  => 16,
            TypeCode.UInt16 => 16,
            TypeCode.Char   => 16,
            TypeCode.Int32  => 32,
            TypeCode.UInt32 => 32,
            TypeCode.Int64  => 64,
            TypeCode.UInt64 => 64,
            _               => 64
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrWhiteSpace(this ReadOnlySpan<char> span) => span.IsEmpty || span.IsWhiteSpace();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InTypeList<T>(this T obj, Type[] types) => obj.GetType().InTypeList(types);

    public static bool InTypeList(this Type t, Type[] types)
    {
        for (var i = 0; i < types.Length; ++i)
        {
            if (types[i].IsAssignableFrom(t))
            {
                return true;
            }
        }

        return false;
    }

    public static int C16232(this int c16)
    {
        c16 &= 0x7FFF;

        var r = ((c16 >> 10) & 0x1F) << 3;
        var g = ((c16 >> 05) & 0x1F) << 3;
        var b = (c16 & 0x1F) << 3;

        return (r << 16) | (g << 8) | b;
    }

    public static int C16216(this int c16) => c16 & 0x7FFF;

    public static int C32216(this int c32)
    {
        c32 &= 0xFFFFFF;

        var r = ((c32 >> 16) & 0xFF) >> 3;
        var g = ((c32 >> 08) & 0xFF) >> 3;
        var b = (c32 & 0xFF) >> 3;

        return (r << 10) | (g << 5) | b;
    }

    public static void AddOrUpdate<TKey, TValue>(this ConditionalWeakTable<TKey, TValue> table, TKey key, TValue value)
        where TKey : class
        where TValue : class
    {
        table.Remove(key);
        table.Add(key, value);
    }

    public static DateTime LocalToUtc(this DateTime local, TimeZoneInfo tz)
    {
        if (tz.IsAmbiguousTime(local))
        {
            var offsets = tz.GetAmbiguousTimeOffsets(local);
            return DateTime.SpecifyKind(local - offsets[1], DateTimeKind.Utc);
        }

        return DateTime.SpecifyKind(local - tz.GetUtcOffset(local), DateTimeKind.Utc);
    }
}
