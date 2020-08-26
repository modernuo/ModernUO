using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Multis
{
  public class BrigandCamp : BaseCamp
  {
    private Mobile m_Prisoner;

    [Constructible]
    public BrigandCamp() : base(0x10EE) // dummy garbage at center
    {
    }

    public BrigandCamp(Serial serial) : base(serial)
    {
    }

    public virtual Mobile Brigands => new Brigand();
    public virtual Mobile Executioners => new Executioner();

    public override void AddComponents()
    {
      Visible = false;
      DecayDelay = TimeSpan.FromMinutes(5.0);

      AddItem(new Static(0x10ee), 0, 0, 0);
      AddItem(new Static(0xfac), 0, 7, 0);

      switch (Utility.Random(3))
      {
        case 0:
          {
            AddItem(new Item(0xDE3), 0, 7, 0); // Campfire
            AddItem(new Item(0x974), 0, 7, 1); // Cauldron
            break;
          }
        case 1:
          {
            AddItem(new Item(0x1E95), 0, 7, 1); // Rabbit on a spit
            break;
          }
        default:
          {
            AddItem(new Item(0x1E94), 0, 7, 1); // Chicken on a spit
            break;
          }
      }

      AddCampChests();

      for (int i = 0; i < 4; i++) AddMobile(Brigands, 6, Utility.RandomMinMax(-7, 7), Utility.RandomMinMax(-7, 7), 0);

      BaseCreature bc = Utility.Random(2) switch
      {
        0 => new Noble(),
        _ => new SeekerOfAdventure()
      };

      bc.IsPrisoner = true;
      bc.CantWalk = true;
      m_Prisoner = bc;

      m_Prisoner.YellHue = Utility.RandomList(0x57, 0x67, 0x77, 0x87, 0x117);
      AddMobile(m_Prisoner, 2, Utility.RandomMinMax(-2, 2), Utility.RandomMinMax(-2, 2), 0);
    }

    private void AddCampChests()
    {
      LockableContainer chest = Utility.Random(3) switch
      {
        0 => new MetalChest(),
        1 => new MetalGoldenChest(),
        _ => new WoodenChest()
      };

      chest.LiftOverride = true;

      TreasureMapChest.Fill(chest, 1);

      AddItem(chest, -2, -2, 0);

      LockableContainer crates = Utility.Random(4) switch
      {
        0 => new SmallCrate(),
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

      crates.DropItem(
        Utility.Random(5) switch
        {
          0 => new LesserCurePotion(),
          1 => new LesserExplosionPotion(),
          2 => new LesserHealPotion(),
          3 => new LesserPoisonPotion(),
          _ => null // 4
        }
      );

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

      writer.Write(0); // version

      writer.Write(m_Prisoner);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      switch (version)
      {
        case 0:
          {
            m_Prisoner = reader.ReadMobile();
            break;
          }
      }
    }
  }
}
