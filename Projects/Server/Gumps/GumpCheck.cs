using Server.Network;

namespace Server.Gumps
{
    public class GumpCheck : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("checkbox");

        public GumpCheck(int x, int y, int inactiveID, int activeID, bool initialState, int switchID)
        {
            X = x;
            Y = y;
            InactiveID = inactiveID;
            ActiveID = activeID;
            InitialState = initialState;
            SwitchID = switchID;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int InactiveID { get; set; }

        public int ActiveID { get; set; }

        public bool InitialState { get; set; }

        public int SwitchID { get; set; }

        public override string Compile(NetState ns) =>
            $"{{ checkbox {X} {Y} {InactiveID} {ActiveID} {(InitialState ? 1 : 0)} {SwitchID} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(X);
            disp.AppendLayout(Y);
            disp.AppendLayout(InactiveID);
            disp.AppendLayout(ActiveID);
            disp.AppendLayout(InitialState);
            disp.AppendLayout(SwitchID);

            disp.Switches++;
        }
    }
}
