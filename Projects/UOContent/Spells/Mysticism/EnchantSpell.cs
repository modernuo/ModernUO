using Server.Gumps;
using Server.Items;
using Server.Spells.Spellweaving;
using System;
using System.Collections.Generic;
using Server.Network;

namespace Server.Spells.Mysticism
{
    public class EnchantSpell : MysticSpell
    {
        public override SpellCircle Circle => SpellCircle.Second;
        public override bool ClearHandsOnCast => false;

        private static readonly Dictionary<Mobile, EnchantmentTimer> _table = new();

        private BaseWeapon _weapon;
        private AosWeaponAttribute _weaponAttribute;

        private static readonly SpellInfo _info = new(
            "Enchant", "In Ort Ylem",
            230,
            9022,
            Reagent.SpidersSilk,
            Reagent.MandrakeRoot,
            Reagent.SulfurousAsh
        );

        public EnchantSpell(Mobile caster, Item scroll) : base(caster, scroll, _info)
        {
        }

        public EnchantSpell(Mobile caster, Item scroll, BaseWeapon weapon, AosWeaponAttribute attribute)
            : base(caster, scroll, _info)
        {
            _weapon = weapon;
            _weaponAttribute = attribute;
        }

        public override bool CheckCast()
        {
            if (_weapon == null)
            {
                if (Caster.Weapon is not BaseWeapon wep)
                {
                    Caster.SendLocalizedMessage(501078); // You must be holding a weapon.
                }
                else
                {
                    Caster.CloseGump<EnchantSpellGump>();
                    _weapon = wep;

                    EnchantSpellGump gump = new EnchantSpellGump(Caster, this);
                    Caster.SendGump(gump);

                    Timer.StartTimer(TimeSpan.FromSeconds(30), () =>
                    {
                        if (Caster.CloseGump<EnchantSpellGump>())
                        {
                            FinishSequence();
                        }
                    });
                }

                return false;
            }

            return CanEnchantWeapon(Caster, _weapon);
        }

        private static bool CanEnchantWeapon(Mobile m, BaseWeapon weapon)
        {
            if (IsUnderSpellEffects(m, weapon))
            {
                m.SendLocalizedMessage(501775); // This spell is already in effect.
            }
            else if (ImmolatingWeaponSpell.IsImmolating(weapon) || weapon.Consecrated)
            {
                m.SendLocalizedMessage(1080128); // You cannot use this ability while your weapon is enchanted.
            }
            else if (weapon.FocusWeilder != null)
            {
                m.SendLocalizedMessage(1080446); // You cannot enchant an item that is under the effects of the ninjitsu focus attack ability.
            }
            else if (weapon.WeaponAttributes.HitLightning > 0 || weapon.WeaponAttributes.HitFireball > 0
                                                              || weapon.WeaponAttributes.HitHarm > 0
                                                              || weapon.WeaponAttributes.HitMagicArrow > 0
                                                              || weapon.WeaponAttributes.HitDispel > 0)
            {
                m.SendLocalizedMessage(1080127); // This weapon already has a hit spell effect and cannot be enchanted.
            }
            else
            {
                return true;
            }

            return false;
        }

        public override void OnCast()
        {
            if (CanEnchantWeapon(Caster, _weapon) && CheckSequence() && Caster.Weapon == _weapon)
            {
                Caster.PlaySound(0x64E);
                Caster.FixedEffect(0x36CB, 1, 9, 1915, 0);

                int prim = (int)Caster.Skills[CastSkill].Value;
                int sec = (int)Caster.Skills[DamageSkill].Value;

                int value = 60 * (prim + sec) / 240;
                double duration = (prim + sec) / 2.0 + 30.0;
                bool malus;

                Enhancement.SetValue(Caster, _weaponAttribute, value, "EnchantSpell");

                if (prim >= 80 && sec >= 80 && _weapon.Attributes.SpellChanneling == 0)
                {
                    Enhancement.SetValue(Caster, AosAttribute.SpellChanneling, 1, "EnchantSpell");
                    Enhancement.SetValue(Caster, AosAttribute.CastSpeed, -1, "EnchantSpell");
                    malus = true;
                }

                _table[Caster] = new EnchantmentTimer(Caster, _weapon, _weaponAttribute, value, malus, duration);

                int loc = _weaponAttribute switch
                {
                    AosWeaponAttribute.HitFireball   => 1060420,
                    AosWeaponAttribute.HitHarm       => 1060421,
                    AosWeaponAttribute.HitMagicArrow => 1060426,
                    AosWeaponAttribute.HitDispel     => 1060417,
                    _                                => 1060423 // AosWeaponAttribute.HitLightning
                };

                BuffInfo.AddBuff(Caster, new BuffInfo(BuffIcon.Enchant, 1080126, loc, TimeSpan.FromSeconds(duration), Caster, value.ToString()));

                _weapon.EnchantedWeilder = Caster;
                _weapon.InvalidateProperties();
            }

            FinishSequence();
        }

        public static bool IsUnderSpellEffects(Mobile m, BaseWeapon weapon) =>
            _table != null && _table.TryGetValue(m, out var timer) && timer._weapon == weapon;

        public static AosWeaponAttribute BonusAttribute(Mobile m) =>
            _table != null && _table.TryGetValue(m, out var timer) ? timer._weaponAttribute : AosWeaponAttribute.HitColdArea;

        public static int BonusValue(Mobile m) =>
            _table != null && _table.TryGetValue(m, out var timer) ? timer._attributeValue : 0;

        public static bool CastingMalus(Mobile m, BaseWeapon weapon) =>
            _table != null && _table.TryGetValue(m, out var timer) && timer._castingMalus;

        public static bool RemoveEnchantment(Mobile caster, BaseWeapon weapon = null)
        {
            if (_table == null || !_table.Remove(caster, out var timer) || weapon != null && timer._weapon != weapon)
            {
                return false;
            }

            timer.Stop();

            caster.SendLocalizedMessage(1115273); // The enchantment on your weapon has expired.
            caster.PlaySound(0x1E6);

            Enhancement.RemoveMobile(caster);

            weapon?.InvalidateProperties();
            BuffInfo.RemoveBuff(caster, BuffIcon.Enchant);

            return true;
        }

        public static void OnWeaponRemoved(BaseWeapon wep, Mobile from)
        {
            if (IsUnderSpellEffects(from, wep))
            {
                RemoveEnchantment(from);
            }

            wep.EnchantedWeilder = null;
        }

        private class EnchantmentTimer : Timer
        {
            private readonly Mobile _owner;
            public readonly BaseWeapon _weapon;
            public readonly bool _castingMalus;
            public readonly AosWeaponAttribute _weaponAttribute;
            public readonly int _attributeValue;

            public EnchantmentTimer(Mobile owner, BaseWeapon wep, AosWeaponAttribute attribute, int value, bool malus, double duration)
                : base(TimeSpan.FromSeconds(duration))
            {
                _owner = owner;
                _weapon = wep;
                _weaponAttribute = attribute;
                _attributeValue = value;
                _castingMalus = malus;

                Start();
            }

            protected override void OnTick()
            {
                if (_weapon != null)
                {
                    _weapon.EnchantedWeilder = null;
                }

                RemoveEnchantment(_owner);
            }
        }

        public class EnchantSpellGump : Gump
        {
            private readonly Mobile _caster;
            private readonly EnchantSpell _spell;

            public EnchantSpellGump(Mobile caster, EnchantSpell spell) : base(20, 20)
            {
                _spell = spell;
                _caster = caster;

                AddBackground(0, 0, 260, 187, 3600);
                AddAlphaRegion(5, 15, 242, 170);

                AddImageTiled(220, 15, 30, 162, 10464);

                AddItem(0, 3, 6882);
                AddItem(-8, 170, 6880);
                AddItem(185, 3, 6883);
                AddItem(192, 170, 6881);

                AddHtmlLocalized(20, 22, 150, 16, 1080133, 0x07FF); // Select Enchant

                AddButton(20, 50, 9702, 9703, 1);
                AddHtmlLocalized(45, 50, 200, 16, 1079705, 0x07FF); // Hit Lighting

                AddButton(20, 75, 9702, 9703, 2);
                AddHtmlLocalized(45, 75, 200, 16, 1079703, 0x07FF); // Hit Fireball

                AddButton(20, 100, 9702, 9703, 3);
                AddHtmlLocalized(45, 100, 200, 16, 1079704, 0x07FF); // Hit Harm

                AddButton(20, 125, 9702, 9703, 4);
                AddHtmlLocalized(45, 125, 200, 16, 1079706, 0x07FF); // Hit Magic Arrow

                AddButton(20, 150, 9702, 9703, 5);
                AddHtmlLocalized(45, 150, 200, 16, 1079702, 0x07FF); // Hit Dispel
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                if (info.ButtonID is < 1 or > 5)
                {
                    _caster.SendLocalizedMessage(1080132); // You decide not to enchant your weapon.
                    return;
                }

                AosWeaponAttribute attr = info.ButtonID switch
                {
                    2 => AosWeaponAttribute.HitFireball,
                    3 => AosWeaponAttribute.HitHarm,
                    4 => AosWeaponAttribute.HitMagicArrow,
                    5 => AosWeaponAttribute.HitDispel,
                    _ => AosWeaponAttribute.HitLightning
                };

                _spell._weaponAttribute = attr;
                _spell.Cast();
            }
        }
    }
}
