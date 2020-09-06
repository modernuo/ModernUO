/***************************************************************************
 *                                Utility.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using Server.Random;

namespace Server
{
    public static class Utility
    {
        private static Encoding m_UTF8, m_UTF8WithEncoding;

        private static Dictionary<IPAddress, IPAddress> _ipAddressTable;

        private static readonly SkillName[] m_AllSkills =
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
            SkillName.Spellweaving
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

        private static readonly Stack<ConsoleColor> m_ConsoleColors = new Stack<ConsoleColor>();

        public static Encoding UTF8 => m_UTF8 ??= new UTF8Encoding(false, false);
        public static Encoding UTF8WithEncoding => m_UTF8WithEncoding ??= new UTF8Encoding(true, false);

        public static void Separate(StringBuilder sb, string value, string separator)
        {
            if (sb.Length > 0)
                sb.Append(separator);

            sb.Append(value);
        }

        public static string Intern(string str) => str?.Length > 0 ? string.Intern(str) : str;

        public static void Intern(ref string str)
        {
            str = Intern(str);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string IsNullOrDefault(this string value, string def) => value?.Length > 0 ? value : def;

        public static IPAddress Intern(IPAddress ipAddress)
        {
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

        public static bool IsValidIP(string text)
        {
            IPMatch(text, IPAddress.None, out var valid);

            return valid;
        }

        public static bool IPMatch(string val, IPAddress ip) => IPMatch(val, ip, out _);

        public static string FixHtml(string str)
        {
            if (str == null)
                return "";

            var hasOpen = str.IndexOf('<') >= 0;
            var hasClose = str.IndexOf('>') >= 0;
            var hasPound = str.IndexOf('#') >= 0;

            if (!hasOpen && !hasClose && !hasPound)
                return str;

            var sb = new StringBuilder(str);

            if (hasOpen)
                sb.Replace('<', '(');

            if (hasClose)
                sb.Replace('>', ')');

            if (hasPound)
                sb.Replace('#', '-');

            return sb.ToString();
        }

        public static bool IPMatchCIDR(string cidr, IPAddress ip)
        {
            if (ip == null || ip.AddressFamily == AddressFamily.InterNetworkV6)
                return false; // Just worry about IPv4 for now

            var bytes = new byte[4];
            var split = cidr.Split('.');
            var cidrBits = false;
            var cidrLength = 0;

            for (var i = 0; i < 4; i++)
            {
                var part = 0;

                var partBase = 10;

                var pattern = split[i];

                for (var j = 0; j < pattern.Length; j++)
                {
                    var c = pattern[j];

                    if (c == 'x' || c == 'X')
                    {
                        partBase = 16;
                    }
                    else if (c >= '0' && c <= '9')
                    {
                        var offset = c - '0';

                        if (cidrBits)
                        {
                            cidrLength *= partBase;
                            cidrLength += offset;
                        }
                        else
                        {
                            part *= partBase;
                            part += offset;
                        }
                    }
                    else if (c >= 'a' && c <= 'f')
                    {
                        var offset = 10 + (c - 'a');

                        if (cidrBits)
                        {
                            cidrLength *= partBase;
                            cidrLength += offset;
                        }
                        else
                        {
                            part *= partBase;
                            part += offset;
                        }
                    }
                    else if (c >= 'A' && c <= 'F')
                    {
                        var offset = 10 + (c - 'A');

                        if (cidrBits)
                        {
                            cidrLength *= partBase;
                            cidrLength += offset;
                        }
                        else
                        {
                            part *= partBase;
                            part += offset;
                        }
                    }
                    else if (c == '/')
                    {
                        if (cidrBits || i != 3) // If there's two '/' or the '/' isn't in the last byte
                            return false;

                        partBase = 10;
                        cidrBits = true;
                    }
                    else
                    {
                        return false;
                    }
                }

                bytes[i] = (byte)part;
            }

            return IPMatchCIDR(OrderedAddressValue(bytes), ip, cidrLength);
        }

        public static bool IPMatchCIDR(IPAddress cidrPrefix, IPAddress ip, int cidrLength)
        {
            // Ignore IPv6 for now
            if (cidrPrefix == null || ip == null || cidrPrefix.AddressFamily == AddressFamily.InterNetworkV6)
                return false;

            var cidrValue = SwapUnsignedInt((uint)GetLongAddressValue(cidrPrefix));
            var ipValue = SwapUnsignedInt((uint)GetLongAddressValue(ip));

            return IPMatchCIDR(cidrValue, ipValue, cidrLength);
        }

        public static bool IPMatchCIDR(uint cidrPrefixValue, IPAddress ip, int cidrLength)
        {
            if (ip == null || ip.AddressFamily == AddressFamily.InterNetworkV6)
                return false;

            var ipValue = SwapUnsignedInt((uint)GetLongAddressValue(ip));

            return IPMatchCIDR(cidrPrefixValue, ipValue, cidrLength);
        }

        public static bool IPMatchCIDR(uint cidrPrefixValue, uint ipValue, int cidrLength)
        {
            if (cidrLength <= 0 || cidrLength >= 32) // if invalid cidr Length, just compare IPs
                return cidrPrefixValue == ipValue;

            var mask = uint.MaxValue << (32 - cidrLength);

            return (cidrPrefixValue & mask) == (ipValue & mask);
        }

        private static uint OrderedAddressValue(byte[] bytes)
        {
            if (bytes.Length != 4)
                return 0;

            return (uint)((bytes[0] << 0x18) | (bytes[1] << 0x10) | (bytes[2] << 8) | bytes[3]) & 0xffffffff;
        }

        private static uint SwapUnsignedInt(uint source) =>
            ((source & 0x000000FF) << 0x18)
            | ((source & 0x0000FF00) << 8)
            | ((source & 0x00FF0000) >> 8)
            | ((source & 0xFF000000) >> 0x18);

        public static bool TryConvertIPv6toIPv4(ref IPAddress address)
        {
            if (!Socket.OSSupportsIPv6 || address.AddressFamily == AddressFamily.InterNetwork)
                return true;

            var addr = address.GetAddressBytes();
            if (addr.Length == 16) // sanity 0 - 15 //10 11 //12 13 14 15
            {
                if (addr[10] != 0xFF || addr[11] != 0xFF)
                    return false;

                for (var i = 0; i < 10; i++)
                    if (addr[i] != 0)
                        return false;

                var v4Addr = new byte[4];

                for (var i = 0; i < 4; i++) v4Addr[i] = addr[12 + i];

                address = new IPAddress(v4Addr);
                return true;
            }

            return false;
        }

        public static bool IPMatch(string val, IPAddress ip, out bool valid)
        {
            valid = true;

            var split = val.Split('.');

            for (var i = 0; i < 4; ++i)
            {
                int lowPart, highPart;

                if (i >= split.Length)
                {
                    lowPart = 0;
                    highPart = 255;
                }
                else
                {
                    var pattern = split[i];

                    if (pattern == "*")
                    {
                        lowPart = 0;
                        highPart = 255;
                    }
                    else
                    {
                        lowPart = 0;
                        highPart = 0;

                        var highOnly = false;
                        var lowBase = 10;
                        var highBase = 10;

                        for (var j = 0; j < pattern.Length; ++j)
                        {
                            var c = pattern[j];

                            if (c == '?')
                            {
                                if (!highOnly)
                                {
                                    lowPart *= lowBase;
                                    lowPart += 0;
                                }

                                highPart *= highBase;
                                highPart += highBase - 1;
                            }
                            else if (c == '-')
                            {
                                highOnly = true;
                                highPart = 0;
                            }
                            else if (c == 'x' || c == 'X')
                            {
                                lowBase = 16;
                                highBase = 16;
                            }
                            else if (c >= '0' && c <= '9')
                            {
                                var offset = c - '0';

                                if (!highOnly)
                                {
                                    lowPart *= lowBase;
                                    lowPart += offset;
                                }

                                highPart *= highBase;
                                highPart += offset;
                            }
                            else if (c >= 'a' && c <= 'f')
                            {
                                var offset = 10 + (c - 'a');

                                if (!highOnly)
                                {
                                    lowPart *= lowBase;
                                    lowPart += offset;
                                }

                                highPart *= highBase;
                                highPart += offset;
                            }
                            else if (c >= 'A' && c <= 'F')
                            {
                                var offset = 10 + (c - 'A');

                                if (!highOnly)
                                {
                                    lowPart *= lowBase;
                                    lowPart += offset;
                                }

                                highPart *= highBase;
                                highPart += offset;
                            }
                            else
                            {
                                valid = false; // high & lowp art would be 0 if it got to here.
                            }
                        }
                    }
                }

                int b = (byte)(GetAddressValue(ip) >> (i * 8));

                if (b < lowPart || b > highPart)
                    return false;
            }

            return true;
        }

        public static bool IPMatchClassC(IPAddress ip1, IPAddress ip2) =>
            (GetAddressValue(ip1) & 0xFFFFFF) == (GetAddressValue(ip2) & 0xFFFFFF);

        public static int InsensitiveCompare(string first, string second) => Insensitive.Compare(first, second);

        public static bool InsensitiveStartsWith(string first, string second) => Insensitive.StartsWith(first, second);

        public static Direction GetDirection(IPoint2D from, IPoint2D to)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;

            var adx = Math.Abs(dx);
            var ady = Math.Abs(dy);

            if (adx >= ady * 3) return dx > 0 ? Direction.East : Direction.West;

            if (ady >= adx * 3) return dy > 0 ? Direction.South : Direction.North;

            if (dx > 0) return dy > 0 ? Direction.Down : Direction.Right;

            return dy > 0 ? Direction.Left : Direction.Up;
        }

        public static object GetArrayCap(Array array, int index, object emptyValue = null) =>
            array.Length > 0 ? array.GetValue(Math.Clamp(index, 0, array.Length - 1)) : emptyValue;

        public static SkillName RandomSkill() =>
            m_AllSkills[Random(
                m_AllSkills.Length - (Core.ML ? 0 :
                    Core.SE ? 1 :
                    Core.AOS ? 3 : 6)
            )];

        public static SkillName RandomCombatSkill() => m_CombatSkills.RandomElement();

        public static SkillName RandomCraftSkill() => m_CraftSkills.RandomElement();

        public static void FixPoints(ref Point3D top, ref Point3D bottom)
        {
            if (bottom.m_X < top.m_X)
            {
                var swap = top.m_X;
                top.m_X = bottom.m_X;
                bottom.m_X = swap;
            }

            if (bottom.m_Y < top.m_Y)
            {
                var swap = top.m_Y;
                top.m_Y = bottom.m_Y;
                bottom.m_Y = swap;
            }

            if (bottom.m_Z < top.m_Z)
            {
                var swap = top.m_Z;
                top.m_Z = bottom.m_Z;
                bottom.m_Z = swap;
            }
        }

        public static bool RangeCheck(IPoint2D p1, IPoint2D p2, int range) =>
            p1.X >= p2.X - range
            && p1.X <= p2.X + range
            && p1.Y >= p2.Y - range
            && p2.Y <= p2.Y + range;

        public static void FormatBuffer(TextWriter output, Stream input, int length)
        {
            output.WriteLine("        0  1  2  3  4  5  6  7   8  9  A  B  C  D  E  F");
            output.WriteLine("       -- -- -- -- -- -- -- --  -- -- -- -- -- -- -- --");

            var byteIndex = 0;

            var whole = length >> 4;
            var rem = length & 0xF;

            for (var i = 0; i < whole; ++i, byteIndex += 16)
            {
                var bytes = new StringBuilder(49);
                var chars = new StringBuilder(16);

                for (var j = 0; j < 16; ++j)
                {
                    var c = input.ReadByte();

                    bytes.Append(c.ToString("X2"));

                    if (j != 7)
                        bytes.Append(' ');
                    else
                        bytes.Append("  ");

                    if (c >= 0x20 && c < 0x7F)
                        chars.Append((char)c);
                    else
                        chars.Append('.');
                }

                output.Write(byteIndex.ToString("X4"));
                output.Write("   ");
                output.Write(bytes.ToString());
                output.Write("  ");
                output.WriteLine(chars.ToString());
            }

            if (rem != 0)
            {
                var bytes = new StringBuilder(49);
                var chars = new StringBuilder(rem);

                for (var j = 0; j < 16; ++j)
                    if (j < rem)
                    {
                        var c = input.ReadByte();

                        bytes.Append(c.ToString("X2"));

                        if (j != 7)
                            bytes.Append(' ');
                        else
                            bytes.Append("  ");

                        if (c >= 0x20 && c < 0x7F)
                            chars.Append((char)c);
                        else
                            chars.Append('.');
                    }
                    else
                    {
                        bytes.Append("   ");
                    }

                output.Write(byteIndex.ToString("X4"));
                output.Write("   ");
                output.Write(bytes.ToString());
                output.Write("  ");
                output.WriteLine(chars.ToString());
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
                var i = bound1;
                bound1 = bound2;
                bound2 = i;
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
                m.HairHue = m.Race.RandomHairHue();
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
                m.FacialHairHue = m.Race.RandomHairHue();
        }

        public static List<TOutput> CastListContravariant<TInput, TOutput>(List<TInput> list) where TInput : TOutput =>
            list.ConvertAll(value => (TOutput)value);

        public static List<TOutput> CastListCovariant<TInput, TOutput>(List<TInput> list) where TOutput : TInput =>
            list.ConvertAll(value => (TOutput)value);

        public static List<TOutput> SafeConvertList<TInput, TOutput>(List<TInput> list) where TOutput : class
        {
            if ((list?.Capacity ?? 0) == 0)
                return new List<TOutput>();

            var output = new List<TOutput>(list.Capacity);
            output.AddRange(list.OfType<TOutput>());

            return output;
        }

        public static bool ToBoolean(string value)
        {
#pragma warning disable CA1806 // Do not ignore method results
            bool.TryParse(value, out var b);
#pragma warning restore CA1806 // Do not ignore method results

            return b;
        }

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

        public static int ToInt32(string value)
        {
            int i;

#pragma warning disable CA1806 // Do not ignore method results
            if (value.StartsWith("0x"))
                int.TryParse(value.Substring(2), NumberStyles.HexNumber, null, out i);
            else
                int.TryParse(value, out i);
#pragma warning restore CA1806 // Do not ignore method results

            return i;
        }

        public static uint ToUInt32(string value)
        {
            uint i;

#pragma warning disable CA1806 // Do not ignore method results
            if (value.StartsWith("0x"))
                uint.TryParse(value.Substring(2), NumberStyles.HexNumber, null, out i);
            else
                uint.TryParse(value, out i);
#pragma warning restore CA1806 // Do not ignore method results

            return i;
        }

        public static bool ToInt32(string value, out int i) =>
            value.StartsWith("0x")
                ? int.TryParse(value.Substring(2), NumberStyles.HexNumber, null, out i)
                : int.TryParse(value, out i);

        public static bool ToUInt32(string value, out uint i) =>
            value.StartsWith("0x")
                ? uint.TryParse(value.Substring(2), NumberStyles.HexNumber, null, out i)
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

        public static string GetText(XmlElement node, string defaultValue) => node == null ? defaultValue : node.InnerText;

        public static int GetAddressValue(IPAddress address) => BitConverter.ToInt32(address.GetAddressBytes(), 0);

        public static long GetLongAddressValue(IPAddress address) => BitConverter.ToInt64(address.GetAddressBytes(), 0);

        public static bool InRange(Point3D p1, Point3D p2, int range) =>
            p1.m_X >= p2.m_X - range
            && p1.m_X <= p2.m_X + range
            && p1.m_Y >= p2.m_Y - range
            && p1.m_Y <= p2.m_Y + range;

        public static bool InUpdateRange(Point3D p1, Point3D p2) =>
            p1.m_X >= p2.m_X - 18
            && p1.m_X <= p2.m_X + 18
            && p1.m_Y >= p2.m_Y - 18
            && p1.m_Y <= p2.m_Y + 18;

        public static bool InUpdateRange(Point2D p1, Point2D p2) =>
            p1.m_X >= p2.m_X - 18
            && p1.m_X <= p2.m_X + 18
            && p1.m_Y >= p2.m_Y - 18
            && p1.m_Y <= p2.m_Y + 18;

        public static bool InUpdateRange(IPoint2D p1, IPoint2D p2) =>
            p1.X >= p2.X - 18
            && p1.X <= p2.X + 18
            && p1.Y >= p2.Y - 18
            && p1.Y <= p2.Y + 18;

        // 4d6+8 would be: Utility.Dice( 4, 6, 8 )
        public static int Dice(uint amount, uint sides, int bonus)
        {
            var total = 0;

            for (var i = 0; i < amount; ++i)
                total += (int)RandomSources.Source.Next(1, sides);

            return total + bonus;
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            var count = list.Count;
            for (var i = 0; i < count; i++)
            {
                var r = RandomMinMax(i, count - 1);
                var swap = list[r];
                list[r] = list[i];
                list[i] = swap;
            }
        }

        public static void Shuffle<T>(this Span<T> list)
        {
            var count = list.Length;
            for (var i = 0; i < count; i++)
            {
                var r = RandomMinMax(i, count - 1);
                var swap = list[r];
                list[r] = list[i];
                list[i] = swap;
            }
        }

        /**
     * Gets a random sample from the source list.
     * Not meant for unbounded lists. Does not shuffle or modify source.
     */
        public static T[] RandomSample<T>(this T[] source, int count)
        {
            if (count <= 0) return Array.Empty<T>();

            var length = source.Length;
            Span<bool> list = stackalloc bool[length];
            var sampleList = new T[count];

            var i = 0;
            do
            {
                var rand = Random(length);
                if (!(list[rand] && (list[rand] = true)))
                    sampleList[i++] = source[rand];
            } while (i < count);

            return sampleList;
        }

        public static List<T> RandomSample<T>(this List<T> source, int count)
        {
            if (count <= 0) return new List<T>();

            var length = source.Count;
            Span<bool> list = stackalloc bool[length];
            var sampleList = new List<T>(count);

            var i = 0;
            do
            {
                var rand = Random(length);
                if (!(list[rand] && (list[rand] = true)))
                    sampleList[i++] = source[rand];
            } while (i < count);

            return sampleList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T RandomList<T>(params T[] list) => list.RandomElement();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T RandomElement<T>(this IList<T> list) => list.RandomElement(default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T RandomElement<T>(this IList<T> list, T valueIfZero) =>
            list.Count == 0 ? valueIfZero : list[Random(list.Count)];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RandomBool() => RandomSources.Source.NextBool();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RandomMinMax(int min, int max)
        {
            if (min > max)
            {
                var copy = min;
                min = max;
                max = copy;
            }
            else if (min == max)
            {
                return min;
            }

            return min + (int)RandomSources.Source.Next((uint)(max - min + 1));
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
        public static TimeSpan Max(this TimeSpan val, TimeSpan max) => val > max ? max : val;
    }
}
