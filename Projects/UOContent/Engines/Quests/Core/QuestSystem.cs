using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Engines.Quests.Ambitious;
using Server.Engines.Quests.Collector;
using Server.Engines.Quests.Doom;
using Server.Engines.Quests.Hag;
using Server.Engines.Quests.Haven;
using Server.Engines.Quests.Matriarch;
using Server.Engines.Quests.Naturalist;
using Server.Engines.Quests.Necro;
using Server.Engines.Quests.Ninja;
using Server.Engines.Quests.Samurai;
using Server.Engines.Quests.Zento;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Quests
{
    public delegate void QuestCallback();

    public abstract class QuestSystem
    {
        public static readonly Type[] QuestTypes =
        {
            typeof(TheSummoningQuest),
            typeof(DarkTidesQuest),
            typeof(UzeraanTurmoilQuest),
            typeof(CollectorQuest),
            typeof(WitchApprenticeQuest),
            typeof(StudyOfSolenQuest),
            typeof(SolenMatriarchQuest),
            typeof(AmbitiousQueenQuest),
            typeof(EminosUndertakingQuest),
            typeof(HaochisTrialsQuest),
            typeof(TerribleHatchlingsQuest)
        };

        private Timer m_Timer;

        public QuestSystem(PlayerMobile from)
        {
            From = from;
            Objectives = new List<QuestObjective>();
            Conversations = new List<QuestConversation>();
        }

        public QuestSystem()
        {
        }

        public abstract object Name { get; }
        public abstract object OfferMessage { get; }

        public abstract int Picture { get; }

        public abstract bool IsTutorial { get; }
        public abstract TimeSpan RestartDelay { get; }

        public abstract Type[] TypeReferenceTable { get; }

        public PlayerMobile From { get; set; }

        public List<QuestObjective> Objectives { get; set; }

        public List<QuestConversation> Conversations { get; set; }

        public virtual void StartTimer()
        {
            if (m_Timer != null)
            {
                return;
            }

            m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5), Slice);
        }

        public virtual void StopTimer()
        {
            m_Timer?.Stop();

            m_Timer = null;
        }

        public virtual void Slice()
        {
            for (var i = Objectives.Count - 1; i >= 0; --i)
            {
                var obj = Objectives[i];

                if (obj.GetTimerEvent())
                {
                    obj.CheckProgress();
                }
            }
        }

        public virtual void OnKill(BaseCreature creature, Container corpse)
        {
            for (var i = Objectives.Count - 1; i >= 0; --i)
            {
                var obj = Objectives[i];

                if (obj.GetKillEvent(creature, corpse))
                {
                    obj.OnKill(creature, corpse);
                }
            }
        }

        public virtual bool IgnoreYoungProtection(Mobile from)
        {
            for (var i = Objectives.Count - 1; i >= 0; --i)
            {
                var obj = Objectives[i];

                if (obj.IgnoreYoungProtection(from))
                {
                    return true;
                }
            }

            return false;
        }

        public virtual void BaseDeserialize(IGenericReader reader)
        {
            var referenceTable = TypeReferenceTable;

            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        var count = reader.ReadEncodedInt();

                        Objectives = new List<QuestObjective>(count);

                        for (var i = 0; i < count; ++i)
                        {
                            var obj = QuestSerializer.DeserializeObjective(referenceTable, reader);

                            if (obj != null)
                            {
                                obj.System = this;
                                Objectives.Add(obj);
                            }
                        }

                        count = reader.ReadEncodedInt();

                        Conversations = new List<QuestConversation>(count);

                        for (var i = 0; i < count; ++i)
                        {
                            var conv = QuestSerializer.DeserializeConversation(referenceTable, reader);

                            if (conv != null)
                            {
                                conv.System = this;
                                Conversations.Add(conv);
                            }
                        }

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
            var referenceTable = TypeReferenceTable;

            writer.WriteEncodedInt(0); // version

            writer.WriteEncodedInt(Objectives.Count);

            for (var i = 0; i < Objectives.Count; ++i)
            {
                QuestSerializer.Serialize(referenceTable, Objectives[i], writer);
            }

            writer.WriteEncodedInt(Conversations.Count);

            for (var i = 0; i < Conversations.Count; ++i)
            {
                QuestSerializer.Serialize(referenceTable, Conversations[i], writer);
            }

            ChildSerialize(writer);
        }

        public virtual void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
        }

        public bool IsObjectiveInProgress(Type type)
        {
            var obj = FindObjective(type);

            return obj?.Completed == false;
        }

        public T FindObjective<T>() where T : QuestObjective
        {
            for (var i = Objectives.Count - 1; i >= 0; --i)
            {
                var obj = Objectives[i];

                if (obj is T t)
                {
                    return t;
                }
            }

            return null;
        }

        public QuestObjective FindObjective(Type type)
        {
            for (var i = Objectives.Count - 1; i >= 0; --i)
            {
                var obj = Objectives[i];

                if (obj.GetType() == type)
                {
                    return obj;
                }
            }

            return null;
        }

        public virtual void SendOffer()
        {
            From.SendGump(new QuestOfferGump(this));
        }

        public virtual void GetContextMenuEntries(List<ContextMenuEntry> list)
        {
            if (Objectives.Count > 0)
            {
                list.Add(new QuestCallbackEntry(6154, ShowQuestLog)); // View Quest Log
            }

            if (Conversations.Count > 0)
            {
                list.Add(new QuestCallbackEntry(6156, ShowQuestConversation)); // Quest Conversation
            }

            list.Add(new QuestCallbackEntry(6155, BeginCancelQuest)); // Cancel Quest
        }

        public virtual void ShowQuestLogUpdated()
        {
            From.CloseGump<QuestLogUpdatedGump>();
            From.SendGump(new QuestLogUpdatedGump(this));
        }

        public virtual void ShowQuestLog()
        {
            if (Objectives.Count > 0)
            {
                From.CloseGump<QuestItemInfoGump>();
                From.CloseGump<QuestLogUpdatedGump>();
                From.CloseGump<QuestObjectivesGump>();
                From.CloseGump<QuestConversationsGump>();

                From.SendGump(new QuestObjectivesGump(Objectives));

                var last = Objectives[^1];

                if (last.Info != null)
                {
                    From.SendGump(new QuestItemInfoGump(last.Info));
                }
            }
        }

        public virtual void ShowQuestConversation()
        {
            if (Conversations.Count > 0)
            {
                From.CloseGump<QuestItemInfoGump>();
                From.CloseGump<QuestObjectivesGump>();
                From.CloseGump<QuestConversationsGump>();

                From.SendGump(new QuestConversationsGump(Conversations));

                var last = Conversations[^1];

                if (last.Info != null)
                {
                    From.SendGump(new QuestItemInfoGump(last.Info));
                }
            }
        }

        public virtual void BeginCancelQuest()
        {
            From.SendGump(new QuestCancelGump(this));
        }

        public virtual void EndCancelQuest(bool shouldCancel)
        {
            if (From.Quest != this)
            {
                return;
            }

            if (shouldCancel)
            {
                From.SendLocalizedMessage(1049015); // You have canceled your quest.
                Cancel();
            }
            else
            {
                From.SendLocalizedMessage(1049014); // You have chosen not to cancel your quest.
            }
        }

        public virtual void Cancel()
        {
            ClearQuest(false);
        }

        public virtual void Complete()
        {
            ClearQuest(true);
        }

        public virtual void ClearQuest(bool completed)
        {
            StopTimer();

            if (From.Quest == this)
            {
                From.Quest = null;

                var restartDelay = RestartDelay;

                if (completed && restartDelay > TimeSpan.Zero || !completed && restartDelay == TimeSpan.MaxValue)
                {
                    From.DoneQuests ??= new List<QuestRestartInfo>();

                    var found = false;

                    var ourQuestType = GetType();

                    for (var i = 0; i < From.DoneQuests.Count; ++i)
                    {
                        var restartInfo = From.DoneQuests[i];

                        if (restartInfo.QuestType == ourQuestType)
                        {
                            restartInfo.Reset(restartDelay);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        From.DoneQuests.Add(new QuestRestartInfo(ourQuestType, restartDelay));
                    }
                }
            }
        }

        public virtual void AddConversation(QuestConversation conv)
        {
            conv.System = this;

            if (conv.Logged)
            {
                Conversations.Add(conv);
            }

            From.CloseGump<QuestItemInfoGump>();
            From.CloseGump<QuestObjectivesGump>();
            From.CloseGump<QuestConversationsGump>();
            From.SendGump(conv.Logged ? new QuestConversationsGump(Conversations) : new QuestConversationsGump(conv));

            if (conv.Info != null)
            {
                From.SendGump(new QuestItemInfoGump(conv.Info));
            }
        }

        public virtual void AddObjective(QuestObjective obj)
        {
            obj.System = this;
            Objectives.Add(obj);

            ShowQuestLogUpdated();
        }

        public virtual void Accept()
        {
            if (From.Quest != null)
            {
                return;
            }

            From.Quest = this;
            From.SendLocalizedMessage(1049019); // You have accepted the Quest.

            StartTimer();
        }

        public virtual void Decline()
        {
            From.SendLocalizedMessage(1049018); // You have declined the Quest.
        }

        public static bool CanOfferQuest(Mobile check, Type questType) => CanOfferQuest(check, questType, out _);

        public static bool CanOfferQuest(Mobile check, Type questType, out bool inRestartPeriod)
        {
            inRestartPeriod = false;

            if (!(check is PlayerMobile pm))
            {
                return false;
            }

            if (pm.HasGump<QuestOfferGump>())
            {
                return false;
            }

            if (questType == typeof(DarkTidesQuest) && pm.Profession != 4) // necromancer
            {
                return false;
            }

            if (questType == typeof(UzeraanTurmoilQuest) && pm.Profession != 1 && pm.Profession != 2 && pm.Profession != 5
            ) // warrior / magician / paladin
            {
                return false;
            }

            if (questType == typeof(HaochisTrialsQuest) && pm.Profession != 6) // samurai
            {
                return false;
            }

            if (questType == typeof(EminosUndertakingQuest) && pm.Profession != 7) // ninja
            {
                return false;
            }

            var doneQuests = pm.DoneQuests;

            if (doneQuests != null)
            {
                for (var i = 0; i < doneQuests.Count; ++i)
                {
                    var restartInfo = doneQuests[i];

                    if (restartInfo.QuestType == questType)
                    {
                        var endTime = restartInfo.RestartTime;

                        if (Core.Now < endTime)
                        {
                            inRestartPeriod = true;
                            return false;
                        }

                        doneQuests.RemoveAt(i);
                        return true;
                    }
                }
            }

            return true;
        }

        public static void FocusTo(Mobile who, Mobile to)
        {
            if (Utility.RandomBool())
            {
                who.Animate(17, 7, 1, true, false, 0);
            }
            else
            {
                who.Animate(32 + Utility.Random(3), 7, 1, true, false, 0);
            }

            who.Direction = who.GetDirectionTo(to);
        }

        public static int RandomBrightHue()
        {
            if (Utility.RandomDouble() < 0.1)
            {
                return Utility.RandomList(0x62, 0x71);
            }

            return Utility.RandomList(0x03, 0x0D, 0x13, 0x1C, 0x21, 0x30, 0x37, 0x3A, 0x44, 0x59);
        }
    }

    public class QuestCancelGump : BaseQuestGump
    {
        private readonly QuestSystem m_System;

        public QuestCancelGump(QuestSystem system) : base(120, 50)
        {
            m_System = system;

            Closable = false;

            AddPage(0);

            AddImageTiled(0, 0, 348, 262, 2702);
            AddAlphaRegion(0, 0, 348, 262);

            AddImage(0, 15, 10152);
            AddImageTiled(0, 30, 17, 200, 10151);
            AddImage(0, 230, 10154);

            AddImage(15, 0, 10252);
            AddImageTiled(30, 0, 300, 17, 10250);
            AddImage(315, 0, 10254);

            AddImage(15, 244, 10252);
            AddImageTiled(30, 244, 300, 17, 10250);
            AddImage(315, 244, 10254);

            AddImage(330, 15, 10152);
            AddImageTiled(330, 30, 17, 200, 10151);
            AddImage(330, 230, 10154);

            AddImage(333, 2, 10006);
            AddImage(333, 248, 10006);
            AddImage(2, 248, 10006);
            AddImage(2, 2, 10006);

            AddHtmlLocalized(25, 22, 200, 20, 1049000, 32000); // Confirm Quest Cancellation
            AddImage(25, 40, 3007);

            if (system.IsTutorial)
            {
                AddHtmlLocalized(
                    25,
                    55,
                    300,
                    120,
                    1060836,
                    White
                ); // This quest will give you valuable information, skills and equipment that will help you advance in the game at a quicker pace.<BR><BR>Are you certain you wish to cancel at this time?
            }
            else
            {
                AddHtmlLocalized(25, 60, 300, 20, 1049001, White); // You have chosen to abort your quest:
                AddImage(25, 81, 0x25E7);
                AddHtmlObject(48, 80, 280, 20, system.Name, DarkGreen, false, false);

                AddHtmlLocalized(25, 120, 280, 20, 1049002, White); // Can this quest be restarted after quitting?
                AddImage(25, 141, 0x25E7);
                AddHtmlLocalized(
                    48,
                    140,
                    280,
                    20,
                    system.RestartDelay < TimeSpan.MaxValue ? 1049016 : 1049017,
                    DarkGreen
                ); // Yes/No
            }

            AddRadio(25, 175, 9720, 9723, true, 1);
            AddHtmlLocalized(60, 180, 280, 20, 1049005, White); // Yes, I really want to quit!

            AddRadio(25, 210, 9720, 9723, false, 0);
            AddHtmlLocalized(60, 215, 280, 20, 1049006, White); // No, I don't want to quit.

            AddButton(265, 220, 247, 248, 1);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1)
            {
                m_System.EndCancelQuest(info.IsSwitched(1));
            }
        }
    }

    public class QuestOfferGump : BaseQuestGump
    {
        private readonly QuestSystem m_System;

        public QuestOfferGump(QuestSystem system) : base(75, 25)
        {
            m_System = system;

            Closable = false;

            AddPage(0);

            AddImageTiled(50, 20, 400, 400, 2624);
            AddAlphaRegion(50, 20, 400, 400);

            AddImage(90, 33, 9005);
            AddHtmlLocalized(130, 45, 270, 20, 1049010, White); // Quest Offer
            AddImageTiled(130, 65, 175, 1, 9101);

            AddImage(140, 110, 1209);
            AddHtmlObject(160, 108, 250, 20, system.Name, DarkGreen, false, false);

            AddHtmlObject(98, 140, 312, 200, system.OfferMessage, LightGreen, false, true);

            AddRadio(85, 350, 9720, 9723, true, 1);
            AddHtmlLocalized(120, 356, 280, 20, 1049011, White); // I accept!

            AddRadio(85, 385, 9720, 9723, false, 0);
            AddHtmlLocalized(120, 391, 280, 20, 1049012, White); // No thanks, I decline.

            AddButton(340, 390, 247, 248, 1);

            AddImageTiled(50, 29, 30, 390, 10460);
            AddImageTiled(34, 140, 17, 279, 9263);

            AddImage(48, 135, 10411);
            AddImage(-16, 285, 10402);
            AddImage(0, 10, 10421);
            AddImage(25, 0, 10420);

            AddImageTiled(83, 15, 350, 15, 10250);

            AddImage(34, 419, 10306);
            AddImage(442, 419, 10304);
            AddImageTiled(51, 419, 392, 17, 10101);

            AddImageTiled(415, 29, 44, 390, 2605);
            AddImageTiled(415, 29, 30, 390, 10460);
            AddImage(425, 0, 10441);

            AddImage(370, 50, 1417);
            AddImage(379, 60, system.Picture);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1)
            {
                if (info.IsSwitched(1))
                {
                    m_System.Accept();
                }
                else
                {
                    m_System.Decline();
                }
            }
        }
    }

    public abstract class BaseQuestGump : Gump
    {
        public const int Black = 0x0000;
        public const int White = 0x7FFF;
        public const int DarkGreen = 10000;
        public const int LightGreen = 90000;
        public const int Blue = 19777215;

        public BaseQuestGump(int x, int y) : base(x, y)
        {
        }

        public static int C16232(int c16)
        {
            c16 &= 0x7FFF;

            var r = ((c16 >> 10) & 0x1F) << 3;
            var g = ((c16 >> 05) & 0x1F) << 3;
            var b = (c16 & 0x1F) << 3;

            return (r << 16) | (g << 8) | b;
        }

        public static int C16216(int c16) => c16 & 0x7FFF;

        public static int C32216(int c32)
        {
            c32 &= 0xFFFFFF;

            var r = ((c32 >> 16) & 0xFF) >> 3;
            var g = ((c32 >> 08) & 0xFF) >> 3;
            var b = (c32 & 0xFF) >> 3;

            return (r << 10) | (g << 5) | b;
        }

        public static string Color(string text, int color) => $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";

        public void AddHtmlObject(int x, int y, int width, int height, object message, int color, bool back, bool scroll)
        {
            if (message is int html)
            {
                AddHtmlLocalized(x, y, width, height, html, C16216(color), back, scroll);
            }
            else
            {
                AddHtml(x, y, width, height, Color(message.ToString(), C16232(color)), back, scroll);
            }
        }
    }
}
