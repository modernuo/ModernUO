using System;
using System.Collections;
using System.Collections.Generic;
using Server.Commands;
using Server.Gumps;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Engines.Help
{
    public class SpeechLog : IEnumerable<SpeechLogEntry>
    {
        // Are speech logs enabled?
        public static readonly bool Enabled = true;

        // How long should we maintain each speech entry?
        public static readonly TimeSpan EntryDuration = TimeSpan.FromMinutes(20.0);

        // What is the maximum number of entries a log can contain? (0 -> no limit)
        public static readonly int MaxLength = 0;

        private readonly Queue<SpeechLogEntry> m_Queue;

        public SpeechLog() => m_Queue = new Queue<SpeechLogEntry>();

        public int Count => m_Queue.Count;

        IEnumerator<SpeechLogEntry> IEnumerable<SpeechLogEntry>.GetEnumerator() => m_Queue.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => m_Queue.GetEnumerator();

        public static void Initialize()
        {
            CommandSystem.Register("SpeechLog", AccessLevel.Counselor, SpeechLog_OnCommand);
        }

        [Usage("SpeechLog"), Description("Opens the speech log of a given target.")]
        private static void SpeechLog_OnCommand(CommandEventArgs e)
        {
            var from = e.Mobile;

            from.SendMessage("Target a player to view his speech log.");
            e.Mobile.Target = new SpeechLogTarget();
        }

        public void Add(Mobile from, string speech)
        {
            Add(new SpeechLogEntry(from, speech));
        }

        public void Add(SpeechLogEntry entry)
        {
            if (MaxLength > 0 && m_Queue.Count >= MaxLength)
            {
                m_Queue.Dequeue();
            }

            Clean();

            m_Queue.Enqueue(entry);
        }

        public void Clean()
        {
            while (m_Queue.Count > 0)
            {
                var entry = m_Queue.Peek();

                if (Core.Now - entry.Created > EntryDuration)
                {
                    m_Queue.Dequeue();
                }
                else
                {
                    break;
                }
            }
        }

        public void CopyTo(SpeechLogEntry[] array, int index)
        {
            m_Queue.CopyTo(array, index);
        }

        private class SpeechLogTarget : Target
        {
            public SpeechLogTarget() : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is not PlayerMobile pm)
                {
                    from.SendMessage("Speech logs aren't supported on that target.");
                }
                else if (from != targeted && from.AccessLevel <= pm.AccessLevel && from.AccessLevel != AccessLevel.Owner)
                {
                    if (pm.Female)
                    {
                        from.SendMessage($"You don't have the required access level to view her speech log.");
                    }
                    else
                    {
                        from.SendMessage($"You don't have the required access level to view his speech log.");
                    }
                }
                else if (pm.SpeechLog == null)
                {
                    if (pm.Female)
                    {
                        from.SendMessage($"She has no speech log.");
                    }
                    else
                    {
                        from.SendMessage($"He has no speech log.");
                    }
                }
                else
                {
                    CommandLogging.WriteLine(
                        from,
                        $"{from.AccessLevel} {CommandLogging.Format(from)} viewing speech log of {CommandLogging.Format(targeted)}"
                    );

                    Gump gump = new SpeechLogGump(pm, pm.SpeechLog);
                    from.SendGump(gump);
                }
            }
        }
    }

    public class SpeechLogEntry
    {
        public SpeechLogEntry(Mobile from, string speech)
        {
            From = from;
            Speech = speech;
            Created = Core.Now;
        }

        public Mobile From { get; }

        public string Speech { get; }

        public DateTime Created { get; }
    }
}
