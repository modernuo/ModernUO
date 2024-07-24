using Server.Items;

namespace Server.ContextMenus
{
    public class EatEntry : ContextMenuEntry
    {
        public EatEntry() : base(6135, 1)
        {
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (from.CheckAlive() && target is Food { Deleted: false, Movable: true } food && food.CheckItemUse(from))
            {
                food.Eat(from);
            }
        }
    }
}
