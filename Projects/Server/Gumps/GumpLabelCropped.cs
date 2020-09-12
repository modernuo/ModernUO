using Server.Network;

namespace Server.Gumps
{
    public class GumpLabelCropped : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("croppedtext");

        public GumpLabelCropped(int x, int y, int width, int height, int hue, string text)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Hue = hue;
            Text = text;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int Hue { get; set; }

        public string Text { get; set; }

        public override string Compile(NetState ns) =>
            $"{{ croppedtext {X} {Y} {Width} {Height} {Hue} {Parent.Intern(Text)} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(X);
            disp.AppendLayout(Y);
            disp.AppendLayout(Width);
            disp.AppendLayout(Height);
            disp.AppendLayout(Hue);
            disp.AppendLayout(Parent.Intern(Text));
        }
    }
}
