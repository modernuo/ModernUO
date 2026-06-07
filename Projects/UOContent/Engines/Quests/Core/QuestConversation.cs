using System.Collections.Generic;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.Quests
{
    public abstract class QuestConversation
    {
        public abstract int Message { get; }

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
        private readonly List<QuestConversation> _conversations;

        private QuestConversationsGump(List<QuestConversation> conversations) : base(30, 50) =>
            _conversations = conversations;

        public static void DisplayTo(Mobile from, QuestConversation conv)
        {
            if (from?.NetState == null || conv == null)
            {
                return;
            }

            from.SendGump(new QuestConversationsGump(new List<QuestConversation> { conv }));
        }

        public static void DisplayTo(Mobile from, List<QuestConversation> conversations)
        {
            if (from?.NetState == null || conversations == null || conversations.Count == 0)
            {
                return;
            }

            from.SendGump(new QuestConversationsGump(conversations));
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            builder.SetNoClose();

            builder.AddPage();

            builder.AddImage(349, 10, 9392);
            builder.AddImageTiled(349, 130, 100, 120, 9395);
            builder.AddImageTiled(149, 10, 200, 140, 9391);
            builder.AddImageTiled(149, 250, 200, 140, 9397);
            builder.AddImage(349, 250, 9398);
            builder.AddImage(35, 10, 9390);
            builder.AddImageTiled(35, 150, 120, 100, 9393);
            builder.AddImage(35, 250, 9396);

            builder.AddHtmlLocalized(110, 60, 200, 20, 1049069, White); // <STRONG>Conversation Event</STRONG>

            builder.AddImage(65, 14, 10102);
            builder.AddImageTiled(81, 14, 349, 17, 10101);
            builder.AddImage(426, 14, 10104);

            builder.AddImageTiled(55, 40, 388, 323, 2624);
            builder.AddAlphaRegion(55, 40, 388, 323);

            builder.AddImageTiled(75, 90, 200, 1, 9101);
            builder.AddImage(75, 58, 9781);
            builder.AddImage(380, 45, 223);

            builder.AddButton(220, 335, 2313, 2312, 1);
            builder.AddImage(0, 0, 10440);

            builder.AddPage(1);

            for (var i = 0; i < _conversations.Count; ++i)
            {
                var conv = _conversations[_conversations.Count - 1 - i];

                if (i > 0)
                {
                    builder.AddButton(65, 366, 9909, 9911, 0, GumpButtonType.Page, 1 + i);
                    builder.AddHtmlLocalized(90, 367, 50, 20, 1043354, Black); // Previous

                    builder.AddPage(1 + i);
                }

                AddHtmlObject(ref builder, 70, 110, 365, 220, conv.Message, LightGreen, false, true);

                if (i > 0)
                {
                    builder.AddButton(420, 366, 9903, 9905, 0, GumpButtonType.Page, i);
                    builder.AddHtmlLocalized(370, 367, 50, 20, 1043353, Black); // Next
                }
            }
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            for (var i = _conversations.Count - 1; i >= 0; --i)
            {
                var qc = _conversations[i];

                if (!qc.HasBeenRead)
                {
                    qc.HasBeenRead = true;
                    qc.OnRead();
                }
            }
        }
    }
}
