using Server.Network;

namespace Server.Gumps
{
    public class GumpAlphaRegion : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("checkertrans");

        public GumpAlphaRegion(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public override string Compile(NetState ns) => $"{{ checkertrans {X} {Y} {Width} {Height} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(X);
            disp.AppendLayout(Y);
            disp.AppendLayout(Width);
            disp.AppendLayout(Height);
        }
    }
}
