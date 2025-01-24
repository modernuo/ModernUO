using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Engines.BuffIcons;
using Server.Gumps;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Spellweaving
{
    public class GiftOfLifeSpell : ArcanistSpell, ITargetingSpell<Mobile>
    {
        private static readonly SpellInfo _info = new(
            "Gift of Life",
            "Illorae",
            -1
        );

        private static readonly Dictionary<Mobile, ExpireTimer> _table = new();

        public GiftOfLifeSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(4.0);

        public override double RequiredSkill => 38.0;
        public override int RequiredMana => 70;

        public double HitsScalar => (Caster.Skills.Spellweaving.Value / 2.4 + FocusLevel) / 100;

        public void Target(Mobile m)
        {
            if (m.IsDeadBondedPet || !m.Alive)
            {
                // As per Osi: Nothing happens.
            }
            else if (m != Caster && !(m is BaseCreature { IsBonded: true } bc && bc.ControlMaster == Caster))
            {
                Caster.SendLocalizedMessage(1072077); // You may only cast this spell on yourself or a bonded pet.
            }
            else if (_table.ContainsKey(m))
            {
                Caster.SendLocalizedMessage(501775); // This spell is already in effect.
            }
            else if (CheckBSequence(m))
            {
                if (Caster == m)
                {
                    Caster.SendLocalizedMessage(1074774); // You weave powerful magic, protecting yourself from death.
                }
                else
                {
                    Caster.SendLocalizedMessage(1074775); // You weave powerful magic, protecting your pet from death.
                    SpellHelper.Turn(Caster, m);
                }

                m.PlaySound(0x244);
                m.FixedParticles(0x3709, 1, 30, 0x26ED, 5, 2, EffectLayer.Waist);
                m.FixedParticles(0x376A, 1, 30, 0x251E, 5, 3, EffectLayer.Waist);

                var skill = Caster.Skills.Spellweaving.Value;

                var duration = TimeSpan.FromMinutes((int)(skill / 24) * 2 + FocusLevel);

                var t = new ExpireTimer(m, duration, this);
                t.Start();

                _table[m] = t;

                (m as PlayerMobile)?.AddBuff(
                    new BuffInfo(BuffIcon.GiftOfLife, 1031615, 1075807, duration, retainThroughDeath: true)
                );
            }
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Beneficial);
        }

        [OnEvent(nameof(BaseCreature.CreatureDeathEvent))]
        [OnEvent(nameof(PlayerMobile.PlayerDeathEvent))]
        public static void OnDeathEvent(Mobile m)
        {
            if (_table.ContainsKey(m))
            {
                Timer.StartTimer(TimeSpan.FromSeconds(Utility.RandomMinMax(2, 4)), () => HandleDeath_OnCallback(m));
            }
        }

        private static void HandleDeath_OnCallback(Mobile m)
        {
            if (!_table.TryGetValue(m, out var timer))
            {
                return;
            }

            var hitsScalar = timer.Spell.HitsScalar;

            if (m is BaseCreature pet && pet.IsDeadBondedPet)
            {
                var master = pet.GetMaster();

                if (master?.NetState != null && Utility.InUpdateRange(pet.Location, master.Location))
                {
                    master.SendGump(new PetResurrectGump(master, pet, hitsScalar));
                }
                else
                {
                    var friends = pet.Friends;

                    for (var i = 0; i < friends?.Count; i++)
                    {
                        var friend = friends[i];

                        if (friend.NetState != null && Utility.InUpdateRange(pet.Location, friend.Location))
                        {
                            friend.SendGump(new PetResurrectGump(friend, pet));
                            break;
                        }
                    }
                }
            }
            else
            {
                m.SendGump(new ResurrectGump(m, hitsScalar));
            }

            // Per OSI, buff is removed when gump sent, irregardless of online status or acceptance
            timer.DoExpire();
        }

        private class ExpireTimer : Timer
        {
            private Mobile _mobile;

            public ExpireTimer(Mobile m, TimeSpan delay, GiftOfLifeSpell spell) : base(delay)
            {
                _mobile = m;
                Spell = spell;
            }

            public GiftOfLifeSpell Spell { get; }

            protected override void OnTick()
            {
                DoExpire();
            }

            public void DoExpire()
            {
                Stop();

                _mobile.SendLocalizedMessage(1074776); // You are no longer protected with Gift of Life.
                _table.Remove(_mobile);

                (_mobile as PlayerMobile)?.RemoveBuff(BuffIcon.GiftOfLife);
            }
        }
    }
}
