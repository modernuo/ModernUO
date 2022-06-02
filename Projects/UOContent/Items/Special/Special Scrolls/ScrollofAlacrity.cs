using System;
using Server.Engines.MLQuests;
using Server.Engines.MLQuests.Objectives;
using Server.Mobiles;

namespace Server.Items
{
    public class ScrollofAlacrity : SpecialScroll
    {
        [Constructible]
        public ScrollofAlacrity(SkillName skill = SkillName.Alchemy) : base(skill, 0.0)
        {
            ItemID = 0x14EF;
            Hue = 0x4AB;
        }

        public ScrollofAlacrity(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1078604; // Scroll of Alacrity

        /* Using a Scroll of Transcendence for a given skill will permanently increase your current
         * level in that skill by the amount of points displayed on the scroll.
         * As you may not gain skills beyond your maximum skill cap, any excess points will be lost.
         */
        public override int Message =>
            1078602;

        public override string DefaultTitle => "<basefont color=#FFFFFF>Scroll of Alacrity:</basefont>";

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1071345, $"{GetName()} 15 Minutes"); // Skill: ~1_val~
        }

        public override bool CanUse(Mobile from)
        {
            if (!(base.CanUse(from) && from is PlayerMobile pm))
            {
                return false;
            }

            var context = MLQuestSystem.GetContext(pm);

            if (context != null)
            {
                foreach (var instance in context.QuestInstances)
                {
                    foreach (var objective in instance.Objectives)
                    {
                        if (!objective.Expired && objective is GainSkillObjectiveInstance objectiveInstance &&
                            objectiveInstance.Handles(Skill))
                        {
                            from.SendMessage("You are already under the effect of an enhanced skillgain quest.");
                            return false;
                        }
                    }
                }
            }

            if (pm.AcceleratedStart > Core.Now)
            {
                from.SendLocalizedMessage(1077951); // You are already under the effect of an accelerated skillgain scroll.
                return false;
            }

            return true;
        }

        public override void Use(Mobile from)
        {
            if (!(CanUse(from) && from is PlayerMobile pm))
            {
                return;
            }

            var tskill = from.Skills[Skill].Base;
            var tcap = from.Skills[Skill].Cap;

            if (tskill >= tcap || from.Skills[Skill].Lock != SkillLock.Up)
            {
                /* You cannot increase this skill at this time. The skill may be locked or set to lower in your skill menu.
                 * If you are at your total skill cap, you must use a Powerscroll to increase your current skill cap.
                 */
                from.SendLocalizedMessage(1094935);
                return;
            }

            from.SendLocalizedMessage(
                1077956
            ); // You are infused with intense energy. You are under the effects of an accelerated skillgain scroll.

            Effects.PlaySound(from.Location, from.Map, 0x1E9);
            Effects.SendTargetParticles(from, 0x373A, 35, 45, 0x00, 0x00, 9502, (EffectLayer)255, 0x100);

            pm.AcceleratedStart = Core.Now + TimeSpan.FromMinutes(15);
            Timer.StartTimer(TimeSpan.FromMinutes(15), () => Expire_Callback(from));

            pm.AcceleratedSkill = Skill;

            Delete();
        }

        // TODO: Handle this upon deserialization. Create Dictionary and serialize Mobile/Timers?
        private static void Expire_Callback(Mobile m)
        {
            m.PlaySound(0x1F8);
            m.SendLocalizedMessage(
                1077957
            ); // The intense energy dissipates. You are no longer under the effects of an accelerated skillgain scroll.
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = InheritsItem ? 0 : reader.ReadInt(); // Required for SpecialScroll insertion

            LootType = LootType.Cursed;
            Insured = false;
        }
    }
}
