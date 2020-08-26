using Server.Network;

namespace Server.Gumps
{
    public class GumpSpriteImage : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("picinpic");

        public GumpSpriteImage(int x, int y, int gumpID, int width, int height, int sx, int sy)
        {
            X = x;
            Y = y;
            GumpID = gumpID;
            Width = width;
            Height = height;
            SX = sx;
            SY = sy;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int GumpID { get; set; }

        public int SX { get; set; }

        public int SY { get; set; }

        public override string Compile(NetState ns) => $"{{ picinpic {X} {Y} {GumpID} {Width} {Height} {SX} {SY} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(X);
            disp.AppendLayout(Y);
            disp.AppendLayout(GumpID);
            disp.AppendLayout(Width);
            disp.AppendLayout(Height);
            disp.AppendLayout(SX);
            disp.AppendLayout(SY);
        }
    }
}
