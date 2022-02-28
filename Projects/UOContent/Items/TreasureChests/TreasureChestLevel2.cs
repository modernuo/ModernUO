using System;

namespace Server.Items;

[Serializable(0, false)]
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
            var reagent = Loot.RandomReagent();
            reagent.Amount = Utility.Random(1, 2);
            DropItem(reagent);
        }

        // Scrolls
        if (Utility.RandomBool())
        {
            var scroll = Loot.RandomScroll(0, 39, SpellbookType.Regular);
            scroll.Amount = Utility.Random(1, 8);
            DropItem(scroll);
        }

        // Potions
        if (Utility.RandomBool())
        {
            DropItem(Loot.RandomPotion());
        }

        // Gems
        if (Utility.RandomBool())
        {
            var gem = Loot.RandomGem();
            gem.Amount = Utility.Random(1, 6);
            DropItem(gem);
        }
    }

    public override bool Decays => true;

    public override bool IsDecoContainer => false;

    public override TimeSpan DecayTime => TimeSpan.FromMinutes(Utility.Random(15, 60));

    public override int DefaultGumpID => 0x42;

    public override int DefaultDropSound => 0x42;

    public override Rectangle2D Bounds => new(18, 105, 144, 73);

    private static readonly ValueTuple<int, int>[] _chestAppearances =
    {
        // Large Crate
        ValueTuple.Create(0xe3c, 0x44),
        ValueTuple.Create(0xe3d, 0x44),

        // Medium Crate
        ValueTuple.Create(0xe3e, 0x44),
        ValueTuple.Create(0xe3f, 0x44),

        // Small Crate
        ValueTuple.Create(0x9a9, 0x44),
        ValueTuple.Create(0xe7e, 0x44),

        // Wooden Chest
        ValueTuple.Create(0xe42, 0x49),
        ValueTuple.Create(0xe43, 0x49),

        // Metal Chest
        ValueTuple.Create(0x9ab, 0x4A),
        ValueTuple.Create(0xe7c, 0x4A),

        // Metal Golden Chest
        ValueTuple.Create(0xe40, 0x42),
        ValueTuple.Create(0xe41, 0x42),

        // Keg
        ValueTuple.Create(0xe7f, 0x3e),

        // Barrel
        ValueTuple.Create(0xe77, 0x3e),
    };

    private void SetChestAppearance()
    {
        (ItemID, GumpID) = _chestAppearances.RandomElement();
    }
}
