/***************************************************************************
 *                               GumpImageTileButton.cs
 *                            -------------------
 *   begin                : April 26, 2005
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
    public class GumpImageTileButton : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("buttontileart");
        private static readonly byte[] m_LayoutTooltip = Gump.StringToBuffer(" }{ tooltip");

        private GumpButtonType m_Type;

        // Note, on OSI, The tooltip supports ONLY clilocs as far as I can figure out, and the tooltip ONLY works after the buttonTileArt (as far as I can tell from testing)

        public GumpImageTileButton(
            int x, int y, int normalID, int pressedID, int buttonID, GumpButtonType type, int param,
            int itemID, int hue, int width, int height, int localizedTooltip = -1
        )
        {
            X = x;
            Y = y;
            NormalID = normalID;
            PressedID = pressedID;
            ButtonID = buttonID;
            m_Type = type;
            Param = param;

            ItemID = itemID;
            Hue = hue;
            Width = width;
            Height = height;

            LocalizedTooltip = localizedTooltip;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int NormalID { get; set; }

        public int PressedID { get; set; }

        public int ButtonID { get; set; }

        public GumpButtonType Type
        {
            get => m_Type;
            set
            {
                if (m_Type != value)
                {
                    m_Type = value;

                    var parent = Parent;
                }
            }
        }

        public int Param { get; set; }

        public int ItemID { get; set; }

        public int Hue { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int LocalizedTooltip { get; set; }

        public override string Compile(NetState ns)
        {
            if (LocalizedTooltip > 0)
                return
                    $"{{ buttontileart {X} {Y} {NormalID} {PressedID} {(int)m_Type} {Param} {ButtonID} {ItemID} {Hue} {Width} {Height} }}{{ tooltip {LocalizedTooltip} }}";
            return
                $"{{ buttontileart {X} {Y} {NormalID} {PressedID} {(int)m_Type} {Param} {ButtonID} {ItemID} {Hue} {Width} {Height} }}";
        }

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(X);
            disp.AppendLayout(Y);
            disp.AppendLayout(NormalID);
            disp.AppendLayout(PressedID);
            disp.AppendLayout((int)m_Type);
            disp.AppendLayout(Param);
            disp.AppendLayout(ButtonID);

            disp.AppendLayout(ItemID);
            disp.AppendLayout(Hue);
            disp.AppendLayout(Width);
            disp.AppendLayout(Height);

            if (LocalizedTooltip > 0)
            {
                disp.AppendLayout(m_LayoutTooltip);
                disp.AppendLayout(LocalizedTooltip);
            }
        }
    }
}
