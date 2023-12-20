using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Multis;

[SerializationGenerator(0, false)]
public partial class RatCamp : BaseCamp
{
    [SerializableField(0)]
    private Mobile _prisoner;

    [Constructible]
    public RatCamp() : base(0x10EE) // dummy garbage at center
    {
    }

    public virtual Mobile Ratmen => new Ratman();

    public override void AddComponents()
    {
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

        for (var i = 0; i < 4; i++)
        {
            AddMobile(Ratmen, 6, Utility.RandomMinMax(-7, 7), Utility.RandomMinMax(-7, 7), 0);
        }

        _prisoner = Utility.Random(2) switch
        {
            0 => new Noble(),
            _ => new SeekerOfAdventure()
        };

        var bc = (BaseCreature)_prisoner;
        bc.IsPrisoner = true;
        bc.CantWalk = true;

        _prisoner.YellHue = Utility.RandomList(0x57, 0x67, 0x77, 0x87, 0x117);
        AddMobile(_prisoner, 2, Utility.RandomMinMax(-2, 2), Utility.RandomMinMax(-2, 2), 0);
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
        {
            Item item = Utility.Random(4) switch
            {
                0 => new LesserCurePotion(),
                1 => new LesserExplosionPotion(),
                2 => new LesserHealPotion(),
                _ => new LesserPoisonPotion()
            };

            crates.DropItem(item);
        }

        AddItem(crates, 2, 2, 0);
    }

    // Don't refresh decay timer
    public override void OnEnter(Mobile m)
    {
        if (m.Player && _prisoner?.CantWalk == true)
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

            _prisoner.Yell(number);
        }
    }

    // Don't refresh decay timer
    public override void OnExit(Mobile m)
    {
    }

    public override void AddItem(Item item, int xOffset, int yOffset, int zOffset)
    {
        if (item != null)
        {
            item.Movable = false;
        }

        base.AddItem(item, xOffset, yOffset, zOffset);
    }
}
