using System.Collections.Generic;
using Server.ContextMenus;
using Server.Multis;
using Server.Spells;

namespace Server.Items
{
  public class SpellScroll : Item, ICommodity
  {
    [Constructible]
    public SpellScroll(int spellID, int itemID, int amount = 1) : base(itemID)
    {
      Stackable = true;
      Weight = 1.0;
      Amount = amount;

      SpellID = spellID;
    }

    public SpellScroll(Serial serial) : base(serial)
    {
    }

    public int SpellID { get; private set; }

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => Core.ML;

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version

      writer.Write(SpellID);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      switch (version)
      {
        case 0:
          {
            SpellID = reader.ReadInt();

            break;
          }
      }
    }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
      base.GetContextMenuEntries(from, list);

      if (from.Alive && Movable)
        list.Add(new AddToSpellbookEntry());
    }

    public override void OnDoubleClick(Mobile from)
    {
      if (!DesignContext.Check(from))
        return; // They are customizing

      if (!IsChildOf(from.Backpack))
      {
        from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        return;
      }

      Spell spell = SpellRegistry.NewSpell(SpellID, from, this);

      if (spell != null)
        spell.Cast();
      else
        from.SendLocalizedMessage(502345); // This spell has been temporarily disabled.
    }
  }
}
