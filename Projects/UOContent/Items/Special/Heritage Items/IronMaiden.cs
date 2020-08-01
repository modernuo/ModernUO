using System;
using Server.Network;
using Server.Spells;

namespace Server.Items
{
  public class IronMaidenAddon : BaseAddon
  {
    public IronMaidenAddon()
    {
      AddComponent(new LocalizedAddonComponent(0x1249, 1076288), 0, 0, 0);
    }

    public IronMaidenAddon(Serial serial) : base(serial)
    {
    }

    public override BaseAddonDeed Deed => new IronMaidenDeed();

    public override void OnComponentUsed(AddonComponent c, Mobile from)
    {
      if (from.InRange(GetWorldLocation(), 2) && from.InLOS(GetWorldLocation()))
      {
        if (Utility.RandomBool())
        {
          from.Location = Location;
          c.ItemID = 0x124A;

          Timer.DelayCall(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5), 3, Activate, c, from);
        }
        else
        {
          from.LocalOverheadMessage(MessageType.Regular, 0,
            501777); // Hmm... you suspect that if you used this again, it might hurt.
        }
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
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();
    }

    public virtual void Activate(AddonComponent c, Mobile from)
    {
      c.ItemID += 1;

      if (c.ItemID < 0x124D)
        return;

      // blood
      int amount = Utility.RandomMinMax(3, 7);

      for (int i = 0; i < amount; i++)
      {
        int x = c.X + Utility.RandomMinMax(-1, 1);
        int y = c.Y + Utility.RandomMinMax(-1, 1);
        int z = c.Z;

        if (!c.Map.CanFit(x, y, z, 1, false, false))
        {
          z = c.Map.GetAverageZ(x, y);

          if (!c.Map.CanFit(x, y, z, 1, false, false))
            continue;
        }

        Blood blood = new Blood(Utility.RandomMinMax(0x122C, 0x122F));
        blood.MoveToWorld(new Point3D(x, y, z), c.Map);
      }

      from.PlaySound(from.Female ? Utility.RandomMinMax(0x150, 0x153) : Utility.RandomMinMax(0x15A, 0x15D));

      from.LocalOverheadMessage(MessageType.Regular, 0,
        501777); // Hmm... you suspect that if you used this again, it might hurt.
      SpellHelper.Damage(TimeSpan.Zero, from, Utility.Dice(2, 10, 5));

      Timer.DelayCall(TimeSpan.FromSeconds(1), Deactivate, c);
    }

    private void Deactivate(AddonComponent c)
    {
      c.ItemID = 0x1249;
    }
  }

  public class IronMaidenDeed : BaseAddonDeed
  {
    [Constructible]
    public IronMaidenDeed() => LootType = LootType.Blessed;

    public IronMaidenDeed(Serial serial) : base(serial)
    {
    }

    public override BaseAddon Addon => new IronMaidenAddon();
    public override int LabelNumber => 1076288; // Iron Maiden

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
}
