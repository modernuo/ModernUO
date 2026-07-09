using System;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Spellweaving
{
    public class DryadAllureSpell : ArcanistSpell
    {
        private const int ControlSlotCost = 3;

        private static readonly SpellInfo _info = new(
            "Dryad Allure",
            "Rathril",
            -1
        );

        public DryadAllureSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(3.0);
        public override double RequiredSkill => 52.0;
        public override int RequiredMana => 40;

        public static bool IsValidTarget(BaseCreature creature)
        {
            if (creature == null || creature.IsParagon || creature.Summoned || creature.AllureImmune ||
                creature.Controlled && !creature.Allured)
            {
                return false;
            }

            return SlayerGroup.GetEntryByName(SlayerName.Repond)?.Slays(creature) == true;
        }

        internal static double GetCharmChance(double spellweaving, int focusLevel) =>
            spellweaving / 150.0 + focusLevel / 50.0;

        internal static bool ApplyAllureAttempt(Mobile caster, BaseCreature creature, int focusLevel)
        {
            if (GetCharmChance(caster.Skills.Spellweaving.Value, focusLevel) <= Utility.RandomDouble())
            {
                EnrageTarget(caster, creature);
                return false;
            }

            var oldControlSlots = creature.ControlSlots;

            creature.ControlSlots = ControlSlotCost;
            creature.Combatant = null;
            creature.Warmode = false;
            creature.RemoveAggressor(caster);
            creature.RemoveAggressed(caster);
            caster.RemoveAggressor(creature);
            caster.RemoveAggressed(creature);

            if (caster.Combatant == creature)
            {
                caster.Combatant = null;
                caster.Warmode = false;
            }

            if (!creature.SetControlMaster(caster))
            {
                creature.ControlSlots = oldControlSlots;
                return false;
            }

            creature.PlaySound(0x5C4);
            creature.Allured = true;
            creature.Loyalty = BaseCreature.MaxLoyalty;
            DeleteBackpackContents(creature);

            caster.SendLocalizedMessage(1074377); // You allure the humanoid to follow and protect you.
            return true;
        }

        private static void EnrageTarget(Mobile caster, BaseCreature creature)
        {
            creature.PlaySound(0x5C5);
            creature.ControlTarget = caster;
            creature.ControlOrder = OrderType.Attack;
            creature.Combatant = caster;
            creature.Warmode = true;

            caster.SendLocalizedMessage(1074378); // The humanoid becomes enraged by your charming attempt and attacks you.
        }

        private static void DeleteBackpackContents(BaseCreature creature)
        {
            var pack = creature.Backpack;

            if (pack == null)
            {
                return;
            }

            for (var i = pack.Items.Count - 1; i >= 0; --i)
            {
                if (i >= pack.Items.Count)
                {
                    continue;
                }

                pack.Items[i].Delete();
            }
        }

        public void Target(BaseCreature creature)
        {
            if (creature == null || creature.Deleted)
            {
                Caster.SendLocalizedMessage(1074379); // You cannot charm that!
                return;
            }

            if (!Caster.CanSee(creature.Location) || !Caster.InLOS(creature))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
                return;
            }

            if (!IsValidTarget(creature))
            {
                Caster.SendLocalizedMessage(1074379); // You cannot charm that!
                return;
            }

            if (creature.Allured)
            {
                Caster.SendLocalizedMessage(1074380); // This humanoid is already controlled by someone else.
                return;
            }

            if (Caster.Followers + ControlSlotCost > Caster.FollowersMax)
            {
                Caster.SendLocalizedMessage(1049607); // You have too many followers to control that creature.
                return;
            }

            if (CheckSequence())
            {
                SpellHelper.Turn(Caster, creature);
                ApplyAllureAttempt(Caster, creature, FocusLevel);
            }
        }

        public override void OnCast()
        {
            Caster.Target = new InternalTarget(this);
        }

        private class InternalTarget : Target
        {
            private readonly DryadAllureSpell _spell;

            public InternalTarget(DryadAllureSpell spell) : base(12, false, TargetFlags.None) =>
                _spell = spell;

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is BaseCreature creature)
                {
                    _spell.Target(creature);
                }
                else
                {
                    from.SendLocalizedMessage(1074379); // You cannot charm that!
                }
            }

            protected override void OnTargetFinish(Mobile from)
            {
                _spell.FinishSequence();
            }
        }
    }
}
