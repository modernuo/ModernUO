using System.Collections.Generic;

namespace Server.Commands.Generic
{
    public enum ObjectTypes
    {
        Both,
        Items,
        Mobiles,
        All
    }

    public abstract class BaseCommand
    {
        private readonly List<MessageEntry> m_Failures = new();
        private readonly List<MessageEntry> m_Responses = new();

        public bool ListOptimized { get; set; }

        public string[] Commands { get; set; }

        public string Usage { get; set; }

        public string Description { get; set; }

        public AccessLevel AccessLevel { get; set; }

        public ObjectTypes ObjectTypes { get; set; }

        public CommandSupport Supports { get; set; }

        public static bool IsAccessible(Mobile from, object obj)
        {
            if (from.AccessLevel >= AccessLevel.Administrator || obj == null)
            {
                return true;
            }

            Mobile mob = obj switch
            {
                Mobile m  => m,
                Item item => item.RootParent as Mobile,
                _         => null
            };

            return mob == null || mob == from || from.AccessLevel > mob.AccessLevel;
        }

        public virtual void ExecuteList(CommandEventArgs e, List<object> list)
        {
            for (var i = 0; i < list.Count; ++i)
            {
                Execute(e, list[i]);
            }
        }

        public virtual void Execute(CommandEventArgs e, object obj)
        {
        }

        public virtual bool ValidateArgs(BaseCommandImplementor impl, CommandEventArgs e) => true;

        public void AddResponse(string message)
        {
            for (var i = 0; i < m_Responses.Count; ++i)
            {
                var entry = m_Responses[i];

                if (entry.m_Message == message)
                {
                    ++entry.m_Count;
                    return;
                }
            }

            if (m_Responses.Count == 10)
            {
                return;
            }

            m_Responses.Add(new MessageEntry(message));
        }

        public void LogFailure(string message)
        {
            for (var i = 0; i < m_Failures.Count; ++i)
            {
                var entry = m_Failures[i];

                if (entry.m_Message == message)
                {
                    ++entry.m_Count;
                    return;
                }
            }

            if (m_Failures.Count == 10)
            {
                return;
            }

            m_Failures.Add(new MessageEntry(message));
        }

        public void Flush(Mobile from, bool flushToLog)
        {
            if (m_Responses.Count > 0)
            {
                for (var i = 0; i < m_Responses.Count; ++i)
                {
                    var entry = m_Responses[i];

                    from.SendMessage(entry.ToString());

                    if (flushToLog)
                    {
                        CommandLogging.WriteLine(from, entry.ToString());
                    }
                }
            }
            else
            {
                for (var i = 0; i < m_Failures.Count; ++i)
                {
                    from.SendMessage(m_Failures[i].ToString());
                }
            }

            m_Responses.Clear();
            m_Failures.Clear();
        }

        private class MessageEntry
        {
            public readonly string m_Message;
            public int m_Count;

            public MessageEntry(string message)
            {
                m_Message = message;
                m_Count = 1;
            }

            public override string ToString() => m_Count > 1 ? $"{m_Message} ({m_Count})" : m_Message;
        }
    }
}
