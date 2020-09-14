using Server.Gumps;

namespace Server.Factions
{
    public abstract class FactionGump : Gump
    {
        public FactionGump(int x, int y) : base(x, y)
        {
        }

        public virtual int ButtonTypes => 10;

        public int ToButtonID(int type, int index) => 1 + index * ButtonTypes + type;

        public bool FromButtonID(int buttonID, out int type, out int index)
        {
            var offset = buttonID - 1;

            if (offset >= 0)
            {
                type = offset % ButtonTypes;
                index = offset / ButtonTypes;
                return true;
            }

            type = index = 0;
            return false;
        }

        public static bool Exists(Mobile mob) => mob.HasGump<FactionGump>();

        public void AddHtmlText(int x, int y, int width, int height, TextDefinition text, bool back, bool scroll)
        {
            if (text?.Number > 0)
            {
                AddHtmlLocalized(x, y, width, height, text.Number, back, scroll);
            }
            else if (text?.String != null)
            {
                AddHtml(x, y, width, height, text.String, back, scroll);
            }
        }
    }
}
