using System;

namespace Server.Items;

[Serializable(0, false)]
public partial class TreasureChestLevel3 : LockableContainer
{
    [Constructible]
    public TreasureChestLevel3() : base(0xE41)
    {
        SetChestAppearance();
        Movable = false;

        TrapType = TrapType.PoisonTrap;
        TrapPower = 3 * Utility.Random(1, 25);
        Locked = true;

        RequiredSkill = 84;
        LockLevel = RequiredSkill - Utility.Random(1, 10);
        MaxLockLevel = RequiredSkill + Utility.Random(1, 10);

        // According to OSI, loot in level 3 chest is:
        //  Gold 250 - 350
        //  Arrows 10
        //  Reagents
        //  Scrolls
        //  Potions
        //  Gems
        //  Magic Wand
        //  Magic weapon
        //  Magic armour
        //  Magic clothing  (not implemented)
        //  Magic jewelry  (not implemented)

        // Gold
        DropItem(new Gold(Utility.Random(180, 240)));

        // Drop bolts
        // DropItem( new Arrow( 10 ) );

        // Reagents
        for (var i = Utility.Random(2); i >= 0; i--)
        {
            var ReagentLoot = Loot.RandomReagent();
            ReagentLoot.Amount = Utility.Random(1, 9);
            DropItem(ReagentLoot);
        }

        // Scrolls
        for (var i = Utility.Random(3); i > 0; i--)
        {
            Item ScrollLoot = Loot.RandomScroll(0, 47, SpellbookType.Regular);
            ScrollLoot.Amount = Utility.Random(1, 12);
            DropItem(ScrollLoot);
        }

        // Potions
        for (var i = Utility.Random(3); i > 0; i--)
        {
            var PotionLoot = Loot.RandomPotion();
            DropItem(PotionLoot);
        }

        // Gems
        for (var i = Utility.Random(3); i > 0; i--)
        {
            var GemLoot = Loot.RandomGem();
            GemLoot.Amount = Utility.Random(1, 9);
            DropItem(GemLoot);
        }

        // Magic Wand
        for (var i = Utility.Random(3); i > 0; i--)
        {
            DropItem(Loot.RandomWand());
        }

        // Equipment
        for (var i = Utility.Random(3); i > 0; i--)
        {
            var item = Loot.RandomArmorOrShieldOrWeapon();

            if (item is BaseWeapon weapon)
            {
                weapon.DamageLevel = (WeaponDamageLevel)Utility.Random(3);
                weapon.AccuracyLevel = (WeaponAccuracyLevel)Utility.Random(3);
                weapon.DurabilityLevel = (WeaponDurabilityLevel)Utility.Random(3);
                weapon.Quality = WeaponQuality.Regular;
            }
            else if (item is BaseArmor armor)
            {
                armor.ProtectionLevel = (ArmorProtectionLevel)Utility.Random(3);
                armor.Durability = (ArmorDurabilityLevel)Utility.Random(3);
                armor.Quality = ArmorQuality.Regular;
            }

            DropItem(item);
        }

        // Clothing
        for (var i = Utility.Random(3); i > 0; i--)
        {
            DropItem(Loot.RandomClothing());
        }

        // Jewelry
        for (var i = Utility.Random(3); i > 0; i--)
        {
            DropItem(Loot.RandomJewelry());
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
        // Wooden Chest
        ValueTuple.Create(0xe42, 0x49),
        ValueTuple.Create(0xe43, 0x49),

        // Metal Chest
        ValueTuple.Create(0x9ab, 0x4A),
        ValueTuple.Create(0xe7c, 0x4A),

        // Metal Golden Chest
        ValueTuple.Create(0xe40, 0x42),
        ValueTuple.Create(0xe41, 0x42),
    };

    private void SetChestAppearance()
    {
        (ItemID, GumpID) = _chestAppearances.RandomElement();
    }
}
