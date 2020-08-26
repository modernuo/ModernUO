using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Multis
{
  public class RatCamp : BaseCamp
  {
    private Mobile m_Prisoner;

    [Constructible]
    public RatCamp() : base(0x10EE) // dummy garbage at center
    {
    }

    public RatCamp(Serial serial) : base(serial)
    {
    }

    public virtual Mobile Ratmen => new Ratman();

    public override void AddComponents()
    {
      BaseCreature bc;
      // BaseEscortable be;

      Visible = false;
      DecayDelay = TimeSpan.FromMinutes(5.0);
      AddItem(new Static(0x10ee), 0, 0, 0);
      AddItem(new Static(0xfac), 0, 6, 0);

      switch (Utility.Random(3))
      {
        case 0:
          {
            AddItem(new Item(0xDE3), 0, 6, 0); // Campfire
            AddItem(new Item(0x974), 0, 6, 1); // Cauldron
            break;
          }
        case 1:
          {
            AddItem(new Item(0x1E95), 0, 6, 1); // Rabbit on a spit
            break;
          }
        default:
          {
            AddItem(new Item(0x1E94), 0, 6, 1); // Chicken on a spit
            break;
          }
      }

      AddItem(new Item(0x41F), 5, 5, 0); // Gruesome Standart South

      AddCampChests();

      for (int i = 0; i < 4; i++) AddMobile(Ratmen, 6, Utility.RandomMinMax(-7, 7), Utility.RandomMinMax(-7, 7), 0);

      m_Prisoner = Utility.Random(2) switch
      {
        0 => (Mobile)new Noble(),
        _ => new SeekerOfAdventure()
      };

      // be = (BaseEscortable)m_Prisoner;
      // be.m_Captive = true;

      bc = (BaseCreature)m_Prisoner;
      bc.IsPrisoner = true;
      bc.CantWalk = true;

      m_Prisoner.YellHue = Utility.RandomList(0x57, 0x67, 0x77, 0x87, 0x117);
      AddMobile(m_Prisoner, 2, Utility.RandomMinMax(-2, 2), Utility.RandomMinMax(-2, 2), 0);
    }

    private void AddCampChests()
    {
      var chest = Utility.Random(3) switch
      {
        0 => (LockableContainer)new MetalChest(),
        1 => new MetalGoldenChest(),
        _ => new WoodenChest()
      };

      chest.LiftOverride = true;

      TreasureMapChest.Fill(chest, 1);

      AddItem(chest, -2, -2, 0);

      var crates = Utility.Random(4) switch
      {
        0 => (LockableContainer)new SmallCrate(),
        1 => new MediumCrate(),
        2 => new LargeCrate(),
        _ => new LockableBarrel()
      };

      crates.TrapType = TrapType.ExplosionTrap;
      crates.TrapPower = Utility.RandomMinMax(30, 40);
      crates.TrapLevel = 2;

      crates.RequiredSkill = 76;
      crates.LockLevel = 66;
      crates.MaxLockLevel = 116;
      crates.Locked = true;

      crates.DropItem(new Gold(Utility.RandomMinMax(100, 400)));
      crates.DropItem(new Arrow(10));
      crates.DropItem(new Bolt(10));

      crates.LiftOverride = true;

      if (Utility.RandomDouble() < 0.8)
        switch (Utility.Random(4))
        {
          case 0:
            crates.DropItem(new LesserCurePotion());
            break;
          case 1:
            crates.DropItem(new LesserExplosionPotion());
            break;
          case 2:
            crates.DropItem(new LesserHealPotion());
            break;
          default:
            crates.DropItem(new LesserPoisonPotion());
            break;
        }

      AddItem(crates, 2, 2, 0);
    }

    // Don't refresh decay timer
    public override void OnEnter(Mobile m)
    {
      if (m.Player && m_Prisoner?.CantWalk == true)
      {
        var number = Utility.Random(8) switch
        {
          0 => 502261,
          1 => 502262,
          2 => 502263,
          3 => 502264,
          4 => 502265,
          5 => 502266,
          6 => 502267,
          _ => 502268
        };

        m_Prisoner.Yell(number);
      }
    }

    // Don't refresh decay timer
    public override void OnExit(Mobile m)
    {
    }

    public override void AddItem(Item item, int xOffset, int yOffset, int zOffset)
    {
      if (item != null)
        item.Movable = false;

      base.AddItem(item, xOffset, yOffset, zOffset);
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(1); // version

      writer.Write(m_Prisoner);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      switch (version)
      {
        case 1:
          {
            m_Prisoner = reader.ReadMobile();
            break;
          }
        case 0:
          {
            m_Prisoner = reader.ReadMobile();
            reader.ReadItem();
            break;
          }
      }
    }
  }
}