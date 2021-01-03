using System.Buffers;
using Server.Collections;
using Server.Network;

namespace Server.Gumps
{
    public class GumpImageTileButton : GumpEntry
    {
        public static readonly byte[] LayoutName = Gump.StringToBuffer("buttontileart");
        public static readonly byte[] LayoutTooltip = Gump.StringToBuffer(" }{ tooltip");

        // Note, on OSI, the tooltip supports ONLY clilocs as far as I can figure out,
        // and the tooltip ONLY works after the buttonTileArt (as far as I can tell from testing)

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
            Type = type;
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

        public GumpButtonType Type { get; set; }

        public int Param { get; set; }

        public int ItemID { get; set; }

        public int Hue { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int LocalizedTooltip { get; set; }

        public override string Compile(OrderedHashSet<string> strings) =>
            LocalizedTooltip > 0 ?
                $"{{ buttontileart {X} {Y} {NormalID} {PressedID} {(int)Type} {Param} {ButtonID} {ItemID} {Hue} {Width} {Height} }}{{ tooltip {LocalizedTooltip} }}" :
                $"{{ buttontileart {X} {Y} {NormalID} {PressedID} {(int)Type} {Param} {ButtonID} {ItemID} {Hue} {Width} {Height} }}";

        public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write((ushort)0x7B20); // "{ "
            writer.Write(LayoutName);
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(X.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Y.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(NormalID.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(PressedID.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(((int)Type).ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Param.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(ButtonID.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(ItemID.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Hue.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Width.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Height.ToString());

            if (LocalizedTooltip > 0)
            {
                writer.Write(LayoutTooltip);
                writer.WriteAscii(LocalizedTooltip.ToString());
            }

            writer.Write((ushort)0x207D); // " }"
        }
    }
}
