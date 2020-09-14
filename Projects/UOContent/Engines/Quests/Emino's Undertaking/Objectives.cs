using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Ninja
{
    public class FindEminoBeginObjective : QuestObjective
    {
        public override object Message => 1063174;

        public override void OnComplete()
        {
            System.AddConversation(new FindZoelConversation());
        }
    }

    public class FindZoelObjective : QuestObjective
    {
        public override object Message => 1063176;

        public override void OnComplete()
        {
            System.AddConversation(new EnterCaveConversation());
        }
    }

    public class EnterCaveObjective : QuestObjective
    {
        public override object Message => 1063179;

        public override void CheckProgress()
        {
            if (System.From.Map == Map.Malas && System.From.InRange(new Point3D(406, 1141, 0), 2))
            {
                Complete();
            }
        }

        public override void OnComplete()
        {
            System.AddConversation(new SneakPastGuardiansConversation());
        }
    }

    public class SneakPastGuardiansObjective : QuestObjective
    {
        public bool TaughtHowToUseSkills { get; set; }

        public override object Message => 1063261;

        public override void CheckProgress()
        {
            if (System.From.Map == Map.Malas && System.From.InRange(new Point3D(412, 1123, 0), 3))
            {
                Complete();
            }
        }

        public override void OnComplete()
        {
            System.AddConversation(new UseTeleporterConversation());
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            TaughtHowToUseSkills = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(TaughtHowToUseSkills);
        }
    }

    public class UseTeleporterObjective : QuestObjective
    {
        public override object Message => 1063183;

        public override void OnComplete()
        {
            System.AddConversation(new GiveZoelNoteConversation());
        }
    }

    public class GiveZoelNoteObjective : QuestObjective
    {
        public override object Message => 1063185;

        public override void OnComplete()
        {
            System.AddConversation(new GainInnInformationConversation());
        }
    }

    public class GainInnInformationObjective : QuestObjective
    {
        public override object Message => 1063190;

        public override void CheckProgress()
        {
            Mobile from = System.From;

            if (from.Map == Map.Malas && from.X > 399 && from.X < 408 && from.Y > 1091 && from.Y < 1099)
            {
                Complete();
            }
        }

        public override void OnComplete()
        {
            System.AddConversation(new ReturnFromInnConversation());
        }
    }

    public class ReturnFromInnObjective : QuestObjective
    {
        public override object Message => 1063197;

        public override void OnComplete()
        {
            System.AddConversation(new SearchForSwordConversation());
        }
    }

    public class SearchForSwordObjective : QuestObjective
    {
        public override object Message => 1063200;

        public override void OnComplete()
        {
            System.AddConversation(new HallwayWalkConversation());
        }
    }

    public class HallwayWalkObjective : QuestObjective
    {
        public bool StolenTreasure { get; set; }

        public override object Message => 1063202;

        public override void OnComplete()
        {
            System.AddConversation(new ReturnSwordConversation());
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            StolenTreasure = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(StolenTreasure);
        }
    }

    public class ReturnSwordObjective : QuestObjective
    {
        public override object Message => 1063204;

        public override void CheckProgress()
        {
            Mobile from = System.From;

            if (from.Map != Map.Malas || from.Y > 992)
            {
                Complete();
            }
        }

        public override void OnComplete()
        {
            System.AddConversation(new SlayHenchmenConversation());
        }
    }

    public class SlayHenchmenObjective : QuestObjective
    {
        public override object Message => 1063206;

        public override int MaxProgress => 3;

        public override void RenderProgress(BaseQuestGump gump)
        {
            if (!Completed)
            {
                // Henchmen killed:
                gump.AddHtmlLocalized(70, 260, 270, 100, 1063207, BaseQuestGump.Blue);
                gump.AddLabel(70, 280, 0x64, CurProgress.ToString());
                gump.AddLabel(100, 280, 0x64, "/");
                gump.AddLabel(130, 280, 0x64, MaxProgress.ToString());
            }
            else
            {
                base.RenderProgress(gump);
            }
        }

        public override void OnKill(BaseCreature creature, Container corpse)
        {
            if (creature is Henchman)
            {
                CurProgress++;
            }
        }

        public override void OnComplete()
        {
            System.AddConversation(new GiveEminoSwordConversation());
        }
    }

    public class GiveEminoSwordObjective : QuestObjective
    {
        public override object Message => 1063210;

        public override void OnComplete()
        {
        }
    }
}
