using System.Collections.Generic;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Quests
{
    public abstract class QuestObjective
    {
        private int m_CurProgress;

        public abstract object Message { get; }

        public virtual int MaxProgress => 1;
        public virtual QuestItemInfo[] Info => null;

        public QuestSystem System { get; set; }

        public bool HasBeenRead { get; set; }

        public int CurProgress
        {
            get => m_CurProgress;
            set
            {
                m_CurProgress = value;
                CheckCompletionStatus();
            }
        }

        public bool HasCompleted { get; set; }

        public virtual bool Completed => m_CurProgress >= MaxProgress;

        public bool IsSingleObjective => MaxProgress == 1;

        public virtual void BaseDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 1:
                    {
                        HasBeenRead = reader.ReadBool();
                        goto case 0;
                    }
                case 0:
                    {
                        m_CurProgress = reader.ReadEncodedInt();
                        HasCompleted = reader.ReadBool();

                        break;
                    }
            }

            ChildDeserialize(reader);
        }

        public virtual void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();
        }

        public virtual void BaseSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(1); // version

            writer.Write(HasBeenRead);
            writer.WriteEncodedInt(m_CurProgress);
            writer.Write(HasCompleted);

            ChildSerialize(writer);
        }

        public virtual void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
        }

        public virtual void Complete()
        {
            CurProgress = MaxProgress;
        }

        public virtual void RenderMessage(BaseQuestGump gump)
        {
            gump.AddHtmlObject(70, 130, 300, 100, Message, BaseQuestGump.Blue, false, false);
        }

        public virtual void RenderProgress(BaseQuestGump gump)
        {
            gump.AddHtmlObject(70, 260, 270, 100, Completed ? 1049077 : 1049078, BaseQuestGump.Blue, false, false);
        }

        public virtual void CheckCompletionStatus()
        {
            if (Completed && !HasCompleted)
            {
                HasCompleted = true;
                OnComplete();
            }
        }

        public virtual void OnRead()
        {
        }

        public virtual bool GetTimerEvent() => !Completed;

        public virtual void CheckProgress()
        {
        }

        public virtual void OnComplete()
        {
        }

        public virtual bool GetKillEvent(BaseCreature creature, Container corpse) => !Completed;

        public virtual void OnKill(BaseCreature creature, Container corpse)
        {
        }

        public virtual bool IgnoreYoungProtection(Mobile from) => false;
    }

    public class QuestLogUpdatedGump : BaseQuestGump
    {
        private readonly QuestSystem m_System;

        public QuestLogUpdatedGump(QuestSystem system) : base(3, 30)
        {
            m_System = system;

            AddPage(0);

            AddImage(20, 5, 1417);

            AddHtmlLocalized(0, 78, 120, 40, 1049079, White); // Quest Log Updated

            AddImageTiled(0, 78, 120, 40, 2624);
            AddAlphaRegion(0, 78, 120, 40);

            AddButton(30, 15, 5575, 5576, 1);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1)
            {
                m_System.ShowQuestLog();
            }
        }
    }

    public class QuestObjectivesGump : BaseQuestGump
    {
        private readonly List<QuestObjective> m_Objectives;

        public QuestObjectivesGump(QuestObjective obj) : this(new List<QuestObjective> { obj })
        {
        }

        public QuestObjectivesGump(List<QuestObjective> objectives) : base(90, 50)
        {
            m_Objectives = objectives;

            Closable = false;

            AddPage(0);

            AddImage(0, 0, 3600);
            AddImageTiled(0, 14, 15, 375, 3603);
            AddImageTiled(380, 14, 14, 375, 3605);
            AddImage(0, 376, 3606);
            AddImageTiled(15, 376, 370, 16, 3607);
            AddImageTiled(15, 0, 370, 16, 3601);
            AddImage(380, 0, 3602);
            AddImage(380, 376, 3608);

            AddImageTiled(15, 15, 365, 365, 2624);
            AddAlphaRegion(15, 15, 365, 365);

            AddImage(20, 87, 1231);
            AddImage(75, 62, 9307);

            AddHtmlLocalized(117, 35, 230, 20, 1046026, Blue); // Quest Log

            AddImage(77, 33, 9781);
            AddImage(65, 110, 2104);

            AddHtmlLocalized(79, 106, 230, 20, 1049073, Blue); // Objective:

            AddImageTiled(68, 125, 120, 1, 9101);
            AddImage(65, 240, 2104);

            AddHtmlLocalized(79, 237, 230, 20, 1049076, Blue); // Progress details:

            AddImageTiled(68, 255, 120, 1, 9101);
            AddButton(175, 355, 2313, 2312, 1);

            AddImage(341, 15, 10450);
            AddImage(341, 330, 10450);
            AddImage(15, 330, 10450);
            AddImage(15, 15, 10450);

            AddPage(1);

            for (var i = 0; i < objectives.Count; ++i)
            {
                var obj = objectives[objectives.Count - 1 - i];

                if (i > 0)
                {
                    AddButton(55, 346, 9909, 9911, 0, GumpButtonType.Page, 1 + i);
                    AddHtmlLocalized(82, 347, 50, 20, 1043354, White); // Previous

                    AddPage(1 + i);
                }

                obj.RenderMessage(this);
                obj.RenderProgress(this);

                if (i > 0)
                {
                    AddButton(317, 346, 9903, 9905, 0, GumpButtonType.Page, i);
                    AddHtmlLocalized(278, 347, 50, 20, 1043353, White); // Next
                }
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            for (var i = m_Objectives.Count - 1; i >= 0; --i)
            {
                var obj = m_Objectives[i];

                if (!obj.HasBeenRead)
                {
                    obj.HasBeenRead = true;
                    obj.OnRead();
                }
            }
        }
    }
}
