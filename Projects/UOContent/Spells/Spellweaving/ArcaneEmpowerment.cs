using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Engines.BuffIcons;
using Server.Items;
using Server.Mobiles;

namespace Server.Spells.Spellweaving
{
    public class ArcaneEmpowermentSpell : ArcanistSpell
    {
        private const int SummonHealthBonus = 110;
        private const int DispelDifficultyPerFocus = 10;
        private const int PvPSdiCap = 15;

        private static readonly SpellInfo _info = new("Arcane Empowerment", "Aslavdra", -1);
        private static readonly Dictionary<Mobile, EmpowermentContext> _table = new();

        static ArcaneEmpowermentSpell() => EventSink.Logout += OnLogout;

        public ArcaneEmpowermentSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(4.0);
        public override double RequiredSkill => 24.0;
        public override int RequiredMana => 50;

        internal static TimeSpan GetDuration(int spellweavingFixed, int focusLevel) =>
            TimeSpan.FromSeconds(15 + Math.Max(1, spellweavingFixed / 240) + 2 * focusLevel);

        internal static int GetBaseBonus(int spellweavingFixed) => Math.Max(0, spellweavingFixed / 120);

        internal static int GetSpellDamageBonus(int spellweavingFixed, int focusLevel, bool playerTarget) =>
            GetBaseBonus(spellweavingFixed) + focusLevel * (playerTarget ? 1 : 5);

        internal static int GetHealingBonus(int spellweavingFixed, int focusLevel) =>
            GetBaseBonus(spellweavingFixed) + focusLevel * 5;

        internal static int GetDispelDifficultyBonus(int focusLevel) => focusLevel * DispelDifficultyPerFocus;

        internal static bool IsUnderEffects(Mobile caster) => caster != null && _table.ContainsKey(caster);

        public override void OnCast()
        {
            if (CheckSequence())
            {
                ApplyEffect(Caster, Caster.Skills.Spellweaving.Fixed, FocusLevel);
                Caster.PlaySound(0x5C1);
            }

            FinishSequence();
        }

        internal static void ApplyEffectForTests(Mobile caster, int spellweavingFixed, int focusLevel)
        {
            ApplyEffect(caster, spellweavingFixed, focusLevel);
        }

        internal static void StopEffect(Mobile caster)
        {
            if (caster == null || !_table.Remove(caster, out var context))
            {
                return;
            }

            context.TimerToken.Cancel();

            foreach (var follower in context.Followers)
            {
                if (follower?.Deleted == false && follower.Hits > follower.HitsMax)
                {
                    follower.Hits = follower.HitsMax;
                }
            }

            (caster as PlayerMobile)?.RemoveBuff(BuffIcon.ArcaneEmpowerment);

            if (caster.Deleted == false && caster.Alive)
            {
                caster.PlaySound(0x5C2);
            }
        }

        internal static void ApplySpellDamage(Mobile caster, Mobile target, ref int damage, bool sdiAlreadyApplied = false)
        {
            if (damage <= 0 || caster?.Alive != true || target?.Deleted != false ||
                !_table.TryGetValue(caster, out var context))
            {
                return;
            }

            var playerTarget = caster.Player && target.Player;
            var bonus = GetSpellDamageBonus(context.SpellweavingFixed, context.FocusLevel, playerTarget);

            if (playerTarget)
            {
                // When the generated damage already includes item SDI, Arcane Empowerment may use
                // only the remaining room so the combined PvP value stays capped.
                var itemSdi = sdiAlreadyApplied ? AosAttributes.GetValue(caster, AosAttribute.SpellDamage) : 0;
                bonus = Math.Min(bonus, Math.Max(0, PvPSdiCap - itemSdi));
            }

            damage = AOS.Scale(damage, 100 + bonus);
        }

        internal static void ApplyHealing(Mobile caster, ref int amount)
        {
            if (amount <= 0 || caster?.Alive != true || !_table.TryGetValue(caster, out var context))
            {
                return;
            }

            amount = AOS.Scale(amount, 100 + GetHealingBonus(context.SpellweavingFixed, context.FocusLevel));
        }

        internal static int GetFollowerHitsMax(BaseCreature follower, int baseHitsMax)
        {
            if (!TryGetFollowerContext(follower, out var context))
            {
                return baseHitsMax;
            }

            if (context.Followers.Add(follower) && follower.Hits >= baseHitsMax)
            {
                follower.Hits = AOS.Scale(baseHitsMax, SummonHealthBonus);
            }

            return AOS.Scale(baseHitsMax, SummonHealthBonus);
        }

        internal static void AlterFollowerMeleeDamageTo(BaseCreature follower, Mobile target, ref int damage)
        {
            AlterFollowerDamageTo(follower, target, ref damage);
        }

        internal static void AlterFollowerSpellDamageTo(BaseCreature follower, Mobile target, ref int damage)
        {
            AlterFollowerDamageTo(follower, target, ref damage);
        }

        internal static double GetDispelDifficulty(BaseCreature follower)
        {
            return TryGetFollowerContext(follower, out var context)
                ? follower.DispelDifficulty + GetDispelDifficultyBonus(context.FocusLevel)
                : follower.DispelDifficulty;
        }

        [OnEvent(nameof(PlayerMobile.PlayerDeathEvent))]
        [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
        [OnEvent(nameof(BaseCreature.CreatureDeathEvent))]
        [OnEvent(nameof(BaseCreature.CreatureDeletedEvent))]
        public static void OnMobileRemoved(Mobile mobile)
        {
            StopEffect(mobile);

            if (mobile is BaseCreature follower)
            {
                foreach (var context in _table.Values)
                {
                    context.Followers.Remove(follower);
                }
            }
        }

        private static void OnLogout(Mobile mobile) => StopEffect(mobile);

        private static void ApplyEffect(Mobile caster, int spellweavingFixed, int focusLevel)
        {
            StopEffect(caster);

            var duration = GetDuration(spellweavingFixed, focusLevel);
            var context = new EmpowermentContext(spellweavingFixed, focusLevel);
            _table[caster] = context;

            TrackNearbyFollowers(caster);

            if (caster is PlayerMobile player)
            {
                var bonus = GetSpellDamageBonus(spellweavingFixed, focusLevel, false);
                player.AddBuff(
                    new BuffInfo(
                        BuffIcon.ArcaneEmpowerment,
                        1031616,
                        1075808,
                        duration,
                        $"{bonus}\t10"
                    )
                );
            }

            Timer.StartTimer(duration, () => StopEffect(caster), out context.TimerToken);
        }

        private static bool TryGetFollowerContext(BaseCreature follower, out EmpowermentContext context)
        {
            context = null;

            if (follower?.Deleted != false || !follower.Summoned && !follower.IsAnimatedDead)
            {
                return false;
            }

            var master = follower.GetMaster();

            return master?.Alive == true && _table.TryGetValue(master, out context);
        }

        private static void AlterFollowerDamageTo(BaseCreature follower, Mobile target, ref int damage)
        {
            if (!TryGetFollowerContext(follower, out var context))
            {
                return;
            }

            damage = AOS.Scale(
                damage,
                100 + GetSpellDamageBonus(context.SpellweavingFixed, context.FocusLevel, target?.Player == true)
            );
        }

        private static void TrackNearbyFollowers(Mobile caster)
        {
            if (caster.Map == null || caster.Map == Map.Internal)
            {
                return;
            }

            foreach (var follower in caster.GetMobilesInRange<BaseCreature>(18))
            {
                if ((follower.Summoned || follower.IsAnimatedDead) && follower.GetMaster() == caster)
                {
                    GetFollowerHitsMax(follower, GetUnmodifiedHitsMax(follower));
                }
            }
        }

        private static int GetUnmodifiedHitsMax(BaseCreature follower) =>
            follower.HitsMaxSeed <= 0
                ? follower.Str
                : Math.Clamp(follower.HitsMaxSeed + follower.GetStatOffset(StatType.Str), 1, 65000);

        private sealed class EmpowermentContext
        {
            public EmpowermentContext(int spellweavingFixed, int focusLevel)
            {
                SpellweavingFixed = spellweavingFixed;
                FocusLevel = focusLevel;
            }

            public int SpellweavingFixed { get; }
            public int FocusLevel { get; }
            public TimerExecutionToken TimerToken;
            public HashSet<BaseCreature> Followers { get; } = new();
        }
    }
}