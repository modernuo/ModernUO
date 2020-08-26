using System.Collections.Generic;
using Server.ContextMenus;
using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Items
{
  [Flippable(0x234E, 0x234F)]
  public class TapestryOfSosaria : Item, ISecurable
  {
    [Constructible]
    public TapestryOfSosaria() : base(0x234E)
    {
      Weight = 1.0;
      LootType = LootType.Blessed;
    }

    public TapestryOfSosaria(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1062917; // The Tapestry of Sosaria

    [CommandProperty(AccessLevel.GameMaster)]
    public SecureLevel Level { get; set; }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
      base.GetContextMenuEntries(from, list);

      SetSecureLevelEntry.AddTo(from, this, list);
    }

    public override void OnDoubleClick(Mobile from)
    {
      if (from.InRange(GetWorldLocation(), 2))
      {
        from.CloseGump<InternalGump>();
        from.SendGump(new InternalGump());
      }
      else
      {
        from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
      }
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version

      writer.WriteEncodedInt((int)Level);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();

      Level = (SecureLevel)reader.ReadEncodedInt();
    }

    private class InternalGump : Gump
    {
      public InternalGump() : base(50, 50)
      {
        AddImage(0, 0, 0x2C95);
      }
    }
  }
}