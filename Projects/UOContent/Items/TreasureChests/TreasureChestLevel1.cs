using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TreasureChestLevel1 : LockableContainer
{
    [Constructible]
    public TreasureChestLevel1() : base(0xE41)
    {
        SetChestAppearance();
        Movable = false;

        TrapType = TrapType.DartTrap;
        TrapPower = Utility.Random(1, 25);
        Locked = true;

        RequiredSkill = 57;
        LockLevel = RequiredSkill - Utility.Random(1, 10);
        MaxLockLevel = RequiredSkill + Utility.Random(1, 10);

        // According to OSI, loot in level 1 chest is:
        //  Gold 25 - 50
        //  Bolts 10
        //  Gems
        //  Normal weapon
        //  Normal armour
        //  Normal clothing
        //  Normal jewelry

        // Gold
        DropItem(new Gold(Utility.Random(30, 100)));

        // Drop bolts
        // DropItem( new Bolt( 10 ) );

        // Gems
        if (Utility.RandomBool())
        {
            var gems = Loot.RandomGem();
            gems.Amount = Utility.Random(1, 3);
            DropItem(gems);
        }

        // Weapon
        if (Utility.RandomBool())
        {
            DropItem(Loot.RandomWeapon());
        }

        // Armour
        if (Utility.RandomBool())
        {
            DropItem(Loot.RandomArmorOrShield());
        }

        // Clothing
        if (Utility.RandomBool())
        {
            DropItem(Loot.RandomClothing());
        }

        // Jewelry
        if (Utility.RandomBool())
        {
            DropItem(Loot.RandomJewelry());
        }
    }

    public override bool Decays => true;

    public override bool IsDecoContainer => false;

    public override TimeSpan DecayTime => TimeSpan.FromMinutes(Utility.Random(15, 60));

    public override int DefaultGumpID => 0x44;

    public override int DefaultDropSound => 0x42;

    public override Rectangle2D Bounds => new(18, 105, 144, 73);

    private void SetChestAppearance()
    {
        ItemID = Utility.Random(6) switch
        {
            0 => 0xe3c, // Large Crate
            1 => 0xe3d, // Large Crate
            2 => 0xe3e, // Medium Crate
            3 => 0xe3f, // Medium Crate
            4 => 0x9a9, // Small Crate
            _ => 0xe7e  // Small Crate
        };
    }
}
