/***************************************************************************
 *                            GumpHtmlLocalized.cs
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

using Server.Network;

namespace Server.Gumps
{
    public enum GumpHtmlLocalizedType
    {
        Plain,
        Color,
        Args
    }

    public class GumpHtmlLocalized : GumpEntry
    {
        private static readonly byte[] m_LayoutNamePlain = Gump.StringToBuffer("xmfhtmlgump");
        private static readonly byte[] m_LayoutNameColor = Gump.StringToBuffer("xmfhtmlgumpcolor");
        private static readonly byte[] m_LayoutNameArgs = Gump.StringToBuffer("xmfhtmltok");

        public GumpHtmlLocalized(
            int x, int y, int width, int height, int number,
            bool background = false, bool scrollbar = false
        )
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Number = number;
            Background = background;
            Scrollbar = scrollbar;

            Type = GumpHtmlLocalizedType.Plain;
        }

        public GumpHtmlLocalized(
            int x, int y, int width, int height, int number, int color,
            bool background = false, bool scrollbar = false
        )
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Number = number;
            Color = color;
            Background = background;
            Scrollbar = scrollbar;

            Type = GumpHtmlLocalizedType.Color;
        }

        public GumpHtmlLocalized(
            int x, int y, int width, int height, int number, string args, int color,
            bool background = false, bool scrollbar = false
        )
        {
            // Are multiple arguments unsupported? And what about non ASCII arguments?

            X = x;
            Y = y;
            Width = width;
            Height = height;
            Number = number;
            Args = args;
            Color = color;
            Background = background;
            Scrollbar = scrollbar;

            Type = GumpHtmlLocalizedType.Args;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int Number { get; set; }

        public string Args { get; set; }

        public int Color { get; set; }

        public bool Background { get; set; }

        public bool Scrollbar { get; set; }

        public GumpHtmlLocalizedType Type { get; set; }

        public override string Compile(NetState ns)
        {
            return Type switch
            {
                GumpHtmlLocalizedType.Plain =>
                    $"{{ xmfhtmlgump {X} {Y} {Width} {Height} {Number} {(Background ? 1 : 0)} {(Scrollbar ? 1 : 0)} }}",
                GumpHtmlLocalizedType.Color =>
                    $"{{ xmfhtmlgumpcolor {X} {Y} {Width} {Height} {Number} {(Background ? 1 : 0)} {(Scrollbar ? 1 : 0)} {Color} }}",
                _ =>
                    $"{{ xmfhtmltok {X} {Y} {Width} {Height} {(Background ? 1 : 0)} {(Scrollbar ? 1 : 0)} {Color} {Number} @{Args}@ }}"
            };
        }

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            switch (Type)
            {
                case GumpHtmlLocalizedType.Plain:
                    {
                        disp.AppendLayout(m_LayoutNamePlain);

                        disp.AppendLayout(X);
                        disp.AppendLayout(Y);
                        disp.AppendLayout(Width);
                        disp.AppendLayout(Height);
                        disp.AppendLayout(Number);
                        disp.AppendLayout(Background);
                        disp.AppendLayout(Scrollbar);

                        break;
                    }

                case GumpHtmlLocalizedType.Color:
                    {
                        disp.AppendLayout(m_LayoutNameColor);

                        disp.AppendLayout(X);
                        disp.AppendLayout(Y);
                        disp.AppendLayout(Width);
                        disp.AppendLayout(Height);
                        disp.AppendLayout(Number);
                        disp.AppendLayout(Background);
                        disp.AppendLayout(Scrollbar);
                        disp.AppendLayout(Color);

                        break;
                    }

                case GumpHtmlLocalizedType.Args:
                    {
                        disp.AppendLayout(m_LayoutNameArgs);

                        disp.AppendLayout(X);
                        disp.AppendLayout(Y);
                        disp.AppendLayout(Width);
                        disp.AppendLayout(Height);
                        disp.AppendLayout(Background);
                        disp.AppendLayout(Scrollbar);
                        disp.AppendLayout(Color);
                        disp.AppendLayout(Number);
                        disp.AppendLayout(Args);

                        break;
                    }
            }
        }
    }
}
