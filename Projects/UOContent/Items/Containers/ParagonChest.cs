using ModernUO.Serialization;

namespace Server.Items;

[Flippable]
[SerializationGenerator(0, false)]
public partial class ParagonChest : LockableContainer
{
    private static readonly int[] _itemIDs =
    {
        0x9AB, 0xE40, 0xE41, 0xE7C
    };

    private static readonly int[] _hues =
    {
        0x0, 0x455, 0x47E, 0x89F, 0x8A5, 0x8AB,
        0x966, 0x96D, 0x972, 0x973, 0x979
    };

    [InternString]
    [SerializableField(0, "private", "private")]
    private string _name;

    [Constructible]
    public ParagonChest(string name, int level) : base(_itemIDs.RandomElement())
    {
        _name = name;
        Hue = _hues.RandomElement();
        Fill(level);
    }

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);
        LabelTo(from, 1063449, _name);
    }

    public override void GetProperties(ObjectPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1063449, _name);
    }

    private static void GetRandomAOSStats(out int attributeCount, out int min, out int max)
    {
        var rnd = Utility.Random(15);

        if (rnd < 1)
        {
            attributeCount = Utility.RandomMinMax(2, 6);
            min = 20;
            max = 70;
        }
        else if (rnd < 3)
        {
            attributeCount = Utility.RandomMinMax(2, 4);
            min = 20;
            max = 50;
        }
        else if (rnd < 6)
        {
            attributeCount = Utility.RandomMinMax(2, 3);
            min = 20;
            max = 40;
        }
        else if (rnd < 10)
        {
            attributeCount = Utility.RandomMinMax(1, 2);
            min = 10;
            max = 30;
        }
        else
        {
            attributeCount = 1;
            min = 10;
            max = 20;
        }
    }

    public void Flip()
    {
        ItemID = ItemID switch
        {
            0x9AB => 0xE7C,
            0xE7C => 0x9AB,
            0xE40 => 0xE41,
            0xE41 => 0xE40,
            _     => ItemID
        };
    }

    private void Fill(int level)
    {
        TrapType = TrapType.ExplosionTrap;
        TrapPower = level * 25;
        TrapLevel = level;
        Locked = true;

        RequiredSkill = level switch
        {
            1 => 36,
            2 => 76,
            3 => 84,
            4 => 92,
            5 => 100,
            _ => RequiredSkill
        };

        LockLevel = RequiredSkill - 10;
        MaxLockLevel = RequiredSkill + 40;

        DropItem(new Gold(level * 200));

        for (var i = 0; i < level; ++i)
        {
            DropItem(Loot.RandomScroll(0, 63, SpellbookType.Regular));
        }

        for (var i = 0; i < level * 2; ++i)
        {
            var item = Core.AOS ? Loot.RandomArmorOrShieldOrWeaponOrJewelry() : Loot.RandomArmorOrShieldOrWeapon();

            if (item is BaseWeapon weapon)
            {
                if (Core.AOS)
                {
                    GetRandomAOSStats(out var attributeCount, out var min, out var max);
                    BaseRunicTool.ApplyAttributesTo(weapon, attributeCount, min, max);
                }
                else
                {
                    weapon.DamageLevel = (WeaponDamageLevel)Utility.Random(6);
                    weapon.AccuracyLevel = (WeaponAccuracyLevel)Utility.Random(6);
                    weapon.DurabilityLevel = (WeaponDurabilityLevel)Utility.Random(6);
                }

                DropItem(weapon);
            }
            else if (item is BaseArmor armor)
            {
                if (Core.AOS)
                {
                    GetRandomAOSStats(out var attributeCount, out var min, out var max);
                    BaseRunicTool.ApplyAttributesTo(armor, attributeCount, min, max);
                }
                else
                {
                    armor.ProtectionLevel = (ArmorProtectionLevel)Utility.Random(6);
                    armor.Durability = (ArmorDurabilityLevel)Utility.Random(6);
                }

                DropItem(armor);
            }
            else if (item is BaseHat hat)
            {
                if (Core.AOS)
                {
                    GetRandomAOSStats(out var attributeCount, out var min, out var max);
                    BaseRunicTool.ApplyAttributesTo(hat, attributeCount, min, max);
                }

                DropItem(hat);
            }
            else if (item is BaseJewel jewel)
            {
                GetRandomAOSStats(out var attributeCount, out var min, out var max);
                BaseRunicTool.ApplyAttributesTo(jewel, attributeCount, min, max);

                DropItem(jewel);
            }
        }

        for (var i = 0; i < level; i++)
        {
            var item = Loot.RandomPossibleReagent();
            item.Amount = Utility.RandomMinMax(40, 60);
            DropItem(item);
        }

        for (var i = 0; i < level; i++)
        {
            var item = Loot.RandomGem();
            DropItem(item);
        }

        DropItem(new TreasureMap(level + 1, Utility.RandomBool() ? Map.Felucca : Map.Trammel));
    }
}
