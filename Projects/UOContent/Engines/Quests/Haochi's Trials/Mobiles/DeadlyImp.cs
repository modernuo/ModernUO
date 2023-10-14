using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.Quests.Samurai;

[SerializationGenerator(0)]
public partial class DeadlyImp : BaseCreature
{
    [Constructible]
    public DeadlyImp() : base(AIType.AI_Melee, FightMode.Aggressor)
    {
        Body = 74;
        BaseSoundID = 422;
        Hue = 0x66A;

        SetStr(91, 115);
        SetDex(61, 80);
        SetInt(86, 105);

        SetHits(1000);

        SetDamage(50, 80);

        SetDamageType(ResistanceType.Fire, 100);

        SetResistance(ResistanceType.Physical, 95, 98);
        SetResistance(ResistanceType.Fire, 95, 98);
        SetResistance(ResistanceType.Cold, 95, 98);
        SetResistance(ResistanceType.Poison, 95, 98);
        SetResistance(ResistanceType.Energy, 95, 98);

        SetSkill(SkillName.Magery, 120.0);
        SetSkill(SkillName.Tactics, 120.0);
        SetSkill(SkillName.Wrestling, 120.0);

        Fame = 2500;
        Karma = -2500;

        CantWalk = true;
    }

    public override string DefaultName => "a deadly imp";

    public override void AggressiveAction(Mobile aggressor, bool criminal)
    {
        base.AggressiveAction(aggressor, criminal);

        if (aggressor is PlayerMobile player)
        {
            var qs = player.Quest;
            if (qs is HaochisTrialsQuest)
            {
                QuestObjective obj = qs.FindObjective<SecondTrialAttackObjective>();
                if (obj?.Completed == false)
                {
                    obj.Complete();
                    qs.AddObjective(new SecondTrialReturnObjective(false));
                }
            }
        }
    }
}
