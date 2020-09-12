using Server.Network;

namespace Server.Gumps
{
    public class GumpHtml : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("htmlgump");

        public GumpHtml(int x, int y, int width, int height, string text, bool background, bool scrollbar)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Text = text;
            Background = background;
            Scrollbar = scrollbar;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public string Text { get; set; }

        public bool Background { get; set; }

        public bool Scrollbar { get; set; }

        public override string Compile(NetState ns) =>
            $"{{ htmlgump {X} {Y} {Width} {Height} {Parent.Intern(Text)} {(Background ? 1 : 0)} {(Scrollbar ? 1 : 0)} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(X);
            disp.AppendLayout(Y);
            disp.AppendLayout(Width);
            disp.AppendLayout(Height);
            disp.AppendLayout(Parent.Intern(Text));
            disp.AppendLayout(Background);
            disp.AppendLayout(Scrollbar);
        }
    }
}
