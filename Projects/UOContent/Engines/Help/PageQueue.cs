using System;
using System.Collections.Generic;
using Server.Misc;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Help
{
    public enum PageType
    {
        Bug,
        Stuck,
        Account,
        Question,
        Suggestion,
        Other,
        VerbalHarassment,
        PhysicalHarassment
    }

    public class PageEntry
    {
        // What page types should have a speech log as attachment?
        public static readonly PageType[] SpeechLogAttachment =
        {
            PageType.VerbalHarassment
        };

        private Mobile m_Handler;

        private Timer m_Timer;

        public PageEntry(Mobile sender, string message, PageType type)
        {
            Sender = sender;
            Sent = Core.Now;
            Message = Utility.FixHtml(message);
            Type = type;
            PageLocation = sender.Location;
            PageMap = sender.Map;

            if (sender is PlayerMobile pm && pm.SpeechLog != null && Array.IndexOf(SpeechLogAttachment, type) >= 0)
            {
                SpeechLog = new List<SpeechLogEntry>(pm.SpeechLog);
            }

            m_Timer = new InternalTimer(this);
            m_Timer.Start();
        }

        public Mobile Sender { get; }

        public Mobile Handler
        {
            get => m_Handler;
            set
            {
                PageQueue.OnHandlerChanged(m_Handler, value, this);
                m_Handler = value;
            }
        }

        public DateTime Sent { get; }

        public string Message { get; }

        public PageType Type { get; }

        public Point3D PageLocation { get; }

        public Map PageMap { get; }

        public List<SpeechLogEntry> SpeechLog { get; }

        public void Stop()
        {
            m_Timer?.Stop();

            m_Timer = null;
        }

        private class InternalTimer : Timer
        {
            private static readonly TimeSpan StatusDelay = TimeSpan.FromMinutes(2.0);

            private readonly PageEntry m_Entry;

            public InternalTimer(PageEntry entry) : base(TimeSpan.FromSeconds(1.0), StatusDelay) => m_Entry = entry;

            protected override void OnTick()
            {
                var index = PageQueue.IndexOf(m_Entry);

                if (m_Entry.Sender.NetState != null && index != -1)
                {
                    // Thank you for paging. Queue status :
                    m_Entry.Sender.SendLocalizedMessage(1008077, true, (index + 1).ToString());
                    // You can reference our website at www.uo.com or contact us at support@uo.com. To cancel your page, please select the help button again and select cancel.
                    m_Entry.Sender.SendLocalizedMessage(1008084);

                    if (m_Entry.Handler != null && m_Entry.Handler.NetState == null)
                    {
                        m_Entry.Handler = null;
                    }
                }
                else
                {
                    if (index != -1)
                    {
                        PageQueue.Remove(m_Entry);
                    }
                }
            }
        }
    }

    public static class PageQueue
    {
        private static readonly Dictionary<Mobile, PageEntry> m_KeyedByHandler = new();
        private static readonly Dictionary<Mobile, PageEntry> m_KeyedBySender = new();

        public static List<PageEntry> List { get; } = new();

        public static void Initialize()
        {
            CommandSystem.Register("Pages", AccessLevel.Counselor, Pages_OnCommand);
        }

        public static bool CheckAllowedToPage(Mobile from)
        {
            if (from is not PlayerMobile pm)
            {
                return true;
            }

            if (pm.DesignContext != null)
            {
                // You cannot request help while customizing a house or transferring a character.
                from.SendLocalizedMessage(500182);
                return false;
            }

            if (pm.PagingSquelched)
            {
                from.SendMessage("You cannot request help, sorry.");
                return false;
            }

            return true;
        }

        public static string GetPageTypeName(PageType type)
        {
            return type switch
            {
                PageType.VerbalHarassment   => "Verbal Harassment",
                PageType.PhysicalHarassment => "Physical Harassment",
                _                           => type.ToString()
            };
        }

        public static void OnHandlerChanged(Mobile old, Mobile value, PageEntry entry)
        {
            if (old != null)
            {
                m_KeyedByHandler.Remove(old);
            }

            if (value != null)
            {
                m_KeyedByHandler[value] = entry;
            }
        }

        [Usage("Pages"), Description("Opens the page queue menu.")]
        private static void Pages_OnCommand(CommandEventArgs e)
        {
            if (m_KeyedByHandler.TryGetValue(e.Mobile, out var entry))
            {
                e.Mobile.SendGump(new PageEntryGump(e.Mobile, entry));
            }
            else if (List.Count > 0)
            {
                e.Mobile.SendGump(new PageQueueGump());
            }
            else
            {
                e.Mobile.SendMessage("The page queue is empty.");
            }
        }

        public static bool IsHandling(Mobile check) => m_KeyedByHandler.ContainsKey(check);

        public static bool Contains(Mobile sender) => m_KeyedBySender.ContainsKey(sender);

        public static int IndexOf(PageEntry e) => List.IndexOf(e);

        public static void Remove(PageEntry e)
        {
            if (e == null)
            {
                return;
            }

            e.Stop();

            List.Remove(e);
            m_KeyedBySender.Remove(e.Sender);

            if (e.Handler != null)
            {
                m_KeyedByHandler.Remove(e.Handler);
            }
        }

        public static PageEntry GetEntry(Mobile sender)
        {
            m_KeyedBySender.TryGetValue(sender, out var entry);
            return entry;
        }

        public static void Remove(Mobile sender)
        {
            Remove(GetEntry(sender));
        }

        public static void Enqueue(PageEntry entry)
        {
            List.Add(entry);
            m_KeyedBySender[entry.Sender] = entry;

            var isStaffOnline = false;

            foreach (var ns in TcpServer.Instances)
            {
                var m = ns.Mobile;

                if (m?.AccessLevel >= AccessLevel.Counselor && m.AutoPageNotify && !IsHandling(m))
                {
                    m.SendMessage("A new page has been placed in the queue.");
                }

                if (m?.AccessLevel >= AccessLevel.Counselor && m.AutoPageNotify && Core.TickCount - m.LastMoveTime < 600000)
                {
                    isStaffOnline = true;
                }
            }

            if (!isStaffOnline)
            {
                entry.Sender.SendMessage(
                    "We are sorry, but no staff members are currently available to assist you.  Your page will remain in the queue until one becomes available, or until you cancel it manually."
                );
            }

            if (entry.SpeechLog != null)
            {
                Email.SendQueueEmail(entry, GetPageTypeName(entry.Type));
            }
        }
    }
}
