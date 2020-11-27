namespace Server.Engines.Quests.Necro
{
    public class AcceptConversation : QuestConversation
    {
        public override object Message => 1049092;

        public override void OnRead()
        {
            var bag = BaseQuester.GetNewContainer();

            bag.DropItem(new DarkTidesHorn());

            System.From.AddToBackpack(bag);

            System.AddConversation(new ReanimateMaabusConversation());
        }
    }

    public class ReanimateMaabusConversation : QuestConversation
    {
        private static readonly QuestItemInfo[] m_Info =
        {
            new(1026153, 6178), // teleporter
            new(1049117, 4036), // Horn of Retreat
            new(1048032, 3702)  // a bag
        };

        public override object Message => 1060099;

        public override QuestItemInfo[] Info => m_Info;

        public override void OnRead()
        {
            System.AddObjective(new FindMaabusTombObjective());
        }
    }

    public class MaabasConversation : QuestConversation
    {
        private static readonly QuestItemInfo[] m_Info =
        {
            new(1026153, 6178) // teleporter
        };

        public override object Message => 1060103;

        public override QuestItemInfo[] Info => m_Info;

        public override void OnRead()
        {
            System.AddObjective(new FindCrystalCaveObjective());
        }
    }

    public class HorusConversation : QuestConversation
    {
        public override object Message => 1060105;

        public override void OnRead()
        {
            System.AddObjective(new FindMardothAboutVaultObjective());
        }
    }

    public class MardothVaultConversation : QuestConversation
    {
        public override object Message => 1060107;

        public override void OnRead()
        {
            System.AddObjective(new FindCityOfLightObjective());
        }
    }

    public class VaultOfSecretsConversation : QuestConversation
    {
        private static readonly QuestItemInfo[] m_Info =
        {
            new(1023643, 8787) // spellbook
        };

        public override object Message => 1060110;

        public override QuestItemInfo[] Info => m_Info;

        public override void OnRead()
        {
            System.AddObjective(new FetchAbraxusScrollObjective());
        }
    }

    public class ReadAbraxusScrollConversation : QuestConversation
    {
        public override object Message => 1060114;

        public override void OnRead()
        {
            System.AddObjective(new ReadAbraxusScrollObjective());
        }
    }

    public class SecondHorusConversation : QuestConversation
    {
        public override object Message => 1060118;

        public override void OnRead()
        {
            System.AddObjective(new FindCallingScrollObjective());
        }
    }

    public class HealConversation : QuestConversation
    {
        public override object Message => 1061610;
    }

    public class HorusRewardConversation : QuestConversation
    {
        public override object Message => 1060717;

        public override bool Logged => false;
    }

    public class LostCallingScrollConversation : QuestConversation
    {
        private bool m_FromMardoth;

        public LostCallingScrollConversation(bool fromMardoth) => m_FromMardoth = fromMardoth;

        // Serialization
        public LostCallingScrollConversation()
        {
        }

        public override object Message
        {
            get
            {
                if (m_FromMardoth)
                {
                    return 1062058;
                }

                /* You have arrived at the well, but no longer have the scroll
                   * of calling.  Use Mardoth's teleporter to return to the
                   * Crystal Cave and fetch another scroll from the box.
                   */
                return 1060129;
            }
        }

        public override bool Logged => false;

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            m_FromMardoth = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(m_FromMardoth);
        }
    }

    public class MardothKronusConversation : QuestConversation
    {
        public override object Message => 1060121;

        public override void OnRead()
        {
            System.AddObjective(new FindWellOfTearsObjective());
        }
    }

    public class MardothEndConversation : QuestConversation
    {
        public override object Message => 1060133;

        public override void OnRead()
        {
            System.AddObjective(new FindBankObjective());
        }
    }

    public class BankerConversation : QuestConversation
    {
        public override object Message => 1060137;

        public override void OnRead()
        {
            System.Complete();
        }
    }

    public class RadarConversation : QuestConversation
    {
        public override object Message => 1061692;

        public override bool Logged => false;
    }
}
