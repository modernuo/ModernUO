using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Engines.Quests.Necro;
using Server.Items;
using Server.Mobiles;

namespace Server.Spells.Necromancy;

public class AnimateDeadSpell : NecromancerSpell, ITargetingSpell<Item>
{
    private static readonly SpellInfo _info = new(
        "Animate Dead",
        "Uus Corp",
        203,
        9031,
        Reagent.GraveDust,
        Reagent.DaemonBlood
    );

    private static readonly CreatureGroup[] _groups =
    [
        // Undead group--empty
        new CreatureGroup(SlayerGroup.GetEntryByName(SlayerName.Silver).Types, []),
        // Insects
        new CreatureGroup(
            [
                typeof(DreadSpider), typeof(FrostSpider), typeof(GiantSpider), typeof(GiantBlackWidow),
                typeof(BlackSolenInfiltratorQueen), typeof(BlackSolenInfiltratorWarrior),
                typeof(BlackSolenQueen), typeof(BlackSolenWarrior), typeof(BlackSolenWorker),
                typeof(RedSolenInfiltratorQueen), typeof(RedSolenInfiltratorWarrior),
                typeof(RedSolenQueen), typeof(RedSolenWarrior), typeof(RedSolenWorker),
                typeof(TerathanAvenger), typeof(TerathanDrone), typeof(TerathanMatriarch),
                typeof(TerathanWarrior)
                // TODO: Giant beetle? Ant lion? Ophidians?
            ],
            [
                new SummonEntry(0, typeof(MoundOfMaggots))
            ]
        ),
        // Mounts
        new CreatureGroup(
            [
                typeof(Horse), typeof(Nightmare), typeof(FireSteed),
                typeof(Kirin), typeof(Unicorn)
            ],
            [
                new SummonEntry(10000, typeof(HellSteed)),
                new SummonEntry(0, typeof(SkeletalMount))
            ]
        ),
        // Elementals
        new CreatureGroup(
            [
                typeof(BloodElemental), typeof(EarthElemental), typeof(SummonedEarthElemental),
                typeof(AgapiteElemental), typeof(BronzeElemental), typeof(CopperElemental),
                typeof(DullCopperElemental), typeof(GoldenElemental), typeof(ShadowIronElemental),
                typeof(ValoriteElemental), typeof(VeriteElemental), typeof(PoisonElemental),
                typeof(FireElemental), typeof(SummonedFireElemental), typeof(SnowElemental),
                typeof(AirElemental), typeof(SummonedAirElemental), typeof(WaterElemental),
                typeof(SummonedAirElemental), typeof(AcidElemental)
            ],
            [
                new SummonEntry(5000, typeof(WailingBanshee)),
                new SummonEntry(0, typeof(Wraith))
            ]
        ),
        // Dragons
        new CreatureGroup(
            [
                typeof(AncientWyrm), typeof(Dragon), typeof(GreaterDragon), typeof(SerpentineDragon),
                typeof(ShadowWyrm), typeof(SkeletalDragon), typeof(WhiteWyrm),
                typeof(Drake), typeof(Wyvern), typeof(LesserHiryu), typeof(Hiryu)
            ],
            [
                new SummonEntry(18000, typeof(SkeletalDragon)),
                new SummonEntry(10000, typeof(FleshGolem)),
                new SummonEntry(5000, typeof(Lich)),
                new SummonEntry(3000, typeof(SkeletalKnight), typeof(BoneKnight)),
                new SummonEntry(2000, typeof(Mummy)),
                new SummonEntry(1000, typeof(SkeletalMage), typeof(BoneMagi)),
                new SummonEntry(0, typeof(PatchworkSkeleton))
            ]
        ),
        // Default group
        new CreatureGroup(
            [],
            [
                new SummonEntry(18000, typeof(LichLord)),
                new SummonEntry(10000, typeof(FleshGolem)),
                new SummonEntry(5000, typeof(Lich)),
                new SummonEntry(3000, typeof(SkeletalKnight), typeof(BoneKnight)),
                new SummonEntry(2000, typeof(Mummy)),
                new SummonEntry(1000, typeof(SkeletalMage), typeof(BoneMagi)),
                new SummonEntry(0, typeof(PatchworkSkeleton))
            ]
        )
    ];

    private static readonly Dictionary<Mobile, List<Mobile>> _table = new();

    public AnimateDeadSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.5);

    public override double RequiredSkill => 40.0;
    public override int RequiredMana => 23;

    public void Target(Item item)
    {
        var comp = item as MaabusCoffinComponent;

        if (comp?.Addon is MaabusCoffin addon)
        {
            if (Caster is PlayerMobile { Quest : DarkTidesQuest quest })
            {
                var objective = quest.FindObjective<AnimateMaabusCorpseObjective>();

                if (objective?.Completed == false)
                {
                    addon.Awake(Caster);
                    objective.Complete();
                }
            }

            return;
        }

        if (item is not Corpse c)
        {
            Caster.SendLocalizedMessage(1061084); // You cannot animate that.
        }
        else
        {
            Type type = c.Owner?.GetType();

            if (c.ItemID != 0x2006 || c.Animated || type == typeof(PlayerMobile) || type == null ||
                c.Owner?.Fame < 100 ||
                c.Owner is BaseCreature creature && (creature.Summoned || creature.IsBonded))
            {
                Caster.SendLocalizedMessage(1061085); // There's not enough life force there to animate.
            }
            else
            {
                var group = FindGroup(type);

                if (group != null)
                {
                    if (group._entries.Length == 0 || type == typeof(DemonKnight))
                    {
                        Caster.SendLocalizedMessage(1061086); // You cannot animate undead remains.
                    }
                    else if (CheckSequence())
                    {
                        var p = c.GetWorldLocation();
                        var map = c.Map;

                        if (map != null)
                        {
                            Effects.PlaySound(p, map, 0x1FB);
                            Effects.SendLocationParticles(
                                EffectItem.Create(p, map, EffectItem.DefaultDuration),
                                0x3789,
                                1,
                                40,
                                0x3F,
                                3,
                                9907,
                                0
                            );

                            Timer.StartTimer(
                                TimeSpan.FromSeconds(2.0),
                                () => SummonDelay_Callback(Caster, c, p, map, group)
                            );
                        }
                    }
                }
            }
        }

        FinishSequence();
    }

    public override void OnCast()
    {
        Caster.Target = new SpellTarget<Item>(this);
        Caster.SendLocalizedMessage(1061083); // Animate what corpse?
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    public static void RemoveEffects(Mobile caster)
    {
        if (_table.Remove(caster, out var list))
        {
            foreach (var m in list)
            {
                m.Delete();
            }

            list.Clear();
        }
    }

    private static CreatureGroup FindGroup(Type type)
    {
        for (var i = 0; i < _groups.Length; ++i)
        {
            var group = _groups[i];
            var types = group._types;

            var contains = types.Length == 0;

            for (var j = 0; !contains && j < types.Length; ++j)
            {
                contains = types[j].IsAssignableFrom(type);
            }

            if (contains)
            {
                return group;
            }
        }

        return null;
    }

    public static void Unregister(Mobile master, Mobile summoned)
    {
        if (master == null || !_table.TryGetValue(master, out var list))
        {
            return;
        }

        if (list.Remove(summoned) && list.Count == 0)
        {
            _table.Remove(master);
        }
    }

    public static void Register(Mobile master, Mobile summoned)
    {
        if (master == null)
        {
            return;
        }

        if (!_table.TryGetValue(master, out var list))
        {
            _table[master] = list = [];
        }

        for (var i = list.Count - 1; i >= 0; --i)
        {
            if (i >= list.Count)
            {
                continue;
            }

            var mob = list[i];

            if (mob.Deleted)
            {
                list.RemoveAt(i--);
            }
        }

        list.Add(summoned);

        if (list.Count > 3)
        {
            var toKill = list[0];
            Unregister(master, toKill);
            toKill.Kill();
        }

        Timer.DelayCall(
            TimeSpan.FromMilliseconds(1650),
            TimeSpan.FromMilliseconds(1650),
            Summoned_Damage,
            summoned
        );
    }

    private static void Summoned_Damage(Mobile mob)
    {
        if (mob.Hits > 0)
        {
            --mob.Hits;
        }
        else
        {
            mob.Kill();
        }
    }

    private static void SummonDelay_Callback(Mobile caster, Corpse corpse, Point3D loc, Map map, CreatureGroup group)
    {
        if (corpse.Animated || corpse.Deleted || caster.Deleted)
        {
            return;
        }

        var owner = corpse.Owner;

        if (owner == null)
        {
            return;
        }

        var necromancy = caster.Skills.Necromancy.Value;
        var spiritSpeak = caster.Skills.SpiritSpeak.Value;

        var casterAbility = (int)(necromancy * 30) + (int)(spiritSpeak * 70);
        casterAbility = Math.Clamp(casterAbility / 10 * 18, 0, owner.Fame);

        Type toSummon = null;
        var entries = group._entries;

        for (var i = 0; toSummon == null && i < entries.Length; ++i)
        {
            var entry = entries[i];

            if (casterAbility < entry._requirement)
            {
                continue;
            }

            var animates = entry._toSummon;

            toSummon = animates.RandomElement();
        }

        if (toSummon == null)
        {
            return;
        }

        Mobile summoned = null;

        try
        {
            summoned = toSummon.CreateInstance<Mobile>();
        }
        catch
        {
            // ignored
        }

        if (summoned == null)
        {
            return;
        }

        if (summoned is BaseCreature bc)
        {
            // to be sure
            bc.Tamable = false;

            bc.ControlSlots = bc is BaseMount ? 1 : 0;

            Effects.PlaySound(loc, map, bc.GetAngerSound());

            BaseCreature.Summon(bc, false, caster, loc, 0x28, TimeSpan.FromDays(1.0));
        }

        if (summoned is SkeletalDragon dragon)
        {
            Scale(dragon, 50); // lose 50% hp and strength
        }

        summoned.Fame = 0;
        summoned.Karma = -1500;
        summoned.MoveToWorld(loc, map);

        corpse.Hue = 1109;
        corpse.Animated = true;

        Register(caster, summoned);
    }

    public static void Scale(BaseCreature bc, int scalar)
    {
        var toScale = bc.RawStr;
        bc.RawStr = AOS.Scale(toScale, scalar);

        toScale = bc.HitsMaxSeed;

        if (toScale > 0)
        {
            bc.HitsMaxSeed = AOS.Scale(toScale, scalar);
        }

        bc.Hits = bc.Hits; // refresh hits
    }

    private class CreatureGroup
    {
        public readonly SummonEntry[] _entries;
        public readonly Type[] _types;

        public CreatureGroup(Type[] types, SummonEntry[] entries)
        {
            _types = types;
            _entries = entries;
        }
    }

    private class SummonEntry
    {
        public readonly int _requirement;
        public readonly Type[] _toSummon;

        public SummonEntry(int requirement, params Type[] toSummon)
        {
            _toSummon = toSummon;
            _requirement = requirement;
        }
    }
}
