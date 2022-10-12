using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using Microsoft.Toolkit.HighPerformance;
using Server.Buffers;
using Server.Collections;
using Server.Logging;
using Server.Random;
using Server.Text;

namespace Server;

public static class Utility
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(Utility));

    private static Dictionary<IPAddress, IPAddress> _ipAddressTable;

    private static SkillName[] _allSkills =
    {
        SkillName.Alchemy,
        SkillName.Anatomy,
        SkillName.AnimalLore,
        SkillName.ItemID,
        SkillName.ArmsLore,
        SkillName.Parry,
        SkillName.Begging,
        SkillName.Blacksmith,
        SkillName.Fletching,
        SkillName.Peacemaking,
        SkillName.Camping,
        SkillName.Carpentry,
        SkillName.Cartography,
        SkillName.Cooking,
        SkillName.DetectHidden,
        SkillName.Discordance,
        SkillName.EvalInt,
        SkillName.Healing,
        SkillName.Fishing,
        SkillName.Forensics,
        SkillName.Herding,
        SkillName.Hiding,
        SkillName.Provocation,
        SkillName.Inscribe,
        SkillName.Lockpicking,
        SkillName.Magery,
        SkillName.MagicResist,
        SkillName.Tactics,
        SkillName.Snooping,
        SkillName.Musicianship,
        SkillName.Poisoning,
        SkillName.Archery,
        SkillName.SpiritSpeak,
        SkillName.Stealing,
        SkillName.Tailoring,
        SkillName.AnimalTaming,
        SkillName.TasteID,
        SkillName.Tinkering,
        SkillName.Tracking,
        SkillName.Veterinary,
        SkillName.Swords,
        SkillName.Macing,
        SkillName.Fencing,
        SkillName.Wrestling,
        SkillName.Lumberjacking,
        SkillName.Mining,
        SkillName.Meditation,
        SkillName.Stealth,
        SkillName.RemoveTrap,
        SkillName.Necromancy,
        SkillName.Focus,
        SkillName.Chivalry,
        SkillName.Bushido,
        SkillName.Ninjitsu,
        SkillName.Spellweaving,
        // TODO: Update RandomSkill once these are implemented!
        // SkillName.Mysticism,
        // SkillName.Imbuing,
        SkillName.Throwing
    };

    private static readonly SkillName[] m_CombatSkills =
    {
        SkillName.Archery,
        SkillName.Swords,
        SkillName.Macing,
        SkillName.Fencing,
        SkillName.Wrestling
    };

    private static readonly SkillName[] m_CraftSkills =
    {
        SkillName.Alchemy,
        SkillName.Blacksmith,
        SkillName.Fletching,
        SkillName.Carpentry,
        SkillName.Cartography,
        SkillName.Cooking,
        SkillName.Inscribe,
        SkillName.Tailoring,
        SkillName.Tinkering
    };

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
    public static uint IPv4ToAddress(IPAddress ipAddress)
    {
        if (ipAddress.IsIPv4MappedToIPv6)
        {
            ipAddress = ipAddress.MapToIPv4();
        }
        else if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return 0;
        }

        Span<byte> integer = stackalloc byte[4];
        ipAddress.TryWriteBytes(integer, out var bytesWritten);
        return bytesWritten != 4 ? 0 : BinaryPrimitives.ReadUInt32BigEndian(integer);
    }

    public static bool IPMatchClassC(IPAddress ip1, IPAddress ip2)
    {
        var a = IPv4ToAddress(ip1);
        var b = IPv4ToAddress(ip2);

        return a == 0 || b == 0 ? ip1.Equals(ip2) : (a & 0xFFFFFF) == (b & 0xFFFFFF);
    }

    public static bool IPMatchCIDR(IPAddress cidrAddress, IPAddress address, int cidrLength)
    {
        if (cidrAddress.AddressFamily == AddressFamily.InterNetwork)
        {
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return false;
            }

            cidrLength += 96;
        }

        cidrAddress = cidrAddress.MapToIPv6();
        address = address.MapToIPv6();

        cidrLength = Math.Clamp(cidrLength, 0, 128);

        Span<byte> cidrBytes = stackalloc byte[16];
        cidrAddress.TryWriteBytes(cidrBytes, out var _);

        Span<byte> addrBytes = stackalloc byte[16];
        address.TryWriteBytes(addrBytes, out var _);

        var i = 0;
        int offset;

        if (cidrLength < 32)
        {
            offset = cidrLength;
        }
        else
        {
            var index = Math.DivRem(cidrLength, 32, out offset);
            while (index > 0)
            {
                if (
                    BinaryPrimitives.ReadInt32BigEndian(cidrBytes.Slice(i, 4)) !=
                    BinaryPrimitives.ReadInt32BigEndian(addrBytes.Slice(i, 4))
                )
                {
                    return false;
                }

                i += 4;
                --index;
            }
        }

        if (offset == 0)
        {
            return true;
        }

        var c = BinaryPrimitives.ReadInt32BigEndian(cidrBytes.Slice(i, 4));
        var a = BinaryPrimitives.ReadInt32BigEndian(addrBytes.Slice(i, 4));

        var mask = (1 << (32 - offset)) - 1;
        var min = ~mask & c;
        var max = c | mask;

        return a >= min && a <= max;
    }

    public static bool IsValidIP(string val) => IPMatch(val, IPAddress.Any, out var valid) || valid;

    public static bool IPMatch(string val, IPAddress ip) => IPMatch(val, ip, out _);

    public static bool IPMatch(string val, IPAddress ip, out bool valid)
    {
        var family = ip.AddressFamily;
        var useIPv6 = family == AddressFamily.InterNetworkV6 || val.ContainsOrdinal(':');

        ip = useIPv6 ? ip.MapToIPv6() : ip.MapToIPv4();

        Span<byte> ipBytes = stackalloc byte[useIPv6 ? 16 : 4];
        ip.TryWriteBytes(ipBytes, out _);

        return useIPv6 ? IPv6Match(val, ipBytes, out valid) : IPv4Match(val, ipBytes, out valid);
    }

    public static bool IPv4Match(ReadOnlySpan<char> val, ReadOnlySpan<byte> ip, out bool valid)
    {
        var match = true;
        valid = true;
        var end = val.Length;
        var byteIndex = 0;
        var section = 0;
        var number = 0;
        var isRange = false;
        var intBase = 10;
        var endOfSection = false;
        var sectionStart = 0;

        var num = ip[byteIndex++];

        for (var i = 0; i < end; i++)
        {
            var chr = val[i];
            if (section >= 4)
            {
                valid = false;
                return false;
            }

            switch (chr)
            {
                default:
                    {
                        if (!Uri.IsHexDigit(chr))
                        {
                            valid = false;
                            return false;
                        }

                        number = number * intBase + Uri.FromHex(chr);
                        break;
                    }
                case 'x':
                case 'X':
                    {
                        if (i == sectionStart)
                        {
                            intBase = 16;
                            break;
                        }

                        valid = false;
                        return false;
                    }
                case '-':
                    {
                        if (i == sectionStart || i + 1 == end || val[i + 1] == '.')
                        {
                            valid = false;
                            return false;
                        }

                        // Only allows a single range in a section
                        if (isRange)
                        {
                            valid = false;
                            return false;
                        }

                        isRange = true;
                        match = match && num >= number;
                        number = 0;
                        break;
                    }
                case '*':
                    {
                        if (i != sectionStart || i + 1 < end && val[i + 1] != '.')
                        {
                            valid = false;
                            return false;
                        }

                        isRange = true;
                        number = 255;
                        break;
                    }
                case '.':
                    {
                        endOfSection = true;
                        break;
                    }
            }

            if (endOfSection || i + 1 == end)
            {
                if (number is < 0 or > 255)
                {
                    valid = false;
                    return false;
                }

                match = match && (isRange ? num <= number : number == num);

                if (++section < 4)
                {
                    num = ip[byteIndex++];
                }

                intBase = 10;
                number = 0;
                endOfSection = false;
                sectionStart = i + 1;
                isRange = false;
            }
        }

        return match;
    }

    public static bool IPv6Match(ReadOnlySpan<char> val, ReadOnlySpan<byte> ip, out bool valid)
    {
        valid = true;

        // Start must be two `::` or a number
        if (val[0] == ':' && val[1] != ':')
        {
            valid = false;
            return false;
        }

        var match = true;
        var end = val.Length;
        var byteIndex = 2;
        var section = 0;
        var number = 0;
        var isRange = false;
        var endOfSection = false;
        var sectionStart = 0;
        var hasCompressor = false;

        var num = BinaryPrimitives.ReadUInt16BigEndian(ip[..2]);

        for (int i = 0; i < end; i++)
        {
            if (section > 7)
            {
                valid = false;
                return false;
            }

            var chr = val[i];
            // We are starting a new sequence, check the previous one then continue
            switch (chr)
            {
                default:
                    {
                        if (!Uri.IsHexDigit(chr))
                        {
                            valid = false;
                            return false;
                        }

                        number = number * 16 + Uri.FromHex(chr);
                        break;
                    }
                case '?':
                    {
                        logger.Debug("IP Match '?' character is not supported.");
                        valid = false;
                        return false;
                    }
                // Range
                case '-':
                    {
                        if (i == sectionStart || i + 1 == end || val[i + 1] == ':')
                        {
                            valid = false;
                            return false;
                        }

                        // Only allows a single range in a section
                        if (isRange)
                        {
                            valid = false;
                            return false;
                        }

                        isRange = true;

                        // Check low part of the range
                        match = match && num >= number;
                        number = 0;
                        break;
                    }
                // Wild section
                case '*':
                    {
                        if (i != sectionStart || i + 1 < end && val[i + 1] != ':')
                        {
                            valid = false;
                            return false;
                        }

                        isRange = true;
                        number = 65535;
                        break;
                    }
                case ':':
                    {
                        endOfSection = true;
                        break;
                    }
            }

            if (!endOfSection && i + 1 != end)
            {
                continue;
            }

            if (++i == end || val[i] != ':' || section > 0)
            {
                match = match && (isRange ? num <= number : number == num);

                // IPv4 matching at the end
                if (section == 6 && num == 0xFFFF)
                {
                    var ipv4 = val[(i + 1)..];
                    if (ipv4.Contains('.'))
                    {
                        return IPv4Match(ipv4, ip[^4..], out valid);
                    }
                }

                if (i == end)
                {
                    break;
                }

                num = BinaryPrimitives.ReadUInt16BigEndian(ip.Slice(byteIndex, 2));
                byteIndex += 2;

                ++section;
            }

            if (i < end && val[i] == ':')
            {
                if (hasCompressor)
                {
                    valid = false;
                    return false;
                }

                int newSection;

                if (i + 1 < end)
                {
                    var remainingColons = val[(i + 1)..].Count(':');
                    // double colon must be at least 2 sections
                    // we need at least 1 section remaining out of 8
                    // This means 8 - 2 would be 6 sections (5 colons)
                    newSection = section + 2 + (5 - remainingColons);
                    if (newSection > 7)
                    {
                        valid = false;
                        return false;
                    }
                }
                else
                {
                    newSection = 7;
                }

                var zeroEnd = (newSection + 1) * 2;
                do
                {
                    if (match)
                    {
                        if (num != 0)
                        {
                            match = false;
                        }

                        num = BinaryPrimitives.ReadUInt16BigEndian(ip.Slice(byteIndex, 2));
                    }

                    byteIndex += 2;
                } while (byteIndex < zeroEnd);

                section = newSection;
                hasCompressor = true;
            }
            else
            {
                i--;
            }

            number = 0;
            endOfSection = false;
            sectionStart = i + 1;
            isRange = false;
        }

        return match;
    }

    public static string FixHtml(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        var chars = str.ToPooledArray();
        var span = chars.AsSpan(0, str.Length);
        FixHtml(span);

        return span.ToString();
    }

    public static void FixHtml(Span<char> chars)
    {
        if (chars.Length == 0)
        {
            return;
        }

        ReadOnlySpan<char> invalid = stackalloc []{ '<', '>', '#' };
        ReadOnlySpan<char> replacement = stackalloc []{ '(', ')', '-' };

        chars.ReplaceAny(invalid, replacement);
    }

    public static PooledArraySpanFormattable FixHtmlFormattable(string str)
    {
        var chars = str.ToPooledArray();
        var span = chars.AsSpan(0, str.Length);
        var formattable = new PooledArraySpanFormattable(chars, str.Length);

        if (!string.IsNullOrEmpty(str))
        {
            FixHtml(span);
        }

        return formattable;
    }

    public static int InsensitiveCompare(string first, string second) => first.InsensitiveCompare(second);

    public static bool InsensitiveStartsWith(string first, string second) => first.InsensitiveStartsWith(second);

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

    public static SkillName RandomSkill()
    {
        // TODO: Add 2 to each entry for Mysticism and Imbuing, once they are uncommented on _allSkills.
        var offset = Core.Expansion switch
        {
            >= Expansion.SA => 0,
            Expansion.ML    => 1,
            Expansion.SE    => 2,
            Expansion.AOS   => 4,
            _               => 7
        };

        return _allSkills[Random(_allSkills.Length - offset)];
    }

    public static SkillName RandomCombatSkill() => m_CombatSkills.RandomElement();

    public static SkillName RandomCraftSkill() => m_CraftSkills.RandomElement();

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

    public static void FormatBuffer(this TextWriter op, ReadOnlySpan<byte> first, ReadOnlySpan<byte> second, int totalLength)
    {
        op.WriteLine("        0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F");
        op.WriteLine("       -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --");

        if (totalLength <= 0)
        {
            op.WriteLine("0000   ");
            return;
        }

        Span<byte> lineBytes = stackalloc byte[16];
        Span<char> lineChars = stackalloc char[47];
        for (var i = 0; i < totalLength; i += 16)
        {
            var length = Math.Min(totalLength - i, 16);
            if (i < first.Length)
            {
                var firstLength = Math.Min(length, first.Length - i);
                first.Slice(i, firstLength).CopyTo(lineBytes);

                if (firstLength < length)
                {
                    second[..(length - first.Length - i)].CopyTo(lineBytes[(length - firstLength)..]);
                }
            }
            else
            {
                second.Slice(i - first.Length, length).CopyTo(lineBytes);
            }

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

    public static bool ToBoolean(string value) =>
        bool.TryParse(value, out var b) && b ||
        value.InsensitiveEquals("enabled") ||
        value.InsensitiveEquals("on") ||
        !value.InsensitiveEquals("disabled") && !value.InsensitiveEquals("off");

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
    public static bool InUpdateRange(Point2D p1, Point2D p2) => InRange(p1, p2, 18);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InRange(Point3D p1, Point3D p2, int range) =>
        InRange(p1.m_X, p1.m_Y, p2.m_X, p2.m_Y, range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InUpdateRange(Point3D p1, Point3D p2) => InRange(p1, p2, 18);

    // 4d6+8 would be: Utility.Dice( 4, 6, 8 )
    public static int Dice(uint amount, uint sides, int bonus)
    {
        var total = 0;

        for (var i = 0; i < amount; ++i)
        {
            total += (int)RandomSources.Source.Next(1, sides);
        }

        return total + bonus;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Shuffle<T>(this IList<T> list)
    {
        var count = list.Count;
        for (var i = 0; i < count; i++)
        {
            var r = RandomMinMax(i, count - 1);
            (list[r], list[i]) = (list[i], list[r]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Shuffle<T>(this Span<T> list)
    {
        var count = list.Length;
        for (var i = 0; i < count; i++)
        {
            var r = RandomMinMax(i, count - 1);
            (list[r], list[i]) = (list[i], list[r]);
        }
    }

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
        var sampleList = new T[count];

        var i = 0;
        do
        {
            var rand = Random(length);
            if (!(list[rand] && (list[rand] = true)))
            {
                sampleList[i++] = source[rand];
            }
        } while (i < count);

        return sampleList;
    }

    public static List<T> RandomSample<T>(this List<T> source, int count)
    {
        if (count <= 0)
        {
            return new List<T>();
        }

        var length = source.Count;
        Span<bool> list = stackalloc bool[length];
        var sampleList = new List<T>(count);

        var i = 0;
        do
        {
            var rand = Random(length);
            if (!(list[rand] && (list[rand] = true)))
            {
                sampleList[i++] = source[rand];
            }
        } while (i < count);

        return sampleList;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T RandomList<T>(params T[] list) => list.RandomElement();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T RandomElement<T>(this IList<T> list) => list.RandomElement(default);

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
    public static T RandomElement<T>(this IList<T> list, T valueIfZero) =>
        list.Count == 0 ? valueIfZero : list[Random(list.Count)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RandomBool() => RandomSources.Source.NextBool();

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

        return min + RandomSources.Source.NextDouble() * (max - min);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint RandomMinMax(uint min, uint max)
    {
        if (min > max)
        {
            (min, max) = (max, min);
        }
        else if (min == max)
        {
            return min;
        }

        return min + RandomSources.Source.Next(max - min + 1);
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

        return min + (int)RandomSources.Source.Next((uint)(max - min + 1));
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

        return min + RandomSources.Source.Next(max - min + 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Random(int from, int count) => RandomSources.Source.Next(from, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Random(int count) => RandomSources.Source.Next(count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Random(uint count) => RandomSources.Source.Next(count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RandomBytes(Span<byte> buffer) => RandomSources.Source.NextBytes(buffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double RandomDouble() => RandomSources.Source.NextDouble();

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
        list ??= new List<T>();
        list.Add(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<T>(ref HashSet<T> set, T value)
    {
        set ??= new HashSet<T>();
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
        if (list != null)
        {
            var removed = list.Remove(value);

            if (list.Count == 0)
            {
                list = null;
            }

            return removed;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Remove<T>(ref HashSet<T> set, T value)
    {
        if (set != null)
        {
            var removed = set.Remove(value);

            if (set.Count == 0)
            {
                set = null;
            }

            return removed;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Remove<K, V>(ref Dictionary<K, V> dict, K key)
    {
        if (dict != null)
        {
            var removed = dict.Remove(key);

            if (dict.Count == 0)
            {
                dict = null;
            }

            return removed;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Remove<K, V>(ref Dictionary<K, V> dict, K key, out V value)
    {
        if (dict != null)
        {
            var removed = dict.Remove(key, out value);

            if (dict.Count == 0)
            {
                dict = null;
            }

            return removed;
        }

        value = default;
        return false;
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
    public static T[] Combine<T>(this IList<T> source, params IList<T>[] arrays) =>
        source.Combine(false, arrays);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] CombinePooled<T>(this IList<T> source, params IList<T>[] arrays) =>
        source.Combine(true, arrays);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] Combine<T>(this IList<T> source, bool pooled, params IList<T>[] arrays)
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
}
