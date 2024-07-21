using Server.Multis;

namespace Server.ContextMenus
{
    public class EjectPlayerEntry : ContextMenuEntry
    {
        public EjectPlayerEntry() : base(6206, 12)
        {
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (target is not Mobile targetMobile)
            {
                return;
            }

            var house = BaseHouse.FindHouseAt(targetMobile);

            if (!from.Alive || house.Deleted || !house.IsFriend(from))
            {
                return;
            }

            house.Kick(from, targetMobile);
        }
    }
}
