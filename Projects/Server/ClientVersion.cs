/***************************************************************************
 *                              ClientVersion.cs
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
using System.Text;

namespace Server
{
    public enum ClientType
    {
        Regular,
        UOTD,
        God,
        SA
    }

    public class ClientVersion : IComparable<ClientVersion>, IComparer<ClientVersion>
    {
        public ClientVersion(int maj, int min, int rev, int pat, ClientType type = ClientType.Regular)
        {
            Major = maj;
            Minor = min;
            Revision = rev;
            Patch = pat;
            Type = type;

            SourceString = ToStringImpl();
        }

        public ClientVersion(string fmt)
        {
            SourceString = fmt;

            try
            {
                fmt = fmt.ToLower();

                var br1 = fmt.IndexOf('.');
                var br2 = fmt.IndexOf('.', br1 + 1);

                var br3 = br2 + 1;
                while (br3 < fmt.Length && char.IsDigit(fmt, br3))
                    br3++;

                Major = Utility.ToInt32(fmt.Substring(0, br1));
                Minor = Utility.ToInt32(fmt.Substring(br1 + 1, br2 - br1 - 1));
                Revision = Utility.ToInt32(fmt.Substring(br2 + 1, br3 - br2 - 1));

                if (br3 < fmt.Length)
                {
                    if (Major <= 5 && Minor <= 0 && Revision <= 6) // Anything before 5.0.7
                    {
                        if (!char.IsWhiteSpace(fmt, br3))
                            Patch = fmt[br3] - 'a' + 1;
                    }
                    else
                    {
                        Patch = Utility.ToInt32(fmt.Substring(br3 + 1, fmt.Length - br3 - 1));
                    }
                }

                if (fmt.IndexOf("god") >= 0 || fmt.IndexOf("gq") >= 0)
                    Type = ClientType.God;
                else if (fmt.IndexOf("third dawn") >= 0 || fmt.IndexOf("uo:td") >= 0 || fmt.IndexOf("uotd") >= 0 ||
                         fmt.IndexOf("uo3d") >= 0 || fmt.IndexOf("uo:3d") >= 0)
                    Type = ClientType.UOTD;
                else
                    Type = ClientType.Regular;
            }
            catch
            {
                Major = 0;
                Minor = 0;
                Revision = 0;
                Patch = 0;
                Type = ClientType.Regular;
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
                return 1;

            if (Major > o.Major)
                return 1;
            if (Major < o.Major)
                return -1;
            if (Minor > o.Minor)
                return 1;
            if (Minor < o.Minor)
                return -1;
            if (Revision > o.Revision)
                return 1;
            if (Revision < o.Revision)
                return -1;
            if (Patch > o.Patch)
                return 1;
            if (Patch < o.Patch)
                return -1;
            return 0;
        }

        int IComparer<ClientVersion>.Compare(ClientVersion x, ClientVersion y) => Compare(x, y);

        public static bool operator ==(ClientVersion l, ClientVersion r) => Compare(l, r) == 0;

        public static bool operator !=(ClientVersion l, ClientVersion r) => Compare(l, r) != 0;

        public static bool operator >=(ClientVersion l, ClientVersion r) => Compare(l, r) >= 0;

        public static bool operator >(ClientVersion l, ClientVersion r) => Compare(l, r) > 0;

        public static bool operator <=(ClientVersion l, ClientVersion r) => Compare(l, r) <= 0;

        public static bool operator <(ClientVersion l, ClientVersion r) => Compare(l, r) < 0;

        public override int GetHashCode() => Major ^ Minor ^ Revision ^ Patch ^ (int)Type;

        public override bool Equals(object obj)
        {
            var v = obj as ClientVersion;

            return Major == v?.Major
                   && Minor == v.Minor
                   && Revision == v.Revision
                   && Patch == v.Patch
                   && Type == v.Type;
        }

        private string ToStringImpl()
        {
            var builder = new StringBuilder(16);

            builder.Append(Major);
            builder.Append('.');
            builder.Append(Minor);
            builder.Append('.');
            builder.Append(Revision);

            if (Major <= 5 && Minor <= 0 && Revision <= 6) // Anything before 5.0.7
            {
                if (Patch > 0)
                    builder.Append((char)('a' + (Patch - 1)));
            }
            else
            {
                builder.Append('.');
                builder.Append(Patch);
            }

            if (Type != ClientType.Regular)
            {
                builder.Append(' ');
                builder.Append(Type.ToString());
            }

            return builder.ToString();
        }

        public override string ToString() => ToStringImpl();

        public static bool IsNull(object x) => ReferenceEquals(x, null);

        public static int Compare(ClientVersion a, ClientVersion b)
        {
            if (IsNull(a) && IsNull(b))
                return 0;
            if (IsNull(a))
                return -1;
            if (IsNull(b))
                return 1;

            return a.CompareTo(b);
        }
    }
}
