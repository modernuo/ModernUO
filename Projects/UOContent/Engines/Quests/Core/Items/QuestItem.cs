using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.Quests;

[SerializationGenerator(0, false)]
public abstract partial class QuestItem : Item
{
    public QuestItem(int itemID) : base(itemID)
    {
    }

    public virtual bool Accepted => Deleted;

    public abstract bool CanDrop(PlayerMobile pm);

    public override bool DropToWorld(Mobile from, Point3D p)
    {
        var ret = base.DropToWorld(from, p);

        if (ret && !Accepted && Parent != from.Backpack)
        {
            if (from.AccessLevel > AccessLevel.Player)
            {
                return true;
            }

            if (from is not PlayerMobile playerMobile || CanDrop(playerMobile))
            {
                return true;
            }

            // You can only drop quest items into the top-most level of your backpack while you still need them for your quest.
            playerMobile.SendLocalizedMessage(1049343);
            return false;
        }

        return ret;
    }

    public override bool DropToMobile(Mobile from, Mobile target, Point3D p)
    {
        var ret = base.DropToMobile(from, target, p);

        if (ret && !Accepted && Parent != from.Backpack)
        {
            if (from.AccessLevel > AccessLevel.Player)
            {
                return true;
            }

            if (from is not PlayerMobile playerMobile || CanDrop(playerMobile))
            {
                return true;
            }

            // You decide against trading the item.  You still need it for your quest.
            playerMobile.SendLocalizedMessage(1049344);
            return false;
        }

        return ret;
    }

    public override bool DropToItem(Mobile from, Item target, Point3D p)
    {
        var ret = base.DropToItem(from, target, p);

        if (ret && !Accepted && Parent != from.Backpack)
        {
            if (from.AccessLevel > AccessLevel.Player)
            {
                return true;
            }

            if (from is not PlayerMobile playerMobile || CanDrop(playerMobile))
            {
                return true;
            }

            // You can only drop quest items into the top-most level of your backpack while you still need them for your quest.
            playerMobile.SendLocalizedMessage(1049343);
            return false;
        }

        return ret;
    }

    public override DeathMoveResult OnParentDeath(Mobile parent)
    {
        if (parent is PlayerMobile mobile && !CanDrop(mobile))
        {
            return DeathMoveResult.MoveToBackpack;
        }

        return base.OnParentDeath(parent);
    }
}
