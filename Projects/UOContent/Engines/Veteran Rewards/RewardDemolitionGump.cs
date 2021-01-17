using Server.Items;
using Server.Multis;
using Server.Network;

namespace Server.Gumps
{
    public class RewardDemolitionGump : Gump
    {
        private readonly IAddon m_Addon;

        public RewardDemolitionGump(IAddon addon, int question) : base(150, 50)
        {
            m_Addon = addon;

            Closable = true;
            Disposable = true;
            Draggable = true;
            Resizable = false;

            AddBackground(0, 0, 220, 170, 0x13BE);
            AddBackground(10, 10, 200, 150, 0xBB8);

            AddHtmlLocalized(20, 30, 180, 60, question); // Do you wish to re-deed this decoration?

            AddHtmlLocalized(55, 100, 150, 25, 1011011); // CONTINUE
            AddButton(20, 100, 0xFA5, 0xFA7, (int)Buttons.Confirm);

            AddHtmlLocalized(55, 125, 150, 25, 1011012); // CANCEL
            AddButton(20, 125, 0xFA5, 0xFA7, (int)Buttons.Cancel);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (!(m_Addon is Item item) || item.Deleted)
            {
                return;
            }

            if (info.ButtonID == (int)Buttons.Confirm)
            {
                var m = sender.Mobile;
                var house = BaseHouse.FindHouseAt(m);

                if (house?.IsOwner(m) == true)
                {
                    if (m.InRange(item.Location, 2))
                    {
                        var deed = m_Addon.Deed;

                        if (deed != null)
                        {
                            m.AddToBackpack(deed);
                            house.Addons.Remove(item);
                            item.Delete();
                        }
                    }
                    else
                    {
                        m.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                    }
                }
                else
                {
                    m.SendLocalizedMessage(
                        1049784
                    ); // You can only re-deed this decoration if you are the house owner or originally placed the decoration.
                }
            }
        }

        private enum Buttons
        {
            Cancel,
            Confirm
        }
    }
}
