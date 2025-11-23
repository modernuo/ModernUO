using System;
using ModernUO.Serialization;
using Server.Engines.MLQuests;
using Server.Engines.MLQuests.Objectives;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ScrollofTranscendence : SpecialScroll
{
    [Constructible]
    public ScrollofTranscendence(SkillName skill = SkillName.Alchemy, double value = 0.0) : base(skill, value)
    {
        ItemID = 0x14EF;
        Hue = 0x490;
    }

    public override int LabelNumber => 1094934; // Scroll of Transcendence

    /* Using a Scroll of Transcendence for a given skill will permanently increase your current
     * level in that skill by the amount of points displayed on the scroll.
     * As you may not gain skills beyond your maximum skill cap, any excess points will be lost.
     */
    public override int Message => 1094933;

    public override string DefaultTitle =>
       Html.Color( $"Scroll of Transcendence ({Math.Floor(Value * 10) / 10:0.#} Skill):", 0xFFFFFF);

    public static ScrollofTranscendence CreateRandom(int min, int max) =>
        new(SkillsInfo.RandomSkill(), Utility.RandomMinMax(min, max) / 10.0);

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add(1151930, $"{SkillLabel:#}\t{Math.Floor(Value * 10) / 10:0.#}\t{1151931:#}");
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
        if (!CanUse(from))
        {
            return;
        }

        var skill = from.Skills[Skill];
        var skillBase = skill.Base;
        var skillCap = skill.Cap;
        var canGain = false;

        var newValue = Value;

        if (skillBase + newValue > skillCap)
        {
            newValue = skillCap - skillBase;
        }

        if (skillBase < skillCap && skill.Lock == SkillLock.Up)
        {
            if (from.SkillsTotal + newValue * 10 > from.SkillsCap)
            {
                var ns = from.Skills.Length; // number of items in from.Skills[]

                for (var i = 0; i < ns; i++)
                {
                    var sk = from.Skills[i];
                    // skill must point down and its value must be enough
                    if (sk.Lock == SkillLock.Down && sk.Base >= newValue)
                    {
                        sk.Base -= newValue;
                        canGain = true;
                        break;
                    }
                }
            }
            else
            {
                canGain = true;
            }
        }

        if (!canGain)
        {
            /* You cannot increase this skill at this time. The skill may be locked or set to lower in your skill menu.
             * If you are at your total skill cap, you must use a Powerscroll to increase your current skill cap.
             */
            from.SendLocalizedMessage(1094935);
            return;
        }

        // You feel a surge of magic as the scroll enhances your ~1_type~!
        from.SendLocalizedMessage(1049513, $"#{AosSkillBonuses.GetLowercaseLabel(Skill)}");

        from.Skills[Skill].Base += newValue;

        Effects.PlaySound(from.Location, from.Map, 0x1F7);
        Effects.SendTargetParticles(from, 0x373A, 35, 45, 0x00, 0x00, 9502, (EffectLayer)255, 0x100);
        Effects.SendTargetParticles(from, 0x376A, 35, 45, 0x00, 0x00, 9502, (EffectLayer)255, 0x100);

        Delete();
    }
}
