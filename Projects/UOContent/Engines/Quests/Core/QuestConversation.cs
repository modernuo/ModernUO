using System.Collections.Generic;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.Quests
{
    public abstract class QuestConversation
    {
        public abstract object Message { get; }

        public virtual QuestItemInfo[] Info => null;
        public virtual bool Logged => true;

        public QuestSystem System { get; set; }

        public bool HasBeenRead { get; set; }

        public virtual void BaseDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        HasBeenRead = reader.ReadBool();

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
            writer.WriteEncodedInt(0); // version

            writer.Write(HasBeenRead);

            ChildSerialize(writer);
        }

        public virtual void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
        }

        public virtual void OnRead()
        {
        }
    }

    public class QuestConversationsGump : BaseQuestGump
    {
        private readonly List<QuestConversation> m_Conversations;

        public QuestConversationsGump(QuestConversation conv) : this(new List<QuestConversation> { conv })
        {
        }

        public QuestConversationsGump(List<QuestConversation> conversations) : base(30, 50)
        {
            m_Conversations = conversations;

            Closable = false;

            AddPage(0);

            AddImage(349, 10, 9392);
            AddImageTiled(349, 130, 100, 120, 9395);
            AddImageTiled(149, 10, 200, 140, 9391);
            AddImageTiled(149, 250, 200, 140, 9397);
            AddImage(349, 250, 9398);
            AddImage(35, 10, 9390);
            AddImageTiled(35, 150, 120, 100, 9393);
            AddImage(35, 250, 9396);

            AddHtmlLocalized(110, 60, 200, 20, 1049069, White); // <STRONG>Conversation Event</STRONG>

            AddImage(65, 14, 10102);
            AddImageTiled(81, 14, 349, 17, 10101);
            AddImage(426, 14, 10104);

            AddImageTiled(55, 40, 388, 323, 2624);
            AddAlphaRegion(55, 40, 388, 323);

            AddImageTiled(75, 90, 200, 1, 9101);
            AddImage(75, 58, 9781);
            AddImage(380, 45, 223);

            AddButton(220, 335, 2313, 2312, 1);
            AddImage(0, 0, 10440);

            AddPage(1);

            for (var i = 0; i < conversations.Count; ++i)
            {
                var conv = conversations[conversations.Count - 1 - i];

                if (i > 0)
                {
                    AddButton(65, 366, 9909, 9911, 0, GumpButtonType.Page, 1 + i);
                    AddHtmlLocalized(90, 367, 50, 20, 1043354, Black); // Previous

                    AddPage(1 + i);
                }

                AddHtmlObject(70, 110, 365, 220, conv.Message, LightGreen, false, true);

                if (i > 0)
                {
                    AddButton(420, 366, 9903, 9905, 0, GumpButtonType.Page, i);
                    AddHtmlLocalized(370, 367, 50, 20, 1043353, Black); // Next
                }
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            for (var i = m_Conversations.Count - 1; i >= 0; --i)
            {
                var qc = m_Conversations[i];

                if (!qc.HasBeenRead)
                {
                    qc.HasBeenRead = true;
                    qc.OnRead();
                }
            }
        }
    }
}
