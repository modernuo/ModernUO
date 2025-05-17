/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ClientVersion.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Network;
using Server.Text;

namespace Server;

public class ClientVersion : IComparable<ClientVersion>, IComparer<ClientVersion>, IEquatable<ClientVersion>
{
    public static readonly ClientVersion Version400a = new("4.0.0a");
    public static readonly ClientVersion Version407a = new("4.0.7a");
    public static readonly ClientVersion Version500a = new("5.0.0a");
    public static readonly ClientVersion Version502b = new("5.0.2b");
    public static readonly ClientVersion Version6000 = new("6.0.0.0");
    public static readonly ClientVersion Version6000KR = new("66.55.38"); // KR 2.44.0.15 (First release)
    public static readonly ClientVersion Version6017 = new("6.0.1.7");
    public static readonly ClientVersion Version6050 = new("6.0.5.0");
    public static readonly ClientVersion Version60142 = new("6.0.14.2");
    public static readonly ClientVersion Version60142KR = new("66.55.53"); // KR 2.59.0.2
    public static readonly ClientVersion Version7000 = new("7.0.0.0");
    public static readonly ClientVersion Version7090 = new("7.0.9.0");
    public static readonly ClientVersion Version70120 = new("7.0.12.0"); // Plant localization change
    public static readonly ClientVersion Version70130 = new("7.0.13.0");
    public static readonly ClientVersion Version70160 = new("7.0.16.0");
    public static readonly ClientVersion Version70300 = new("7.0.30.0");
    public static readonly ClientVersion Version70331 = new("7.0.33.1");
    public static readonly ClientVersion Version704565 = new("7.0.45.65");
    public static readonly ClientVersion Version70500 = new("7.0.50.0");
    public static readonly ClientVersion Version70610 = new("7.0.61.0");
    public static readonly ClientVersion Version70654 = new("7.0.65.4"); // Insufficient mana change

    public ClientVersion(int maj, int min, int rev, int pat, ClientType type = ClientType.Classic)
    {
        if (maj >= 67)
        {
            Major = maj - 60;
            Type = ClientType.SA;
        }
        else
        {
            Major = maj;
            Type = maj == 66 ? ClientType.KR : type;
        }

        Minor = min;
        Revision = rev;
        Patch = pat;

        SourceString = ToStringImpl().Intern();
    }

    public ClientVersion(string fmt)
    {
        fmt = fmt.ToLower();
        SourceString = fmt.Intern();

        try
        {
            var br1 = fmt.IndexOfOrdinal('.');
            var br2 = fmt.IndexOf('.', br1 + 1);

            var br3 = br2 + 1;
            while (br3 < fmt.Length && char.IsDigit(fmt, br3))
            {
                br3++;
            }

            Major = Utility.ToInt32(fmt.AsSpan()[..br1]);
            Minor = Utility.ToInt32(fmt.AsSpan(br1 + 1, br2 - br1 - 1));
            Revision = Utility.ToInt32(fmt.AsSpan(br2 + 1, br3 - br2 - 1));

            if (br3 < fmt.Length)
            {
                if (Major <= 5 && Minor <= 0 && Revision <= 6) // Anything before 5.0.7
                {
                    if (!char.IsWhiteSpace(fmt, br3))
                    {
                        Patch = fmt[br3] - 'a';
                    }
                }
                else
                {
                    Patch = Utility.ToInt32(fmt.AsSpan(br3 + 1, fmt.Length - br3 - 1));
                }
            }

            if (Major == 66)
            {
                Type = ClientType.KR;
            }
            else if (Major > 66)
            {
                Major -= 60;
                Type = ClientType.SA;
            }
            else if (fmt.InsensitiveContains("third dawn") ||
                     fmt.InsensitiveContains("uo:td") ||
                     fmt.InsensitiveContains("uotd") ||
                     fmt.InsensitiveContains("uo3d") ||
                     fmt.InsensitiveContains("uo:3d"))
            {
                Type = ClientType.UOTD;
            }
        }
        catch
        {
            Major = 0;
            Minor = 0;
            Revision = 0;
            Patch = 0;
            Type = ClientType.Classic;
        }
    }

    public int Major { get; }

    public int Minor { get; }

    public int Revision { get; }

    public int Patch { get; }

    public ClientType Type { get; }

    public string SourceString { get; }

    public int CompareTo(ClientVersion o)
    {
        if (o == null)
        {
            return 1;
        }

        if (Major > o.Major)
        {
            return 1;
        }

        if (Major < o.Major)
        {
            return -1;
        }

        if (Minor > o.Minor)
        {
            return 1;
        }

        if (Minor < o.Minor)
        {
            return -1;
        }

        if (Revision > o.Revision)
        {
            return 1;
        }

        if (Revision < o.Revision)
        {
            return -1;
        }

        // Don't test patch for EC since it is always 0 but compatible with classic non-zero
        if (Type == ClientType.SA || o.Type == ClientType.SA)
        {
            return 0;
        }

        if (Patch > o.Patch)
        {
            return 1;
        }

        if (Patch < o.Patch)
        {
            return -1;
        }

        return 0;
    }

    int IComparer<ClientVersion>.Compare(ClientVersion x, ClientVersion y) => Compare(x, y);

    public static bool operator >=(ClientVersion l, ClientVersion r) => Compare(l, r) >= 0;

    public static bool operator >(ClientVersion l, ClientVersion r) => Compare(l, r) > 0;

    public static bool operator <=(ClientVersion l, ClientVersion r) => Compare(l, r) <= 0;

    public static bool operator <(ClientVersion l, ClientVersion r) => Compare(l, r) < 0;

    public static bool operator ==(ClientVersion l, ClientVersion r) => Equals(l, r);

    public static bool operator !=(ClientVersion l, ClientVersion r) => !Equals(l, r);

    private string ToStringImpl()
    {
        using var builder = ValueStringBuilder.Create();

        if (Type == ClientType.SA)
        {
            builder.Append($"{Major + 60:00}.{Minor:00}.{Revision:00}");
        }
        else if (Major > 5 || Minor > 0 || Revision > 6)
        {
            builder.Append($"{Major}.{Minor}.{Revision}.{Patch}");
        }
        else if (Patch > 0)
        {
            builder.Append($"{Major}.{Minor}.{Revision}{(char)('a' + (Patch - 1))}");
        }
        else
        {
            builder.Append($"{Major}.{Minor}.{Revision}");
        }

        if (Type == ClientType.UOTD)
        {
            builder.Append(" uotd");
        }

        return builder.ToString();
    }

    public override string ToString() => SourceString;

    public static bool IsNull(object x) => ReferenceEquals(x, null);

    public static int Compare(ClientVersion a, ClientVersion b)
    {
        if (IsNull(a) && IsNull(b))
        {
            return 0;
        }

        if (IsNull(a))
        {
            return -1;
        }

        if (IsNull(b))
        {
            return 1;
        }

        return a.CompareTo(b);
    }

    public ProtocolChanges ProtocolChanges
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this switch
        {
            var v when v.Type is ClientType.KR && v >= Version60142KR => ProtocolChanges.Version60142,
            var v when v.Type is ClientType.KR                        => ProtocolChanges.Version6000,
            var v when v >= Version70610                              => ProtocolChanges.Version70610,
            var v when v >= Version70500                              => ProtocolChanges.Version70500,
            var v when v >= Version704565                             => ProtocolChanges.Version704565,
            var v when v >= Version70331                              => ProtocolChanges.Version70331,
            var v when v >= Version70300                              => ProtocolChanges.Version70300,
            var v when v >= Version70160                              => ProtocolChanges.Version70160,
            var v when v >= Version70130                              => ProtocolChanges.Version70130,
            var v when v >= Version7090                               => ProtocolChanges.Version7090,
            var v when v >= Version7000                               => ProtocolChanges.Version7000,
            var v when v >= Version60142                              => ProtocolChanges.Version60142,
            var v when v >= Version6017                               => ProtocolChanges.Version6017,
            var v when v >= Version6000                               => ProtocolChanges.Version6000,
            var v when v >= Version502b                               => ProtocolChanges.Version502b,
            _                               => ProtocolChanges.Version500a, // We do not support versions lower than 5.0.0a
        };
    }

    public bool Equals(ClientVersion other) =>
        !ReferenceEquals(null, other) && (ReferenceEquals(this, other) || Major == other.Major &&
            Minor == other.Minor && Revision == other.Revision && Patch == other.Patch && Type == other.Type);

    public override bool Equals(object obj) =>
        !ReferenceEquals(null, obj) &&
        (ReferenceEquals(this, obj) || obj.GetType() == GetType() && Equals((ClientVersion)obj));

    public override int GetHashCode() => HashCode.Combine(Major, Minor, Revision, Patch);
}
