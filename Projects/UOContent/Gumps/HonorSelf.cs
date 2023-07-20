using Server.Engines.Virtues;
using Server.Mobiles;
using Server.Network;

namespace Server.Gumps
{
    public class HonorSelf : Gump
    {
        private readonly PlayerMobile m_from;

        public HonorSelf(PlayerMobile from) : base(150, 50)
        {
            m_from = from;
            AddBackground(0, 0, 245, 145, 9250);
            AddButton(157, 101, 247, 248, 1);
            AddButton(81, 100, 241, 248, 0);
            AddHtml(21, 20, 203, 70, "Are you sure you want to use honor points on yourself?", true);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1)
            {
                HonorVirtue.ActivateEmbrace(m_from);
            }
        }
    }
}
