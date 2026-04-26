using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Matriarch
{
    public class KillInfiltratorsObjective : QuestObjective
    {
        public override int Message => ((SolenMatriarchQuest)System).RedSolen ? 1054086 : 1054085;

        public override int MaxProgress => 7;

        public override void RenderProgress(ref DynamicGumpBuilder builder)
        {
            if (!Completed)
            {
                // Black/Red Solen Infiltrators killed:
                builder.AddHtmlLocalized(
                    70,
                    260,
                    270,
                    100,
                    ((SolenMatriarchQuest)System).RedSolen ? 1054088 : 1054087,
                    BaseQuestGump.Blue
                );
                builder.AddLabel(70, 280, 0x64, $"{CurProgress}");
                builder.AddLabel(100, 280, 0x64, "/");
                builder.AddLabel(130, 280, 0x64, $"{MaxProgress}");
            }
            else
            {
                base.RenderProgress(ref builder);
            }
        }

        public override bool IgnoreYoungProtection(Mobile from)
        {
            if (Completed)
            {
                return false;
            }

            var redSolen = ((SolenMatriarchQuest)System).RedSolen;

            if (redSolen)
            {
                return from is BlackSolenInfiltratorWarrior or BlackSolenInfiltratorQueen;
            }

            return from is RedSolenInfiltratorWarrior or RedSolenInfiltratorQueen;
        }

        public override void OnKill(BaseCreature creature, Container corpse)
        {
            var redSolen = ((SolenMatriarchQuest)System).RedSolen;

            if (redSolen)
            {
                if (creature is BlackSolenInfiltratorWarrior or BlackSolenInfiltratorQueen)
                {
                    CurProgress++;
                }
            }
            else
            {
                if (creature is RedSolenInfiltratorWarrior or RedSolenInfiltratorQueen)
                {
                    CurProgress++;
                }
            }
        }

        public override void OnComplete()
        {
            System.AddObjective(new ReturnAfterKillsObjective());
        }
    }

    public class ReturnAfterKillsObjective : QuestObjective
    {
        public override int Message => 1054090;

        public override void OnComplete()
        {
            System.AddConversation(new GatherWaterConversation());
        }
    }

    public class GatherWaterObjective : QuestObjective
    {
        public override int Message => 1054092;

        public override int MaxProgress => 40;

        public override void RenderProgress(ref DynamicGumpBuilder builder)
        {
            if (!Completed)
            {
                builder.AddHtmlLocalized(70, 260, 270, 100, 1054093, BaseQuestGump.Blue); // Gallons of Water gathered:
                builder.AddLabel(70, 280, 0x64, (CurProgress / 5).ToString());
                builder.AddLabel(100, 280, 0x64, "/");
                builder.AddLabel(130, 280, 0x64, (MaxProgress / 5).ToString());
            }
            else
            {
                base.RenderProgress(ref builder);
            }
        }

        public override void OnComplete()
        {
            System.AddObjective(new ReturnAfterWaterObjective());
        }
    }

    public class ReturnAfterWaterObjective : QuestObjective
    {
        public override int Message => 1054095;

        public override void OnComplete()
        {
            var player = System.From;
            var redSolen = ((SolenMatriarchQuest)System).RedSolen;

            var friend = SolenMatriarchQuest.IsFriend(player, redSolen);

            System.AddConversation(new ProcessFungiConversation(friend));

            if (redSolen)
            {
                player.SolenFriendship = SolenFriendship.Red;
            }
            else
            {
                player.SolenFriendship = SolenFriendship.Black;
            }
        }
    }

    public class ProcessFungiObjective : QuestObjective
    {
        public override int Message => 1054098;

        public override void OnComplete()
        {
            if (SolenMatriarchQuest.GiveRewardTo(System.From))
            {
                System.Complete();
            }
            else
            {
                System.AddConversation(new FullBackpackConversation(true));
            }
        }
    }

    public class GetRewardObjective : QuestObjective
    {
        public override int Message => 1054149;

        public override void OnComplete()
        {
            System.AddConversation(new EndConversation());
        }
    }
}
