using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class OilFlask : Item
{
    [Constructible]
    public OilFlask(int amount = 1) : base(0x1C18)
    {
        Stackable = true;
        Amount = amount;
    }

    public override int LabelNumber => 1027199; // oil flask
    public virtual int FilledMessageNumber => 1150864; // You fill the lamp with oil.
    public virtual double FillMultiplier => 1;

    public override void OnDoubleClick(Mobile from)
    {
        from.SendLocalizedMessage(501947); // Select lantern to refuel.
        from.BeginTarget(2, true, TargetFlags.None, OnTargetPicked);
    }

    private void OnTargetPicked(Mobile from, object targeted)
    {
        if (targeted is Lantern lantern)
        {
            if (!lantern.Movable || lantern.Protected && from.AccessLevel == AccessLevel.Player)
            {
                from.SendLocalizedMessage(500685); // You can't use that, it belongs to someone else.
                return;
            }

            var wasBurning = lantern.Burning;
            lantern.Douse();
            lantern.Duration = Lantern.FullDuration * FillMultiplier;
            lantern.BurntOut = false;

            if (wasBurning)
            {
                lantern.Ignite();
            }

            from.SendLocalizedMessage(FilledMessageNumber);

            var emptyFlask = new EmptyOilFlask();
            if (!from.PlaceInBackpack(emptyFlask))
            {
                var didStack = false;
                var eable = from.GetItemsInRange(0);

                foreach (var i in eable)
                {
                    if (i.StackWith(from, this, false))
                    {
                        didStack = true;
                        break;
                    }
                }

                if (!didStack)
                {
                    emptyFlask.MoveToWorld(Location, Map);
                }
            }

            Consume(1);
        }
        else
        {
            from.SendLocalizedMessage(502218); // You cannot use that.
        }
    }
}

[SerializationGenerator(0)]
public partial class FishOilFlask : OilFlask
{
    [Constructible]
    public FishOilFlask(int amount = 1) : base(amount)
    {
    }

    public override int LabelNumber => 1150863; // fish oil flask
    public override int FilledMessageNumber => 1150865; // You fill the lamp with fish oil. It should burn for a nice long time.
    public override double FillMultiplier => 2;
}

[SerializationGenerator(0)]
public partial class EmptyOilFlask : Item
{
    [Constructible]
    public EmptyOilFlask(int amount = 1) : base(0x1C18)
    {
        Stackable = true;
        Amount = amount;
    }

    public override int LabelNumber => 1150866; // empty oil flask

    public override void OnDoubleClick(Mobile from)
    {
        from.SendLocalizedMessage(500318); // This flask is empty.
    }
}
