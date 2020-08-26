using Server.Gumps;
using Server.Network;

namespace Server.Items
{
  public class VanityAddon : BaseAddonContainer
  {
    [Constructible]
    public VanityAddon(bool east) : base(east ? 0xA44 : 0xA3C)
    {
      if (east) // east
        AddComponent(new AddonContainerComponent(0xA45), 0, -1, 0);
      else // south
        AddComponent(new AddonContainerComponent(0xA3D), -1, 0, 0);
    }

    public VanityAddon(Serial serial) : base(serial)
    {
    }

    public override BaseAddonContainerDeed Deed => new VanityDeed();
    public override int LabelNumber => 1074027; // Vanity
    public override int DefaultGumpID => 0x51;
    public override int DefaultDropSound => 0x42;

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();
    }
  }

  public class VanityDeed : BaseAddonContainerDeed
  {
    private bool m_East;

    [Constructible]
    public VanityDeed() => LootType = LootType.Blessed;

    public VanityDeed(Serial serial) : base(serial)
    {
    }

    public override BaseAddonContainer Addon => new VanityAddon(m_East);
    public override int LabelNumber => 1074027; // Vanity

    public override void OnDoubleClick(Mobile from)
    {
      if (IsChildOf(from.Backpack))
      {
        from.CloseGump<InternalGump>();
        from.SendGump(new InternalGump(this));
      }
      else
      {
        from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
      }
    }

    private void SendTarget(Mobile m)
    {
      base.OnDoubleClick(m);
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();
    }

    private class InternalGump : Gump
    {
      private readonly VanityDeed m_Deed;

      public InternalGump(VanityDeed deed) : base(60, 36)
      {
        m_Deed = deed;

        AddPage(0);

        AddBackground(0, 0, 273, 324, 0x13BE);
        AddImageTiled(10, 10, 253, 20, 0xA40);
        AddImageTiled(10, 40, 253, 244, 0xA40);
        AddImageTiled(10, 294, 253, 20, 0xA40);
        AddAlphaRegion(10, 10, 253, 304);
        AddButton(10, 294, 0xFB1, 0xFB2, 0);
        AddHtmlLocalized(45, 296, 450, 20, 1060051, 0x7FFF); // CANCEL
        AddHtmlLocalized(14, 12, 273, 20, 1076744, 0x7FFF); // Please select your vanity position.

        AddPage(1);

        AddButton(19, 49, 0x845, 0x846, 1);
        AddHtmlLocalized(44, 47, 213, 20, 1075386, 0x7FFF); // South
        AddButton(19, 73, 0x845, 0x846, 2);
        AddHtmlLocalized(44, 71, 213, 20, 1075387, 0x7FFF); // East
      }

      public override void OnResponse(NetState sender, RelayInfo info)
      {
        if (m_Deed?.Deleted != false || info.ButtonID == 0)
          return;

        m_Deed.m_East = info.ButtonID != 1;
        m_Deed.SendTarget(sender.Mobile);
      }
    }
  }
}
