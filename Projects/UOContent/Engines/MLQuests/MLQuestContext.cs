using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Engines.MLQuests
{
    [Flags]
    public enum MLQuestFlag
    {
        None = 0x00,
        Spellweaving = 0x01,
        SummonFey = 0x02,
        SummonFiend = 0x04,
        BedlamAccess = 0x08
    }

    [PropertyObject]
    public class MLQuestContext
    {
        private readonly List<MLDoneQuestInfo> m_DoneQuests;
        private MLQuestFlag m_Flags;

        public MLQuestContext(PlayerMobile owner)
        {
            Owner = owner;
            QuestInstances = new List<MLQuestInstance>();
            m_DoneQuests = new List<MLDoneQuestInfo>();
            ChainOffers = new List<MLQuest>();
            m_Flags = MLQuestFlag.None;
        }

        public MLQuestContext(IGenericReader reader, int version)
        {
            Owner = reader.ReadMobile<PlayerMobile>();
            QuestInstances = new List<MLQuestInstance>();
            m_DoneQuests = new List<MLDoneQuestInfo>();
            ChainOffers = new List<MLQuest>();

            int instances = reader.ReadInt();

            for (int i = 0; i < instances; ++i)
            {
                MLQuestInstance instance = MLQuestInstance.Deserialize(reader, version, Owner);

                if (instance != null)
                    QuestInstances.Add(instance);
            }

            int doneQuests = reader.ReadInt();

            for (int i = 0; i < doneQuests; ++i)
            {
                MLDoneQuestInfo info = MLDoneQuestInfo.Deserialize(reader, version);

                if (info != null)
                    m_DoneQuests.Add(info);
            }

            int chainOffers = reader.ReadInt();

            for (int i = 0; i < chainOffers; ++i)
            {
                MLQuest quest = MLQuestSystem.ReadQuestRef(reader);

                if (quest?.IsChainTriggered == true)
                    ChainOffers.Add(quest);
            }

            m_Flags = (MLQuestFlag)reader.ReadEncodedInt();
        }

        public PlayerMobile Owner { get; }

        public List<MLQuestInstance> QuestInstances { get; }

        public List<MLQuest> ChainOffers { get; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsFull => QuestInstances.Count >= MLQuestSystem.MaxConcurrentQuests;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Spellweaving
        {
            get => GetFlag(MLQuestFlag.Spellweaving);
            set => SetFlag(MLQuestFlag.Spellweaving, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool SummonFey
        {
            get => GetFlag(MLQuestFlag.SummonFey);
            set => SetFlag(MLQuestFlag.SummonFey, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool SummonFiend
        {
            get => GetFlag(MLQuestFlag.SummonFiend);
            set => SetFlag(MLQuestFlag.SummonFiend, value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool BedlamAccess
        {
            get => GetFlag(MLQuestFlag.BedlamAccess);
            set => SetFlag(MLQuestFlag.BedlamAccess, value);
        }

        public bool HasDoneQuest(Type questType)
        {
            MLQuest quest = MLQuestSystem.FindQuest(questType);

            return quest != null && HasDoneQuest(quest);
        }

        public bool HasDoneQuest(MLQuest quest)
        {
            foreach (MLDoneQuestInfo info in m_DoneQuests)
                if (info.m_Quest == quest)
                    return true;

            return false;
        }

        public bool HasDoneQuest(MLQuest quest, out DateTime nextAvailable)
        {
            nextAvailable = DateTime.MinValue;

            foreach (MLDoneQuestInfo info in m_DoneQuests)
                if (info.m_Quest == quest)
                {
                    nextAvailable = info.m_NextAvailable;
                    return true;
                }

            return false;
        }

        public void SetDoneQuest(MLQuest quest)
        {
            SetDoneQuest(quest, DateTime.MinValue);
        }

        public void SetDoneQuest(MLQuest quest, DateTime nextAvailable)
        {
            foreach (MLDoneQuestInfo info in m_DoneQuests)
                if (info.m_Quest == quest)
                {
                    info.m_NextAvailable = nextAvailable;
                    return;
                }

            m_DoneQuests.Add(new MLDoneQuestInfo(quest, nextAvailable));
        }

        public void RemoveDoneQuest(MLQuest quest)
        {
            for (int i = m_DoneQuests.Count - 1; i >= 0; --i)
            {
                MLDoneQuestInfo info = m_DoneQuests[i];

                if (info.m_Quest == quest)
                    m_DoneQuests.RemoveAt(i);
            }
        }

        public void HandleDeath()
        {
            for (int i = QuestInstances.Count - 1; i >= 0; --i)
                QuestInstances[i].OnPlayerDeath();
        }

        public void HandleDeletion()
        {
            for (int i = QuestInstances.Count - 1; i >= 0; --i)
                QuestInstances[i].Remove();
        }

        public MLQuestInstance FindInstance(Type questType)
        {
            MLQuest quest = MLQuestSystem.FindQuest(questType);

            if (quest == null)
                return null;

            return FindInstance(quest);
        }

        public MLQuestInstance FindInstance(MLQuest quest)
        {
            foreach (MLQuestInstance instance in QuestInstances)
                if (instance.Quest == quest)
                    return instance;

            return null;
        }

        public bool IsDoingQuest(Type questType)
        {
            MLQuest quest = MLQuestSystem.FindQuest(questType);

            return quest != null && IsDoingQuest(quest);
        }

        public bool IsDoingQuest(MLQuest quest) => FindInstance(quest) != null;

        public void Serialize(IGenericWriter writer)
        {
            // Version info is written in MLQuestPersistence.Serialize

            writer.WriteMobile(Owner);
            writer.Write(QuestInstances.Count);

            foreach (MLQuestInstance instance in QuestInstances)
                instance.Serialize(writer);

            writer.Write(m_DoneQuests.Count);

            foreach (MLDoneQuestInfo info in m_DoneQuests)
                info.Serialize(writer);

            writer.Write(ChainOffers.Count);

            foreach (MLQuest quest in ChainOffers)
                MLQuestSystem.WriteQuestRef(writer, quest);

            writer.WriteEncodedInt((int)m_Flags);
        }

        public bool GetFlag(MLQuestFlag flag) => (m_Flags & flag) != 0;

        public void SetFlag(MLQuestFlag flag, bool value)
        {
            if (value)
                m_Flags |= flag;
            else
                m_Flags &= ~flag;
        }

        private class MLDoneQuestInfo
        {
            public DateTime m_NextAvailable;
            public readonly MLQuest m_Quest;

            public MLDoneQuestInfo(MLQuest quest, DateTime nextAvailable)
            {
                m_Quest = quest;
                m_NextAvailable = nextAvailable;
            }

            public void Serialize(IGenericWriter writer)
            {
                MLQuestSystem.WriteQuestRef(writer, m_Quest);
                writer.Write(m_NextAvailable);
            }

            public static MLDoneQuestInfo Deserialize(IGenericReader reader, int version)
            {
                MLQuest quest = MLQuestSystem.ReadQuestRef(reader);
                DateTime nextAvailable = reader.ReadDateTime();

                if (quest?.RecordCompletion != true)
                    return null; // forget about this record

                return new MLDoneQuestInfo(quest, nextAvailable);
            }
        }
    }
}
