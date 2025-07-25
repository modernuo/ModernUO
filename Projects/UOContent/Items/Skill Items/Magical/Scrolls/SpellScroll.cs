using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Multis;
using Server.Spells;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SpellScroll : Item, ICommodity
{
    [SerializableField(0, setter: "private")]
    private int _spellID;

    [Constructible]
    public SpellScroll(int spellID, int itemID, int amount = 1) : base(itemID)
    {
        Stackable = true;
        Amount = amount;

        _spellID = spellID;
    }

    public override double DefaultWeight => 1.0;

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => Core.ML;

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (from.Alive && Movable)
        {
            list.Add(new AddToSpellbookEntry());
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!DesignContext.Check(from))
        {
            return; // They are customizing
        }

        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        var spell = SpellRegistry.NewSpell(_spellID, from, this);

        if (spell != null)
        {
            spell.Cast();
        }
        else
        {
            from.SendLocalizedMessage(502345); // This spell has been temporarily disabled.
        }
    }
}
