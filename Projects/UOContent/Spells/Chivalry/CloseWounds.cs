using System;
using Server.Engines.ConPVP;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Spells.Chivalry
{
    public class CloseWoundsSpell : PaladinSpell, ISpellTargetingMobile
    {
        private static readonly SpellInfo _info = new(
            "Close Wounds",
            "Obsu Vulni",
            -1,
            9002
        );

        public CloseWoundsSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.5);

        public override double RequiredSkill => 0.0;
        public override int RequiredMana => 10;
        public override int RequiredTithing => 10;
        public override int MantraNumber => 1060719; // Obsu Vulni

        public void Target(Mobile m)
        {
            if (m == null)
            {
                return;
            }

            if (!Caster.InRange(m, 2))
            {
                Caster.SendLocalizedMessage(1060178); // You are too far away to perform that action!
            }
            else if (m is BaseCreature { IsAnimatedDead: true })
            {
                Caster.SendLocalizedMessage(1061654); // You cannot heal that which is not alive.
            }
            else if (m.IsDeadBondedPet)
            {
                Caster.SendLocalizedMessage(1060177); // You cannot heal a creature that is already dead!
            }
            else if (m.Hits >= m.HitsMax)
            {
                Caster.SendLocalizedMessage(500955); // That being is not damaged!
            }
            else if (m.Poisoned || MortalStrike.IsWounded(m))
            {
                Caster.LocalOverheadMessage(MessageType.Regular, 0x3B2, Caster == m ? 1005000 : 1010398);
            }
            else if (CheckBSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                /* Heals the target for 7 to 39 points of damage.
                 * The caster's Karma affects the amount of damage healed.
                 */

                // TODO: Should caps be applied?
                var toHeal = Math.Clamp(ComputePowerValue(6) + Utility.RandomMinMax(0, 2), 7, 39);

                if (m.Hits + toHeal > m.HitsMax)
                {
                    toHeal = m.HitsMax - m.Hits;
                }

                SpellHelper.Heal(toHeal, m, Caster, false);

                // You have had ~1_HEALED_AMOUNT~ hit points of damage healed.
                m.SendLocalizedMessage(1060203, toHeal.ToString());

                m.PlaySound(0x202);
                m.FixedParticles(0x376A, 1, 62, 9923, 3, 3, EffectLayer.Waist);
                m.FixedParticles(0x3779, 1, 46, 9502, 5, 3, EffectLayer.Waist);
            }

            FinishSequence();
        }

        public override bool CheckCast()
        {
            if (DuelContext.CheckSuddenDeath(Caster))
            {
                Caster.SendMessage(0x22, "You cannot cast this spell when in sudden death.");
                return false;
            }

            return base.CheckCast();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetMobile(this, TargetFlags.Beneficial);
        }
    }
}
