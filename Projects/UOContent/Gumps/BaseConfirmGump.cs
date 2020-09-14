using Server.Network;

namespace Server.Gumps
{
    public class BaseConfirmGump : Gump
    {
        public BaseConfirmGump() : base(120, 50)
        {
            Closable = false;
            Disposable = true;
            Draggable = true;
            Resizable = false;

            AddPage(0);

            AddImageTiled(0, 0, 348, 262, 0xA8E);
            AddAlphaRegion(0, 0, 348, 262);
            AddImage(0, 15, 0x27A8);
            AddImageTiled(0, 30, 17, 200, 0x27A7);
            AddImage(0, 230, 0x27AA);
            AddImage(15, 230, 0x280C);
            AddImageTiled(30, 0, 300, 17, 0x280A);
            AddImage(315, 0, 0x280E);
            AddImage(15, 244, 0x280C);
            AddImageTiled(30, 244, 300, 17, 0x280A);
            AddImage(315, 244, 0x280E);
            AddImage(330, 15, 0x27A8);
            AddImageTiled(330, 30, 17, 200, 0x27A7);
            AddImage(330, 230, 0x27AA);
            AddImage(333, 2, 0x2716);
            AddImage(315, 248, 0x2716);
            AddImage(2, 248, 0x2716);
            AddImage(2, 2, 0x2716);
            AddHtmlLocalized(25, 25, 200, 20, TitleNumber, 0x7D00);
            AddImage(25, 40, 0xBBF);
            AddHtmlLocalized(25, 55, 300, 120, LabelNumber, 0xFFFFFF);

            AddRadio(25, 175, 0x25F8, 0x25FB, true, (int)Buttons.Break);
            AddRadio(25, 210, 0x25F8, 0x25FB, false, (int)Buttons.Close);

            AddHtmlLocalized(60, 180, 280, 20, 1074976, 0xFFFFFF);
            AddHtmlLocalized(60, 215, 280, 20, 1074977, 0xFFFFFF);

            AddButton(265, 220, 0xF7, 0xF8, (int)Buttons.Confirm);
        }

        public virtual int TitleNumber // <center>Warning!</center>
            => 1075083;

        public virtual int LabelNumber // Are you sure you wish to select this?
            => 1074975;

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (info.ButtonID == (int)Buttons.Confirm)
            {
                if (info.IsSwitched((int)Buttons.Break))
                {
                    Confirm(state.Mobile);
                }
                else
                {
                    Refuse(state.Mobile);
                }
            }
        }

        public virtual void Confirm(Mobile from)
        {
        }

        public virtual void Refuse(Mobile from)
        {
        }

        private enum Buttons
        {
            Close,
            Break,
            Confirm
        }
    }
}
