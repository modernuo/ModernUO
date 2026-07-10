using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Engines.BuffIcons;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Spells.Ninjitsu;
using Server.Spells.Spellweaving;

namespace Server.Spells.Mysticism;

public class EnchantSpell : MysticSpell
{
    private const int MaxHitSpellChance = 60;
    private const double SpellChannelingSkillThreshold = 80.0;

    private static readonly SpellInfo _info = new(
        "Enchant",
        "In Ort Ylem",
        230,
        9022,
        Reagent.SpidersSilk,
        Reagent.MandrakeRoot,
        Reagent.SulfurousAsh
    );

    private static readonly AosWeaponAttribute[] _hitSpellAttributes =
    [
        AosWeaponAttribute.HitMagicArrow,
        AosWeaponAttribute.HitHarm,
        AosWeaponAttribute.HitFireball,
        AosWeaponAttribute.HitLightning,
        AosWeaponAttribute.HitDispel
    ];

    private static readonly Dictionary<BaseWeapon, EnchantmentTimer> _table = new();
    private static bool _configured;

    private TimerExecutionToken _selectionTimer;

    public EnchantSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Second;

    public static void Configure()
    {
        if (_configured)
        {
            return;
        }

        _configured = true;
        EventSink.Logout += OnLogout;
    }

    public override bool ClearHandsOnCast => false;

    public override bool CheckCast()
    {
        if (!base.CheckCast())
        {
            return false;
        }

        if (Caster.Weapon is not BaseWeapon weapon || weapon is Fists)
        {
            Caster.SendLocalizedMessage(501078); // You must be holding a weapon.
            return false;
        }

        return ValidateWeapon(weapon);
    }

    public override void OnCast()
    {
        if (Caster.Weapon is not BaseWeapon weapon || weapon is Fists)
        {
            Caster.SendLocalizedMessage(501078); // You must be holding a weapon.
            FinishSequence();
        }
        else if (!ValidateWeapon(weapon))
        {
            FinishSequence();
        }
        else
        {
            EnchantGump.DisplayTo(Caster, this, weapon);
            Timer.StartTimer(TimeSpan.FromSeconds(30), CancelSelection, out _selectionTimer);
        }
    }

    public override void OnDisturb(DisturbType type, bool message)
    {
        Caster.CloseGump<EnchantGump>();
        _selectionTimer.Cancel();
        base.OnDisturb(type, message);
    }

    public override void FinishSequence()
    {
        _selectionTimer.Cancel();
        Caster.CloseGump<EnchantGump>();
        base.FinishSequence();
    }

    internal void FinishSelection(BaseWeapon weapon, AosWeaponAttribute attribute)
    {
        if (Caster.Spell != this || State != SpellState.Sequencing)
        {
            return;
        }

        try
        {
            if (Caster.Weapon != weapon || weapon.Deleted || weapon is Fists)
            {
                Caster.SendLocalizedMessage(501078); // You must be holding a weapon.
                return;
            }

            if (!ValidateWeapon(weapon))
            {
                return;
            }

            if (!CheckSequence())
            {
                return;
            }

            var value = GetHitSpellChance(Caster);
            var duration = GetDuration(attribute);
            var grantsAdvancedEffects =
                Caster.Skills.Mysticism.Value >= SpellChannelingSkillThreshold &&
                Math.Max(Caster.Skills.Imbuing.Value, Caster.Skills.Focus.Value) >= SpellChannelingSkillThreshold;
            var grantsSpellChanneling = grantsAdvancedEffects && weapon.Attributes.SpellChanneling == 0;

            var timer = new EnchantmentTimer(
                Caster,
                weapon,
                attribute,
                value,
                grantsSpellChanneling,
                grantsAdvancedEffects,
                duration
            );
            _table[weapon] = timer;
            timer.Start();

            Caster.PlaySound(0x64E);
            Caster.FixedEffect(0x36CB, 1, 9, 1915, 0);
            weapon.InvalidateProperties();

            if (Caster is PlayerMobile player)
            {
                player.AddBuff(
                    new BuffInfo(
                        BuffIcon.Enchant,
                        1080126,
                        GetBuffCliloc(attribute),
                        duration,
                        $"{Caster.Name}\t{value}"
                    )
                );
            }
        }
        finally
        {
            FinishSequence();
        }
    }

    internal void CancelSelection()
    {
        if (Caster.Spell == this && State == SpellState.Sequencing)
        {
            Caster.SendLocalizedMessage(1080132); // You decide not to enchant your weapon.
            FinishSequence();
        }
        else
        {
            _selectionTimer.Cancel();
            Caster.CloseGump<EnchantGump>();
        }
    }

    internal static int GetHitSpellChance(Mobile caster)
    {
        var skillTotal = GetBaseSkill(caster) + Math.Max(caster.Skills.Imbuing.Value, caster.Skills.Focus.Value);
        return Math.Clamp((int)(MaxHitSpellChance * skillTotal / 240.0), 0, MaxHitSpellChance);
    }

    internal static TimeSpan GetDuration(AosWeaponAttribute attribute) =>
        attribute switch
        {
            // Publish 65 states that duration scales with the selected hit spell's level.
            // The issue does not provide an official per-level table, so keep the chosen
            // deterministic current-SA policy explicit and capped at the 150-second maximum.
            AosWeaponAttribute.HitMagicArrow => TimeSpan.FromSeconds(30),
            AosWeaponAttribute.HitHarm => TimeSpan.FromSeconds(60),
            AosWeaponAttribute.HitFireball => TimeSpan.FromSeconds(90),
            AosWeaponAttribute.HitLightning => TimeSpan.FromSeconds(120),
            AosWeaponAttribute.HitDispel => TimeSpan.FromSeconds(150),
            _ => TimeSpan.Zero
        };

    internal static int GetHitSpellBonus(BaseWeapon weapon, AosWeaponAttribute attribute) =>
        _table.TryGetValue(weapon, out var timer) && timer.Attribute == attribute ? timer.Value : 0;

    internal static bool ProvidesSpellChanneling(BaseWeapon weapon, Mobile caster) =>
        _table.TryGetValue(weapon, out var timer) && timer.Caster == caster && timer.Weapon.Parent == caster &&
        timer.GrantsSpellChanneling;

    internal static int GetFasterCasting(Mobile caster)
    {
        foreach (var timer in _table.Values)
        {
            if (timer.Caster == caster && timer.Weapon?.Deleted == false && timer.Weapon.Parent == caster)
            {
                return timer.GrantsFasterCasting ? -1 : 0;
            }
        }

        return 0;
    }

    public static bool IsEnchanted(BaseWeapon weapon) => weapon != null && _table.ContainsKey(weapon);

    public static void StopEffect(BaseWeapon weapon) => StopEffect(weapon, false);

    internal static void ExpireForTests(BaseWeapon weapon) => StopEffect(weapon, true);

    private static void StopEffect(BaseWeapon weapon, bool expired)
    {
        if (weapon == null || !_table.Remove(weapon, out var timer))
        {
            return;
        }

        timer.Stop();
        var caster = timer.Caster;

        if (expired && caster?.Deleted == false)
        {
            caster.SendLocalizedMessage(1115273); // The enchantment on your weapon has expired.
            caster.PlaySound(0x1E6);
        }

        (caster as PlayerMobile)?.RemoveBuff(BuffIcon.Enchant);
        timer.ClearReferences();

        if (!weapon.Deleted)
        {
            weapon.InvalidateProperties();
        }
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeathEvent))]
    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    [OnEvent(nameof(BaseCreature.CreatureDeathEvent))]
    [OnEvent(nameof(BaseCreature.CreatureDeletedEvent))]
    public static void OnCasterRemoved(Mobile caster)
    {
        var weapons = new List<BaseWeapon>();

        foreach (var pair in _table)
        {
            if (pair.Value.Caster == caster)
            {
                weapons.Add(pair.Key);
            }
        }

        for (var i = 0; i < weapons.Count; i++)
        {
            StopEffect(weapons[i]);
        }
    }

    private static void OnLogout(Mobile caster) => OnCasterRemoved(caster);

    private static EnchantmentTimer FindEffect(Mobile caster)
    {
        foreach (var timer in _table.Values)
        {
            if (timer.Caster == caster)
            {
                return timer;
            }
        }

        return null;
    }

    private bool ValidateWeapon(BaseWeapon weapon)
    {
        if (weapon == null || weapon.Deleted || weapon is Fists)
        {
            return false;
        }

        if (FindEffect(Caster) != null || _table.ContainsKey(weapon))
        {
            Caster.SendLocalizedMessage(501775); // You already have an enchantment on a weapon.
            return false;
        }

        if (weapon.Cursed || weapon.Consecrated || ImmolatingWeaponSpell.IsImmolating(weapon))
        {
            Caster.SendLocalizedMessage(1080128); // You cannot enchant that weapon.
            return false;
        }

        if (weapon.Parent is Mobile wielder && SpecialMove.GetCurrentMove(wielder) is FocusAttack)
        {
            Caster.SendLocalizedMessage(1080446); // You cannot enchant your weapon while using Focus Attack.
            return false;
        }

        for (var i = 0; i < _hitSpellAttributes.Length; i++)
        {
            if (weapon.WeaponAttributes[_hitSpellAttributes[i]] > 0)
            {
                Caster.SendLocalizedMessage(1080127); // That weapon already has a hit spell.
                return false;
            }
        }

        return true;
    }

    private static int GetBuffCliloc(AosWeaponAttribute attribute) =>
        attribute switch
        {
            AosWeaponAttribute.HitLightning => 1060423,
            AosWeaponAttribute.HitFireball => 1060420,
            AosWeaponAttribute.HitHarm => 1060421,
            AosWeaponAttribute.HitMagicArrow => 1060426,
            AosWeaponAttribute.HitDispel => 1060417,
            _ => 0
        };

    private sealed class EnchantmentTimer : Timer
    {
        public EnchantmentTimer(
            Mobile caster,
            BaseWeapon weapon,
            AosWeaponAttribute attribute,
            int value,
            bool grantsSpellChanneling,
            bool grantsFasterCasting,
            TimeSpan duration
        ) : base(duration)
        {
            Caster = caster;
            Weapon = weapon;
            Attribute = attribute;
            Value = value;
            GrantsSpellChanneling = grantsSpellChanneling;
            GrantsFasterCasting = grantsFasterCasting;
        }

        public Mobile Caster { get; private set; }

        public BaseWeapon Weapon { get; private set; }

        public AosWeaponAttribute Attribute { get; }

        public int Value { get; }

        public bool GrantsSpellChanneling { get; }

        public bool GrantsFasterCasting { get; }

        protected override void OnTick() => StopEffect(Weapon, true);

        public void ClearReferences()
        {
            Caster = null;
            Weapon = null;
        }
    }
}
