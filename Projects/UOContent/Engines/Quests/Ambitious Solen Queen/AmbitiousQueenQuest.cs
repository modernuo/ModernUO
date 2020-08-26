using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Ambitious
{
    public class AmbitiousQueenQuest : QuestSystem
    {
        private static readonly Type[] m_TypeReferenceTable =
        {
            typeof(DontOfferConversation),
            typeof(AcceptConversation),
            typeof(DuringKillQueensConversation),
            typeof(GatherFungiConversation),
            typeof(DuringFungiGatheringConversation),
            typeof(EndConversation),
            typeof(FullBackpackConversation),
            typeof(End2Conversation),
            typeof(KillQueensObjective),
            typeof(ReturnAfterKillsObjective),
            typeof(GatherFungiObjective),
            typeof(GetRewardObjective)
        };

        public AmbitiousQueenQuest(PlayerMobile from, bool redSolen) : base(from) => RedSolen = redSolen;

        // Serialization
        public AmbitiousQueenQuest()
        {
        }

        public override Type[] TypeReferenceTable => m_TypeReferenceTable;

        public override object Name => 1054146;

        public override object OfferMessage => 1054060;

        public override TimeSpan RestartDelay => TimeSpan.Zero;
        public override bool IsTutorial => false;

        public override int Picture => 0x15C9;

        public bool RedSolen { get; private set; }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            RedSolen = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(RedSolen);
        }

        public override void Accept()
        {
            base.Accept();

            AddConversation(new AcceptConversation());
        }

        public static void GiveRewardTo(
            PlayerMobile player, ref bool bagOfSending, ref bool powderOfTranslocation,
            ref bool gold
        )
        {
            if (bagOfSending)
            {
                Item reward = new BagOfSending();

                if (player.PlaceInBackpack(reward))
                {
                    player.SendLocalizedMessage(1054074, "", 0x59); // You have been given a bag of sending.
                    bagOfSending = false;
                }
                else
                {
                    reward.Delete();
                }
            }

            if (powderOfTranslocation)
            {
                Item reward = new PowderOfTranslocation(Utility.RandomMinMax(10, 12));

                if (player.PlaceInBackpack(reward))
                {
                    player.SendLocalizedMessage(1054075, "", 0x59); // You have been given some powder of translocation.
                    powderOfTranslocation = false;
                }
                else
                {
                    reward.Delete();
                }
            }

            if (gold)
            {
                Item reward = new Gold(Utility.RandomMinMax(250, 350));

                if (player.PlaceInBackpack(reward))
                {
                    player.SendLocalizedMessage(1054076, "", 0x59); // You have been given some gold.
                    gold = false;
                }
                else
                {
                    reward.Delete();
                }
            }
        }
    }
}
