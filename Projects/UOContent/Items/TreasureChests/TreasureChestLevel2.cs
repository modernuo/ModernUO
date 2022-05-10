using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TreasureChestLevel2 : LockableContainer
{
    [Constructible]
    public TreasureChestLevel2() : base(0xE41)
    {
        SetChestAppearance();
        Movable = false;

        TrapType = TrapType.ExplosionTrap;
        TrapPower = 2 * Utility.Random(1, 25);
        Locked = true;

        RequiredSkill = 72;
        LockLevel = RequiredSkill - Utility.Random(1, 10);
        MaxLockLevel = RequiredSkill + Utility.Random(1, 10);

        // According to OSI, loot in level 2 chest is:
        //  Gold 80 - 150
        //  Arrows 10
        //  Reagents
        //  Scrolls
        //  Potions
        //  Gems

        // Gold
        DropItem(new Gold(Utility.Random(70, 100)));

        // Drop bolts
        // DropItem( new Arrow( 10 ) );

        // Reagents
        for (var i = Utility.Random(3); i > 0; i--)
        {
            var reagents = Loot.RandomReagent();
            reagents.Amount = Utility.Random(1, 2);
            DropItem(reagents);
        }

        // Scrolls
        if (Utility.RandomBool())
        {
            var scrolls = Loot.RandomScroll(0, 39, SpellbookType.Regular);
            scrolls.Amount = Utility.Random(1, 8);
            DropItem(scrolls);
        }

        // Potions
        if (Utility.RandomBool())
        {
            DropItem(Loot.RandomPotion());
        }

        // Gems
        if (Utility.RandomBool())
        {
            var gems = Loot.RandomGem();
            gems.Amount = Utility.Random(1, 6);
            DropItem(gems);
        }
    }

    public override bool Decays => true;

    public override bool IsDecoContainer => false;

    public override TimeSpan DecayTime => TimeSpan.FromMinutes(Utility.Random(15, 60));

    public override int DefaultGumpID => 0x42;

    public override int DefaultDropSound => 0x42;

    public override Rectangle2D Bounds => new(18, 105, 144, 73);

    private static readonly (int, int)[] _chestAppearances =
    {
        // Large Crate
        (0xe3c, 0x44),
        (0xe3d, 0x44),

        // Medium Crate
        (0xe3e, 0x44),
        (0xe3f, 0x44),

        // Small Crate
        (0x9a9, 0x44),
        (0xe7e, 0x44),

        // Wooden Chest
        (0xe42, 0x49),
        (0xe43, 0x49),

        // Metal Chest
        (0x9ab, 0x4A),
        (0xe7c, 0x4A),

        // Metal Golden Chest
        (0xe40, 0x42),
        (0xe41, 0x42),

        // Keg
        (0xe7f, 0x3e),

        // Barrel
        (0xe77, 0x3e),
    };

    private void SetChestAppearance()
    {
        (ItemID, GumpID) = _chestAppearances.RandomElement();
    }
}
