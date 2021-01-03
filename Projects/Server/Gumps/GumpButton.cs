using System.Buffers;
using Server.Collections;
using Server.Network;

namespace Server.Gumps
{
    public enum GumpButtonType
    {
        Page = 0,
        Reply = 1
    }

    public class GumpButton : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("button");

        public GumpButton(
            int x, int y, int normalID, int pressedID, int buttonID,
            GumpButtonType type = GumpButtonType.Reply, int param = 0
        )
        {
            X = x;
            Y = y;
            NormalID = normalID;
            PressedID = pressedID;
            ButtonID = buttonID;
            Type = type;
            Param = param;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int NormalID { get; set; }

        public int PressedID { get; set; }

        public int ButtonID { get; set; }

        public GumpButtonType Type { get; set; }

        public int Param { get; set; }

        public override string Compile(NetState ns) =>
            $"{{ button {X} {Y} {NormalID} {PressedID} {(int)Type} {Param} {ButtonID} }}";

        public override string Compile(OrderedHashSet<string> strings) =>
            $"{{ button {X} {Y} {NormalID} {PressedID} {(int)Type} {Param} {ButtonID} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(X);
            disp.AppendLayout(Y);
            disp.AppendLayout(NormalID);
            disp.AppendLayout(PressedID);
            disp.AppendLayout((int)Type);
            disp.AppendLayout(Param);
            disp.AppendLayout(ButtonID);
        }

        public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches)
        {
            writer.Write((ushort)0x7B20); // "{ "
            writer.Write(m_LayoutName);
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
            writer.Write((ushort)0x207D); // " }"
        }
    }
}
