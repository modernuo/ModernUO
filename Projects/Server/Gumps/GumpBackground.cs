using Server.Network;

namespace Server.Gumps
{
    public class GumpBackground : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("resizepic");

        public GumpBackground(int x, int y, int width, int height, int gumpID)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            GumpID = gumpID;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int GumpID { get; set; }

        public override string Compile(NetState ns) => $"{{ resizepic {X} {Y} {GumpID} {Width} {Height} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(X);
            disp.AppendLayout(Y);
            disp.AppendLayout(GumpID);
            disp.AppendLayout(Width);
            disp.AppendLayout(Height);
        }
    }
}
