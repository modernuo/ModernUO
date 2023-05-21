using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseImprisonedMobile : Item
{
    [Constructible]
    public BaseImprisonedMobile(int itemID) : base(itemID)
    {
    }

    public abstract BaseCreature Summon { get; }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            from.SendGump(new ConfirmBreakCrystalGump(this));
        }
        else
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
    }

    public virtual void Release(Mobile from, BaseCreature summon)
    {
    }
}
