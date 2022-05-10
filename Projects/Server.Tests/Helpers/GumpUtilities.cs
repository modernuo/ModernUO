using System.Collections.Generic;
using Server.Gumps;
using Server.Network;

namespace Server.Tests.Network
{
    public static class GumpUtilities
    {
        private static readonly byte[] m_BeginLayout = Gump.StringToBuffer("{ ");
        private static readonly byte[] m_EndLayout = Gump.StringToBuffer(" }");

        public static Packet Compile(this Gump g, NetState ns = null)
        {
            IGumpWriter disp;

            if (ns?.Unpack == true)
            {
                disp = new DisplayGumpPacked(g);
            }
            else
            {
                disp = new DisplayGumpFast(g);
            }

            if (!g.Draggable)
            {
                disp.AppendLayout(Gump.NoMove);
            }

            if (!g.Closable)
            {
                disp.AppendLayout(Gump.NoClose);
            }

            if (!g.Disposable)
            {
                disp.AppendLayout(Gump.NoDispose);
            }

            if (!g.Resizable)
            {
                disp.AppendLayout(Gump.NoResize);
            }

            var count = g.Entries.Count;
            var strings = new List<string>();

            for (var i = 0; i < count; ++i)
            {
                var e = g.Entries[i];

                disp.AppendLayout(m_BeginLayout);
                e.AppendToByType(disp, strings);
                disp.AppendLayout(m_EndLayout);
            }

            disp.WriteStrings(strings);

            disp.Flush();

            return (Packet)disp;
        }

        public static int Intern(this List<string> strings, string value)
        {
            var indexOf = strings.IndexOf(value);

            if (indexOf >= 0)
            {
                return indexOf;
            }

            strings.Add(value);
            return strings.Count - 1;
        }

        public static void AppendToByType(this GumpEntry e, IGumpWriter disp, List<string> strings)
        {
            switch (e)
            {
                case GumpAlphaRegion g:
                    {
                        g.AppendTo(disp, strings);
                        break;
                    }
                case GumpBackground g:
                    {
                        g.AppendTo(disp, strings);
                        break;
                    }
                case GumpButton g:
                    {
                        g.AppendTo(disp, strings);
                        break;
                    }
                case GumpCheck g:
                    {
                        g.AppendTo(disp, strings);
                        break;
                    }
                case GumpGroup g:
                    {
                        g.AppendTo(disp, strings);
                        break;
                    }
                case GumpECHandleInput g:
                    {
                        g.AppendTo(disp, strings);
                        break;
                    }
                case GumpHtml g:
                    {
                        g.AppendTo(disp, strings);
                        break;
                    }
                case GumpHtmlLocalized g:
                    {
                        g.AppendTo(disp, strings);
                        break;
                    }
                case GumpImage g:
                    {
                        g.AppendTo(disp, strings);
                        break;
                    }
                case GumpImageTileButton g:
                    {
                        g.AppendTo(disp, strings);
                        break;
                    }
                case GumpImageTiled g:
                    {
                        g.AppendTo(disp, strings);
                        break;
                    }
                case GumpItem g:
                    {
                        g.AppendTo(disp, strings);
                        break;
                    }
                case GumpItemProperty g:
                    {
                        g.AppendTo(disp, strings);
                        break;
                    }
                case GumpLabel g:
                    {
                        g.AppendTo(disp, strings);
                        break;
                    }
                case GumpLabelCropped g:
                    {
                        g.AppendTo(disp, strings);
                        break;
                    }
                case GumpMasterGump g:
                    {
                        g.AppendTo(disp, strings);
                        break;
                    }
                case GumpPage g:
                    {
                        g.AppendTo(disp, strings);
                        break;
                    }
                case GumpRadio g:
                    {
                        g.AppendTo(disp, strings);
                        break;
                    }
                case GumpSpriteImage g:
                    {
                        g.AppendTo(disp, strings);
                        break;
                    }
                case GumpTextEntry g:
                    {
                        g.AppendTo(disp, strings);
                        break;
                    }
                case GumpTextEntryLimited g:
                    {
                        g.AppendTo(disp, strings);
                        break;
                    }
                case GumpTooltip g:
                    {
                        g.AppendTo(disp, strings);
                        break;
                    }
            }
        }

        public static void AppendTo(this GumpAlphaRegion g, IGumpWriter disp, List<string> strings)
        {
            disp.AppendLayout(Gump.StringToBuffer("checkertrans"));
            disp.AppendLayout(g.X);
            disp.AppendLayout(g.Y);
            disp.AppendLayout(g.Width);
            disp.AppendLayout(g.Height);
        }

        public static void AppendTo(this GumpBackground g, IGumpWriter disp, List<string> strings)
        {
            disp.AppendLayout(Gump.StringToBuffer("resizepic"));
            disp.AppendLayout(g.X);
            disp.AppendLayout(g.Y);
            disp.AppendLayout(g.GumpID);
            disp.AppendLayout(g.Width);
            disp.AppendLayout(g.Height);
        }

        public static void AppendTo(this GumpButton g, IGumpWriter disp, List<string> strings)
        {
            disp.AppendLayout(Gump.StringToBuffer("button"));
            disp.AppendLayout(g.X);
            disp.AppendLayout(g.Y);
            disp.AppendLayout(g.NormalID);
            disp.AppendLayout(g.PressedID);
            disp.AppendLayout((int)g.Type);
            disp.AppendLayout(g.Param);
            disp.AppendLayout(g.ButtonID);
        }

        public static void AppendTo(this GumpCheck g, IGumpWriter disp, List<string> strings)
        {
            disp.AppendLayout(Gump.StringToBuffer("checkbox"));
            disp.AppendLayout(g.X);
            disp.AppendLayout(g.Y);
            disp.AppendLayout(g.InactiveID);
            disp.AppendLayout(g.ActiveID);
            disp.AppendLayout(g.InitialState);
            disp.AppendLayout(g.SwitchID);

            disp.Switches++;
        }

        public static void AppendTo(this GumpGroup g, IGumpWriter disp, List<string> strings)
        {
            disp.AppendLayout(Gump.StringToBuffer("group"));
            disp.AppendLayout(g.Group);
        }

        public static void AppendTo(this GumpECHandleInput g, IGumpWriter disp, List<string> strings)
        {
            disp.AppendLayout(Gump.StringToBuffer("echandleinput"));
        }

        public static void AppendTo(this GumpHtml g, IGumpWriter disp, List<string> strings)
        {
            disp.AppendLayout(Gump.StringToBuffer("htmlgump"));
            disp.AppendLayout(g.X);
            disp.AppendLayout(g.Y);
            disp.AppendLayout(g.Width);
            disp.AppendLayout(g.Height);
            disp.AppendLayout(strings.Intern(g.Text));
            disp.AppendLayout(g.Background);
            disp.AppendLayout(g.Scrollbar);
        }

        public static void AppendTo(this GumpHtmlLocalized g, IGumpWriter disp, List<string> strings)
        {
            switch (g.Type)
            {
                case GumpHtmlLocalizedType.Plain:
                    {
                        disp.AppendLayout(Gump.StringToBuffer("xmfhtmlgump"));

                        disp.AppendLayout(g.X);
                        disp.AppendLayout(g.Y);
                        disp.AppendLayout(g.Width);
                        disp.AppendLayout(g.Height);
                        disp.AppendLayout(g.Number);
                        disp.AppendLayout(g.Background);
                        disp.AppendLayout(g.Scrollbar);

                        break;
                    }

                case GumpHtmlLocalizedType.Color:
                    {
                        disp.AppendLayout(Gump.StringToBuffer("xmfhtmlgumpcolor"));

                        disp.AppendLayout(g.X);
                        disp.AppendLayout(g.Y);
                        disp.AppendLayout(g.Width);
                        disp.AppendLayout(g.Height);
                        disp.AppendLayout(g.Number);
                        disp.AppendLayout(g.Background);
                        disp.AppendLayout(g.Scrollbar);
                        disp.AppendLayout(g.Color);

                        break;
                    }

                case GumpHtmlLocalizedType.Args:
                    {
                        disp.AppendLayout(Gump.StringToBuffer("xmfhtmltok"));

                        disp.AppendLayout(g.X);
                        disp.AppendLayout(g.Y);
                        disp.AppendLayout(g.Width);
                        disp.AppendLayout(g.Height);
                        disp.AppendLayout(g.Background);
                        disp.AppendLayout(g.Scrollbar);
                        disp.AppendLayout(g.Color);
                        disp.AppendLayout(g.Number);
                        disp.AppendLayout(g.Args);

                        break;
                    }
            }
        }

        public static void AppendTo(this GumpImage g, IGumpWriter disp, List<string> strings)
        {
            disp.AppendLayout(Gump.StringToBuffer("gumppic"));
            disp.AppendLayout(g.X);
            disp.AppendLayout(g.Y);
            disp.AppendLayout(g.GumpID);

            if (g.Hue != 0)
            {
                disp.AppendLayoutNS(" hue=");
                disp.AppendLayoutNS(g.Hue);
            }

            if (!string.IsNullOrEmpty(g.Class))
            {
                disp.AppendLayoutNS(" class=");
                disp.AppendLayout(Gump.StringToBuffer(g.Class));
            }
        }

        public static void AppendTo(this GumpImageTileButton g, IGumpWriter disp, List<string> strings)
        {
            disp.AppendLayout(Gump.StringToBuffer("buttontileart"));
            disp.AppendLayout(g.X);
            disp.AppendLayout(g.Y);
            disp.AppendLayout(g.NormalID);
            disp.AppendLayout(g.PressedID);
            disp.AppendLayout((int)g.Type);
            disp.AppendLayout(g.Param);
            disp.AppendLayout(g.ButtonID);

            disp.AppendLayout(g.ItemID);
            disp.AppendLayout(g.Hue);
            disp.AppendLayout(g.Width);
            disp.AppendLayout(g.Height);
        }

        public static void AppendTo(this GumpImageTiled g, IGumpWriter disp, List<string> strings)
        {
            disp.AppendLayout(Gump.StringToBuffer("gumppictiled"));
            disp.AppendLayout(g.X);
            disp.AppendLayout(g.Y);
            disp.AppendLayout(g.Width);
            disp.AppendLayout(g.Height);
            disp.AppendLayout(g.GumpID);
        }

        public static void AppendTo(this GumpItem g, IGumpWriter disp, List<string> strings)
        {
            disp.AppendLayout(Gump.StringToBuffer(g.Hue == 0 ? "tilepic" : "tilepichue"));
            disp.AppendLayout(g.X);
            disp.AppendLayout(g.Y);
            disp.AppendLayout(g.ItemID);

            if (g.Hue != 0)
            {
                disp.AppendLayout(g.Hue);
            }
        }

        public static void AppendTo(this GumpItemProperty g, IGumpWriter disp, List<string> strings)
        {
            disp.AppendLayout(Gump.StringToBuffer("itemproperty"));
            disp.AppendLayout(g.Serial);
        }

        public static void AppendTo(this GumpLabel g, IGumpWriter disp, List<string> strings)
        {
            disp.AppendLayout(Gump.StringToBuffer("text"));
            disp.AppendLayout(g.X);
            disp.AppendLayout(g.Y);
            disp.AppendLayout(g.Hue);
            disp.AppendLayout(strings.Intern(g.Text));
        }

        public static void AppendTo(this GumpLabelCropped g, IGumpWriter disp, List<string> strings)
        {
            disp.AppendLayout(Gump.StringToBuffer("croppedtext"));
            disp.AppendLayout(g.X);
            disp.AppendLayout(g.Y);
            disp.AppendLayout(g.Width);
            disp.AppendLayout(g.Height);
            disp.AppendLayout(g.Hue);
            disp.AppendLayout(strings.Intern(g.Text));
        }

        public static void AppendTo(this GumpMasterGump g, IGumpWriter disp, List<string> strings)
        {
            disp.AppendLayout(Gump.StringToBuffer("mastergump"));
            disp.AppendLayout(g.GumpID);
        }

        public static void AppendTo(this GumpPage g, IGumpWriter disp, List<string> strings)
        {
            disp.AppendLayout(Gump.StringToBuffer("page"));
            disp.AppendLayout(g.Page);
        }

        public static void AppendTo(this GumpRadio g, IGumpWriter disp, List<string> strings)
        {
            disp.AppendLayout(Gump.StringToBuffer("radio"));
            disp.AppendLayout(g.X);
            disp.AppendLayout(g.Y);
            disp.AppendLayout(g.InactiveID);
            disp.AppendLayout(g.ActiveID);
            disp.AppendLayout(g.InitialState);
            disp.AppendLayout(g.SwitchID);

            disp.Switches++;
        }

        public static void AppendTo(this GumpSpriteImage g, IGumpWriter disp, List<string> strings)
        {
            disp.AppendLayout(Gump.StringToBuffer("picinpic"));
            disp.AppendLayout(g.X);
            disp.AppendLayout(g.Y);
            disp.AppendLayout(g.GumpID);
            disp.AppendLayout(g.Width);
            disp.AppendLayout(g.Height);
            disp.AppendLayout(g.SX);
            disp.AppendLayout(g.SY);
        }

        public static void AppendTo(this GumpTextEntry g, IGumpWriter disp, List<string> strings)
        {
            disp.AppendLayout(Gump.StringToBuffer("textentry"));
            disp.AppendLayout(g.X);
            disp.AppendLayout(g.Y);
            disp.AppendLayout(g.Width);
            disp.AppendLayout(g.Height);
            disp.AppendLayout(g.Hue);
            disp.AppendLayout(g.EntryID);
            disp.AppendLayout(strings.Intern(g.InitialText));

            disp.TextEntries++;
        }

        public static void AppendTo(this GumpTextEntryLimited g, IGumpWriter disp, List<string> strings)
        {
            disp.AppendLayout(Gump.StringToBuffer("textentrylimited"));
            disp.AppendLayout(g.X);
            disp.AppendLayout(g.Y);
            disp.AppendLayout(g.Width);
            disp.AppendLayout(g.Height);
            disp.AppendLayout(g.Hue);
            disp.AppendLayout(g.EntryID);
            disp.AppendLayout(strings.Intern(g.InitialText));
            disp.AppendLayout(g.Size);

            disp.TextEntries++;
        }

        public static void AppendTo(this GumpTooltip g, IGumpWriter disp, List<string> strings)
        {
            disp.AppendLayout(Gump.StringToBuffer("tooltip"));
            disp.AppendLayout(g.Number);

            if (!string.IsNullOrEmpty(g.Args))
            {
                disp.AppendLayout(g.Args);
            }
        }
    }
}
