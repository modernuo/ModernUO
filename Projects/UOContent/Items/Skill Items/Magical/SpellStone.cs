using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Multis;
using Server.Spells.Mysticism;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SpellStone : SpellScroll
{
    public const int ItemID = 0x4079;

    [Constructible]
    public SpellStone(int spellId) : base(spellId, ItemID, 1)
    {
        Stackable = false;
        LootType = LootType.Blessed;
    }

    public int SpellId => SpellID;

    public override string DefaultName => "a spell stone";

    public override bool Nontransferable => true;

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        var definition = SpellTriggerSpell.GetDefinition(SpellId);

        if (definition != null)
        {
            list.Add(1080166, definition.Name); // Use: ~1_spellName~
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!DesignContext.Check(from))
        {
            return;
        }

        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        SpellTriggerSpell.TryUseStone(this, from);
    }

    internal bool TryUseForTests(Mobile from) => SpellTriggerSpell.TryUseStone(this, from);

    public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted) => false;

    public override bool DropToWorld(Mobile from, Point3D p)
    {
        Delete();
        return false;
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        if (SpellTriggerSpell.GetDefinition(SpellId) == null)
        {
            Delete();
        }
    }
}
