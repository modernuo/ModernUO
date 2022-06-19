using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseEquipableLight : BaseLight
{
    [Constructible]
    public BaseEquipableLight(int itemID) : base(itemID) => Layer = Layer.TwoHanded;

    private BaseEquipableLight SplitStack()
    {
        if (!Stackable || Amount < 2)
        {
            return null;
        }

        var stack = Mobile.LiftItemDupe(this, 1);
        stack.BurntOut = BurntOut;
        stack.Duration = Duration;
        stack.Light = Light;
        stack.Protected = Protected;

        return stack;
    }

    public override bool OnEquip(Mobile from)
    {
        if (!base.OnEquip(from))
        {
            return false;
        }

        var stack = SplitStack();
        if (stack != null && stack.Parent != from.Backpack)
        {
            if (from.AddToBackpack(stack))
            {
                if (this is Candle)
                {
                    from.SendLocalizedMessage(502967); // You put the remaining unlit candles into your backpack.
                }
                else if (this is Torch)
                {
                    from.SendLocalizedMessage(502970); // You put the remaining unlit torches into your backpack.
                }
            }
            else
            {
                stack.MoveToWorld(from.Location, from.Map);
            }
        }

        return true;
    }

    public override void Ignite()
    {
        if (Parent is not Mobile && RootParent is Mobile holder)
        {
            if (holder.EquipItem(this))
            {
                if (this is Candle)
                {
                    holder.SendLocalizedMessage(502969); // You put the candle in your left hand.
                }
                else if (this is Torch)
                {
                    holder.SendLocalizedMessage(502972); // You put the torch in your left hand.
                }

                // No message for lanterns?
            }
            else
            {
                SplitStack();
                MoveToWorld(holder.Location, holder.Map);

                if (this is Candle)
                {
                    holder.SendLocalizedMessage(502968); // You cannot hold the candle, so it has been placed at your feet.
                }
                else if (this is Torch)
                {
                    // 502971 has the wrong message
                    holder.SendMessage("You cannot hold the torch, so it has been placed at your feet.");
                }

                // No message for lanterns?
            }
        }
        else
        {
            SplitStack();
        }

        base.Ignite();
    }

    public override void OnAdded(IEntity parent)
    {
        if (Burning && parent is Container)
        {
            Douse();
        }

        base.OnAdded(parent);
    }
}
