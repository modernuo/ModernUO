using System.Buffers;
using Server.Collections;
using Server.Network;

namespace Server.Gumps
{
    public class GumpTextEntry : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("textentry");

        public GumpTextEntry(int x, int y, int width, int height, int hue, int entryID, string initialText)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Hue = hue;
            EntryID = entryID;
            InitialText = initialText;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int Hue { get; set; }

        public int EntryID { get; set; }

        public string InitialText { get; set; }

        public override string Compile(NetState ns) =>
            $"{{ textentry {X} {Y} {Width} {Height} {Hue} {EntryID} {Parent.Intern(InitialText)} }}";

        public override string Compile(OrderedHashSet<string> strings) =>
            $"{{ textentry {X} {Y} {Width} {Height} {Hue} {EntryID} {strings.GetOrAdd(InitialText)} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(X);
            disp.AppendLayout(Y);
            disp.AppendLayout(Width);
            disp.AppendLayout(Height);
            disp.AppendLayout(Hue);
            disp.AppendLayout(EntryID);
            disp.AppendLayout(Parent.Intern(InitialText));

            disp.TextEntries++;
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
            writer.WriteAscii(Width.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Height.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(Hue.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(EntryID.ToString());
            writer.Write((byte)0x20); // ' '
            writer.WriteAscii(strings.GetOrAdd(InitialText).ToString());
            writer.Write((ushort)0x207D); // " }"

            entries++;
        }
    }
}
