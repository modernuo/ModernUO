using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Engines.BuffIcons;
using Server.Mobiles;
using Server.Spells.Fifth;
using Server.Spells.First;
using Server.Spells.Second;
using Server.Targeting;

namespace Server.Spells.Mysticism;

public class PurgeMagicSpell : MysticSpell, ITargetingSpell<Mobile>
{
    private const int StandardPurgeImmunitySeconds = 8;
    private const int HistoricalSkillScaledImmunityMinSeconds = 1;
    private const int HistoricalSkillScaledImmunityMaxSeconds = 6;
    private const int ManaDisruptionSeconds = 8;
    private const int ManaDisruptionAdditionalImmunitySeconds = 16;
    private const double MinManaDisruptionScalar = 1.10;
    private const double MaxManaDisruptionScalar = 1.50;
    private const int MaxManaDisruptionDamage = 40;

    private static readonly SpellInfo _info = new(
        "Purge Magic",
        "An Ort Sanct",
        -1,
        9002,
        Reagent.FertileDirt,
        Reagent.Garlic,
        Reagent.MandrakeRoot,
        Reagent.SulfurousAsh
    );

    private static readonly Dictionary<Mobile, DateTime> _purgeImmunity = new();
    private static readonly Dictionary<Mobile, Dictionary<PurgeWardType, DateTime>> _rePurgeableWards = new();
    private static readonly Dictionary<Mobile, ManaDisruptionContext> _manaDisruptions = new();
    private static bool _applyingDisruptionDamage;

    public PurgeMagicSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Second;

    public static void Configure()
    {
        EventSink.Logout += OnLogout;
    }

    public override bool CheckCast()
    {
        if (IsManaDisrupted(Caster))
        {
            Caster.SendMessage("Your disrupted mana flow prevents you from casting Purge Magic.");
            return false;
        }

        return base.CheckCast();
    }

    public override void OnCast()
    {
        Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Harmful);
    }

    public void Target(Mobile m)
    {
        if (CheckHSequence(m))
        {
            var source = Caster;
            SpellHelper.Turn(source, m);
            SpellHelper.CheckReflect((int)Circle, ref source, ref m);

            if (ApplyPurge(source, m, sendMessages: true))
            {
                HarmfulSpell(m);
            }
        }
    }

    public static bool ApplyPurge(Mobile caster, Mobile target, bool sendMessages = false)
    {
        if (caster == null || target == null || caster.Deleted || target.Deleted || !caster.Alive || !target.Alive)
        {
            return false;
        }

        if (TryPurgeWard(caster, target, out var wardName))
        {
            if (wardName.Length == 0)
            {
                return false;
            }

            StartPurgeImmunity(target, TimeSpan.FromSeconds(StandardPurgeImmunitySeconds));

            if (sendMessages)
            {
                caster.SendMessage($"You purge {wardName} from your target.");
                target.SendMessage($"Your {wardName} ward has been purged.");
            }

            target.FixedParticles(0x3728, 1, 13, 0x26B8, 0x834, 7, EffectLayer.Head, 0);
            target.PlaySound(0x655);
            return true;
        }

        if (IsImmuneToPurge(target, null))
        {
            if (sendMessages)
            {
                caster.SendMessage("That target is temporarily immune to purge effects.");
            }

            return false;
        }

        return ApplyManaDisruption(caster, target, sendMessages);
    }

    public static bool TryPurgeWard(Mobile caster, Mobile target, out string wardName)
    {
        wardName = null;

        if (!TryGetRandomPurgeableWard(target, out var ward))
        {
            return false;
        }

        if (IsImmuneToPurge(target, ward.Type))
        {
            wardName = string.Empty;
            return true;
        }

        if (CheckPurgeResisted(caster, target, ward.Circle))
        {
            target.SendMessage("You resist the purge magic.");
            wardName = string.Empty;
            return true;
        }

        if (!ward.Remove(target))
        {
            return false;
        }

        wardName = ward.Name;
        MarkRePurgeable(target, ward.Type, TimeSpan.FromSeconds(StandardPurgeImmunitySeconds));
        return true;
    }

    public static bool ApplyManaDisruption(Mobile caster, Mobile target, bool sendMessages = false)
    {
        if (_manaDisruptions.ContainsKey(target) || target.Deleted || !target.Alive)
        {
            return false;
        }

        var skillTotal = caster.Skills.Mysticism.Value + Math.Max(caster.Skills.Focus.Value, caster.Skills.Imbuing.Value);
        var scalar = Math.Clamp(1.0 + skillTotal / 240.0 * 0.5, MinManaDisruptionScalar, MaxManaDisruptionScalar);
        var context = new ManaDisruptionContext(caster, target, scalar);
        _manaDisruptions[target] = context;

        Timer.StartTimer(TimeSpan.FromSeconds(ManaDisruptionSeconds), () => EndManaDisruption(target, applyDamage: true), out context.TimerToken);

        if (sendMessages)
        {
            caster.SendMessage("You disrupt the target's mana flow.");
            target.SendMessage("Your mana flow has been disrupted.");
        }

        target.FixedParticles(0x3728, 1, 13, 0x26B8, 0x834, 7, EffectLayer.Head, 0);
        target.PlaySound(0x655);
        return true;
    }

    public static bool EndManaDisruption(Mobile target, bool applyDamage)
    {
        if (!_manaDisruptions.Remove(target, out var context))
        {
            return false;
        }

        context.TimerToken.Cancel();
        StartPurgeImmunity(target, TimeSpan.FromSeconds(ManaDisruptionAdditionalImmunitySeconds));

        if (applyDamage && target?.Deleted == false && target.Alive)
        {
            var elapsed = Core.Now - context.Started;
            var damage = Math.Clamp((int)Math.Ceiling(elapsed.TotalSeconds / ManaDisruptionSeconds * MaxManaDisruptionDamage), 1, MaxManaDisruptionDamage);
            _applyingDisruptionDamage = true;

            try
            {
                AOS.Damage(target, context.Caster, damage, 0, 0, 0, 0, 0, 100);
            }
            finally
            {
                _applyingDisruptionDamage = false;
            }

            target.SendMessage("Chaotic energy rushes back through your disrupted mana flow.");
        }

        return true;
    }

    public static bool IsManaDisrupted(Mobile m) => m != null && _manaDisruptions.ContainsKey(m);

    public static bool GetManaDisruptionScalar(Mobile m, ref double scalar)
    {
        if (m != null && _manaDisruptions.TryGetValue(m, out var context))
        {
            scalar = Math.Max(scalar, context.Scalar);
            return true;
        }

        return false;
    }

    public static bool IsImmuneToPurge(Mobile target, PurgeWardType? wardType)
    {
        if (target == null)
        {
            return false;
        }

        if (wardType.HasValue && IsRePurgeable(target, wardType.Value))
        {
            return false;
        }

        if (IsManaDisrupted(target))
        {
            return true;
        }

        if (_purgeImmunity.TryGetValue(target, out var immuneUntil))
        {
            if (immuneUntil > Core.Now)
            {
                return true;
            }

            _purgeImmunity.Remove(target);
        }

        return false;
    }

    public static void OnMobileDamaged(Mobile attacker, Mobile defender, int damage)
    {
        if (_applyingDisruptionDamage)
        {
            return;
        }

        if (damage > 0 && attacker?.Deleted == false && defender?.Deleted == false && attacker != defender)
        {
            EndManaDisruption(attacker, applyDamage: true);
        }
    }

    public static void ClearState(Mobile m)
    {
        if (m == null)
        {
            return;
        }

        if (_manaDisruptions.Remove(m, out var context))
        {
            context.TimerToken.Cancel();
        }

        _purgeImmunity.Remove(m);
        _rePurgeableWards.Remove(m);
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeathEvent))]
    public static void OnPlayerDeath(PlayerMobile m) => ClearState(m);

    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    public static void OnPlayerDeleted(PlayerMobile m) => ClearState(m);

    private static void OnLogout(Mobile m) => ClearState(m);

    private static bool TryGetRandomPurgeableWard(Mobile target, out PurgeWard selectedWard)
    {
        var selected = default(PurgeWard);
        var wardCount = 0;

        if (MagicReflectSpell.HasEffect(target))
        {
            TrySelectWard(new PurgeWard(PurgeWardType.MagicReflection, "Magic Reflection", SpellCircle.Fifth, MagicReflectSpell.EndReflect));
        }

        if (ProtectionSpell.HasEffect(target))
        {
            TrySelectWard(new PurgeWard(PurgeWardType.Protection, "Protection", SpellCircle.Second, ProtectionSpell.EndProtection));
        }

        if (ReactiveArmorSpell.HasAosEffect(target))
        {
            TrySelectWard(new PurgeWard(PurgeWardType.ReactiveArmor, "Reactive Armor", SpellCircle.First, ReactiveArmorSpell.EndArmor));
        }

        if (HasBless(target))
        {
            TrySelectWard(new PurgeWard(PurgeWardType.Bless, "Bless", SpellCircle.Third, RemoveBless));
        }

        selectedWard = selected;
        return wardCount > 0;

        void TrySelectWard(PurgeWard ward)
        {
            wardCount++;

            if (Utility.Random(wardCount) == 0)
            {
                selected = ward;
            }
        }
    }

    private static bool CheckPurgeResisted(Mobile caster, Mobile target, SpellCircle wardCircle)
    {
        var resistPercent = GetPurgeResistPercent(caster, target, wardCircle);

        if (resistPercent <= 0.0)
        {
            return false;
        }

        if (resistPercent >= 100.0)
        {
            return true;
        }

        if (target.Skills.MagicResist.Value < (1 + (int)wardCircle) * 10)
        {
            target.CheckSkill(SkillName.MagicResist, 0.0, target.Skills.MagicResist.Cap);
        }

        return resistPercent >= Utility.RandomDouble() * 100.0;
    }

    private static double GetPurgeResistPercent(Mobile caster, Mobile target, SpellCircle wardCircle)
    {
        var effectiveSkill = (caster.Skills.Mysticism.Value + Math.Max(caster.Skills.Focus.Value, caster.Skills.Imbuing.Value)) / 2.0;
        var magicResist = target.Skills.MagicResist.Value;
        var firstPercent = magicResist / 5.0;
        var secondPercent = magicResist - ((effectiveSkill - 20.0) / 5.0 + (1 + (int)wardCircle) * 5.0);

        return Math.Max(firstPercent, secondPercent) / 2.0;
    }

    private static bool HasBless(Mobile target) =>
        target.GetStatMod("[Magic] Str Buff") != null &&
        target.GetStatMod("[Magic] Dex Buff") != null &&
        target.GetStatMod("[Magic] Int Buff") != null;

    private static bool RemoveBless(Mobile target)
    {
        if (!HasBless(target))
        {
            return false;
        }

        target.RemoveStatMod("[Magic] Str Buff");
        target.RemoveStatMod("[Magic] Dex Buff");
        target.RemoveStatMod("[Magic] Int Buff");
        (target as PlayerMobile)?.RemoveBuff(BuffIcon.Bless);
        return true;
    }

    private static void StartPurgeImmunity(Mobile target, TimeSpan duration)
    {
        if (target == null || target.Deleted)
        {
            return;
        }

        var until = Core.Now + duration;

        if (!_purgeImmunity.TryGetValue(target, out var current) || current < until)
        {
            _purgeImmunity[target] = until;
            Timer.StartTimer(duration, () => ExpirePurgeImmunity(target, until));
        }
    }

    private static void MarkRePurgeable(Mobile target, PurgeWardType wardType, TimeSpan duration)
    {
        if (target == null || target.Deleted)
        {
            return;
        }

        if (!_rePurgeableWards.TryGetValue(target, out var wards))
        {
            wards = new Dictionary<PurgeWardType, DateTime>();
            _rePurgeableWards[target] = wards;
        }

        var until = Core.Now + duration;
        wards[wardType] = until;
        Timer.StartTimer(duration, () => ExpireRePurgeableWard(target, wardType, until));
    }

    private static void ExpirePurgeImmunity(Mobile target, DateTime until)
    {
        if (_purgeImmunity.TryGetValue(target, out var current) && current <= until)
        {
            _purgeImmunity.Remove(target);
        }
    }

    private static void ExpireRePurgeableWard(Mobile target, PurgeWardType wardType, DateTime until)
    {
        if (!_rePurgeableWards.TryGetValue(target, out var wards) || !wards.TryGetValue(wardType, out var current) || current > until)
        {
            return;
        }

        wards.Remove(wardType);

        if (wards.Count == 0)
        {
            _rePurgeableWards.Remove(target);
        }
    }

    private static bool IsRePurgeable(Mobile target, PurgeWardType wardType)
    {
        if (!_rePurgeableWards.TryGetValue(target, out var wards) || !wards.TryGetValue(wardType, out var until))
        {
            return false;
        }

        if (until > Core.Now)
        {
            return true;
        }

        wards.Remove(wardType);

        if (wards.Count == 0)
        {
            _rePurgeableWards.Remove(target);
        }

        return false;
    }

    public enum PurgeWardType
    {
        MagicReflection,
        Protection,
        ReactiveArmor,
        Bless
    }

    private readonly record struct PurgeWard(
        PurgeWardType Type,
        string Name,
        SpellCircle Circle,
        Func<Mobile, bool> Remove
    );

    private sealed class ManaDisruptionContext
    {
        public ManaDisruptionContext(Mobile caster, Mobile target, double scalar)
        {
            Caster = caster;
            Target = target;
            Scalar = scalar;
            Started = Core.Now;
        }

        public Mobile Caster { get; }
        public Mobile Target { get; }
        public double Scalar { get; }
        public DateTime Started { get; }
        public TimerExecutionToken TimerToken;
    }
}
