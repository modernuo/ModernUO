using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class PsychicAttack : WeaponAbility
    {
        public static Dictionary<Mobile, PsychicAttackTimer> Registry { get; } = new();
        public override int BaseMana => 30;

        public override void OnHit(Mobile attacker, Mobile defender, int damage, WorldLocation worldLocation)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
            {
                return;
            }

            ClearCurrentAbility(attacker);

            attacker.SendLocalizedMessage(1074383); // Your shot sends forth a wave of psychic energy.
            defender.SendLocalizedMessage(1074384); // Your mind is attacked by psychic force!

            defender.FixedParticles(0x3789, 10, 25, 5032, EffectLayer.Head);
            defender.PlaySound(0x1F8);

            if (Registry.TryGetValue(defender, out var timer))
            {
                if (!timer.DoneIncrease)
                {
                    timer.SpellDamageMalus *= 2;
                    timer.ManaCostMalus *= 2;
                }
            }
            else
            {
                timer = new PsychicAttackTimer(defender);
                timer.Start();
                Registry.Add(defender, timer);
            }

            BuffInfo.RemoveBuff(defender, BuffIcon.PsychicAttack);

            string args = $"{timer.SpellDamageMalus}\t{timer.ManaCostMalus}";
            BuffInfo.AddBuff(defender, new BuffInfo(BuffIcon.PsychicAttack, 1151296, 1151297, args));
        }

        public static void RemoveEffects(Mobile defender)
        {
            if (defender == null)
            {
                return;
            }

            BuffInfo.RemoveBuff(defender, BuffIcon.PsychicAttack);

            Registry.Remove(defender);

            defender.SendLocalizedMessage(1150292); // You recover from the effects of the psychic attack.
        }

        public class PsychicAttackTimer : Timer
        {
            private readonly Mobile _defender;
            private int _spellDamageMalus;
            private int _manaCostMalus;

            public int SpellDamageMalus
            {
                get => _spellDamageMalus;
                set { _spellDamageMalus = value; DoneIncrease = true; }
            }
            public int ManaCostMalus
            {
                get => _manaCostMalus;
                set { _manaCostMalus = value; DoneIncrease = true; }
            }
            public bool DoneIncrease { get; private set; }

            public PsychicAttackTimer(Mobile defender) : base(TimeSpan.FromSeconds(10))
            {
                _defender = defender;
                _spellDamageMalus = 15;
                _manaCostMalus = 15;
                DoneIncrease = false;
            }

            protected override void OnTick()
            {
                RemoveEffects(_defender);
            }
        }
    }
}
