using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class GargishOutcast : BaseCreature
{
    private long _nextSummonTick;

    [Constructible]
    public GargishOutcast() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        SetSpeed(0.2, 0.4);
        Race = Race.Gargoyle;
        Title = "the Gargish Outcast";

        if (Utility.RandomBool())
        {
            Name = NameList.RandomName("gargoyle vendor");
            Female = false;
            Body = 666;
        }
        else
        {
            Name = NameList.RandomName("gargoyle vendor");
            Female = true;
            Body = 667;
        }

        Utility.AssignRandomHair(this, true);

        if (!Female)
        {
            Utility.AssignRandomFacialHair(this, true);
        }

        Hue = Race.Gargoyle.RandomSkinHue();

        SetStr(150);
        SetDex(150);
        SetInt(150);

        SetHits(1000, 1200);
        SetMana(450, 600);

        SetDamage(15, 19);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 10, 25);
        SetResistance(ResistanceType.Fire, 40, 65);
        SetResistance(ResistanceType.Cold, 40, 65);
        SetResistance(ResistanceType.Poison, 40, 65);
        SetResistance(ResistanceType.Energy, 40, 65);

        SetSkill(SkillName.MagicResist, 120.0);
        SetSkill(SkillName.Tactics, 50.1, 60.0);
        SetSkill(SkillName.Throwing, 120.0);
        SetSkill(SkillName.Anatomy, 0.0, 10.0);
        SetSkill(SkillName.Magery, 50.0, 80.0);
        SetSkill(SkillName.EvalInt, 50.0, 80.0);
        SetSkill(SkillName.Meditation, 120.0);

        Fame = 12000;
        Karma = -12000;

        BaseWeapon weapon = Utility.Random(3) switch
        {
            0 => new Cyclone(),
            1 => new SoulGlaive(),
            _ => new Boomerang()
        };

        weapon.Attributes.SpellChanneling = 1;
        weapon.LootType = LootType.Blessed;
        AddItem(weapon);

        var chest = new GargishClothChestType1 { Hue = Utility.RandomNeutralHue(), LootType = LootType.Blessed };
        AddItem(chest);

        var arms = new GargishClothArmsType1 { Hue = Utility.RandomNeutralHue(), LootType = LootType.Blessed };
        AddItem(arms);

        var legs = new GargishClothLegsType1 { Hue = Utility.RandomNeutralHue(), LootType = LootType.Blessed };
        AddItem(legs);

        var kilt = new GargishClothKiltType1 { Hue = Utility.RandomNeutralHue(), LootType = LootType.Blessed };
        AddItem(kilt);

        // TODO: When Mysticism is implemented, restore the ServUO 50/50 split:
        // Utility.RandomBool() ? (Necromancy+SpiritSpeak) : (AI_Mystic + Mysticism+Focus)
        // Also add SkillName.Throwing as a usable skill in the Mysticism spell school.
        SetSkill(SkillName.Necromancy, 90.0, 105.0);
        SetSkill(SkillName.SpiritSpeak, 90.0, 105.0);
    }

    public override Poison PoisonImmune => Poison.Deadly;
    public override bool AlwaysMurderer => true;
    public override bool ReacquireOnMovement => true;
    public override bool AcquireOnApproach => true;
    public override int AcquireOnApproachRange => 8;

    public override WeaponAbility GetWeaponAbility() =>
        Weapon is BaseWeapon bw
            ? Utility.RandomBool() ? bw.PrimaryAbility : bw.SecondaryAbility
            : WeaponAbility.WhirlwindAttack;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich);
        AddLoot(LootPack.MedScrolls, 2);
        AddLoot(LootPack.HighScrolls, 2);
    }

    // TODO: Needs world spawn entries in Ter Mur once a spawn file for the region is added.

    public override void OnThink()
    {
        base.OnThink();

        if (Combatant == null || Core.TickCount < _nextSummonTick)
        {
            return;
        }

        if (Mana > 40 && Followers + 4 <= FollowersMax)
        {
            var level = (int)((Skills.Necromancy.Value + Skills.SpiritSpeak.Value) / 2.0);

            var duration = TimeSpan.FromSeconds(10 + level);
            var summon = new AnimatedWeapon(this, level);

            Summon(summon, false, this, Location, 0x212, duration);
            summon.PlaySound(0x64A);

            _nextSummonTick = Core.TickCount + 30000;
        }
    }
}
