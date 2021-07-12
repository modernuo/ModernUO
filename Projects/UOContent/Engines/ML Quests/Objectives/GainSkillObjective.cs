using System;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Objectives
{
    [Flags]
    public enum GainSkillObjectiveFlags : byte
    {
        None = 0x00,
        UseReal = 0x01,
        Accelerate = 0x02
    }

    public class GainSkillObjective : BaseObjective
    {
        private GainSkillObjectiveFlags m_Flags;

        public GainSkillObjective(
            SkillName skill = SkillName.Alchemy, int thresholdFixed = 0, bool useReal = false, bool accelerate = false
        )
        {
            Skill = skill;
            ThresholdFixed = thresholdFixed;
            m_Flags = GainSkillObjectiveFlags.None;

            if (useReal)
            {
                m_Flags |= GainSkillObjectiveFlags.UseReal;
            }

            if (accelerate)
            {
                m_Flags |= GainSkillObjectiveFlags.Accelerate;
            }
        }

        public SkillName Skill { get; set; }

        public int ThresholdFixed { get; set; }

        public bool UseReal
        {
            get => GetFlag(GainSkillObjectiveFlags.UseReal);
            set => SetFlag(GainSkillObjectiveFlags.UseReal, value);
        }

        public bool Accelerate
        {
            get => GetFlag(GainSkillObjectiveFlags.Accelerate);
            set => SetFlag(GainSkillObjectiveFlags.Accelerate, value);
        }

        public override bool CanOffer(IQuestGiver quester, PlayerMobile pm, bool message)
        {
            var skill = pm.Skills[Skill];

            if ((UseReal ? skill.Fixed : skill.BaseFixedPoint) >= ThresholdFixed)
            {
                if (message)
                {
                    MLQuestSystem.Tell(quester, pm, 1077772); // I cannot teach you, for you know all I can teach!
                }

                return false;
            }

            return true;
        }

        public override void WriteToGump(Gump g, ref int y)
        {
            var skillLabel = AosSkillBonuses.GetLabel(Skill);
            var args = $"#{skillLabel}\t{ThresholdFixed / 10.0:0.#}";

            g.AddHtmlLocalized(98, y, 312, 16, 1077485, args, 0x15F90); // Increase ~1_SKILL~ to ~2_VALUE~
            y += 16;
        }

        public override BaseObjectiveInstance CreateInstance(MLQuestInstance instance) =>
            new GainSkillObjectiveInstance(this, instance);

        private bool GetFlag(GainSkillObjectiveFlags flag) => (m_Flags & flag) != 0;

        private void SetFlag(GainSkillObjectiveFlags flag, bool value)
        {
            if (value)
            {
                m_Flags |= flag;
            }
            else
            {
                m_Flags &= ~flag;
            }
        }
    }

    // On OSI, once this is complete, it will *stay* complete, even if you lower your skill again
    public class GainSkillObjectiveInstance : BaseObjectiveInstance
    {
        public GainSkillObjectiveInstance(GainSkillObjective objective, MLQuestInstance instance)
            : base(instance, objective) =>
            Objective = objective;

        public GainSkillObjective Objective { get; set; }

        public bool Handles(SkillName skill) => Objective.Skill == skill;

        public override bool IsCompleted()
        {
            var pm = Instance.Player;

            var valueFixed = Objective.UseReal
                ? pm.Skills[Objective.Skill].Fixed
                : pm.Skills[Objective.Skill].BaseFixedPoint;

            return valueFixed >= Objective.ThresholdFixed;
        }

        // TODO: This may interfere with scrolls, or even quests among each other
        // How does OSI deal with this?
        public override void OnQuestAccepted()
        {
            if (!Objective.Accelerate)
            {
                return;
            }

            var pm = Instance.Player;

            pm.AcceleratedSkill = Objective.Skill;
            pm.AcceleratedStart = Core.Now + TimeSpan.FromMinutes(15); // TODO: Is there a max duration?
        }

        public override void OnQuestCancelled()
        {
            if (!Objective.Accelerate)
            {
                return;
            }

            var pm = Instance.Player;

            pm.AcceleratedStart = Core.Now;
            pm.PlaySound(0x100);
        }

        public override void OnQuestCompleted()
        {
            OnQuestCancelled();
        }

        public override void WriteToGump(Gump g, ref int y)
        {
            Objective.WriteToGump(g, ref y);

            base.WriteToGump(g, ref y);

            if (IsCompleted())
            {
                g.AddHtmlLocalized(113, y, 312, 20, 1055121, 0xFFFFFF); // Complete
                y += 16;
            }
        }
    }
}
