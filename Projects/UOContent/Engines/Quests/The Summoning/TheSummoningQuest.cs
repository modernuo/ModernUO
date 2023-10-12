using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Doom
{
    public class TheSummoningQuest : QuestSystem
    {
        private static readonly Type[] m_TypeReferenceTable =
        {
            typeof(AcceptConversation),
            typeof(CollectBonesObjective),
            typeof(VanquishDaemonConversation),
            typeof(VanquishDaemonObjective)
        };

        public TheSummoningQuest(Victoria victoria, PlayerMobile from) : base(from) => Victoria = victoria;

        public TheSummoningQuest()
        {
        }

        public override Type[] TypeReferenceTable => m_TypeReferenceTable;

        public Victoria Victoria { get; private set; }

        public bool WaitForSummon { get; set; }

        public override object Name => 1050025;

        public override object OfferMessage => 1050020;

        public override bool IsTutorial => false;
        public override TimeSpan RestartDelay => TimeSpan.Zero;
        public override int Picture => 0x15B5;

        // NOTE: Quest not entirely OSI-accurate: some changes made to prevent numerous OSI bugs

        public override void Slice()
        {
            if (WaitForSummon && Victoria != null)
            {
                var altar = Victoria.Altar;

                if (altar != null && altar.Daemon?.Alive != true)
                {
                    if (From.Map == Victoria.Map && From.InRange(Victoria, 8))
                    {
                        WaitForSummon = false;

                        AddConversation(new VanquishDaemonConversation());
                    }
                }
            }

            base.Slice();
        }

        public static int GetDaemonBonesFor(BaseCreature creature)
        {
            if (creature?.Controlled != false || creature.Summoned)
            {
                return 0;
            }

            var fame = creature.Fame;

            if (fame < 1500)
            {
                return Utility.Dice(2, 5, -1);
            }

            if (fame < 20000)
            {
                return Utility.Dice(2, 4, 8);
            }

            return 50;
        }

        public override void Cancel()
        {
            base.Cancel();

            QuestObjective obj = FindObjective<CollectBonesObjective>();

            if (obj?.CurProgress > 0)
            {
                From.BankBox.DropItem(new DaemonBone(obj.CurProgress));

                // The Daemon bones that you have thus far given to Victoria have been returned to you.
                From.SendLocalizedMessage(1050030);
            }
        }

        public override void Accept()
        {
            base.Accept();

            AddConversation(new AcceptConversation());
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            Victoria = reader.ReadEntity<Victoria>();
            WaitForSummon = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(Victoria);
            writer.Write(WaitForSummon);
        }
    }
}
