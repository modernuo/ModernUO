using System.Collections;
using Server.Gumps;

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
    private ArrayList m_Responses, m_Failures;

    public BaseCommand()
    {
      m_Responses = new ArrayList();
      m_Failures = new ArrayList();
    }

    public bool ListOptimized{ get; set; }

    public string[] Commands{ get; set; }

    public string Usage{ get; set; }

    public string Description{ get; set; }

    public AccessLevel AccessLevel{ get; set; }

    public ObjectTypes ObjectTypes{ get; set; }

    public CommandSupport Supports{ get; set; }

    public static bool IsAccessible(Mobile from, object obj)
    {
      if (from.AccessLevel >= AccessLevel.Administrator || obj == null)
        return true;

      Mobile mob = null;

      if (obj is Mobile m)
        mob = m;
      else if (obj is Item item)
        mob = item.RootParent as Mobile;

      return mob == null || mob == from || from.AccessLevel > mob.AccessLevel;
    }

    public virtual void ExecuteList(CommandEventArgs e, ArrayList list)
    {
      for (int i = 0; i < list.Count; ++i)
        Execute(e, list[i]);
    }

    public virtual void Execute(CommandEventArgs e, object obj)
    {
    }

    public virtual bool ValidateArgs(BaseCommandImplementor impl, CommandEventArgs e)
    {
      return true;
    }

    public void AddResponse(string message)
    {
      for (int i = 0; i < m_Responses.Count; ++i)
      {
        MessageEntry entry = (MessageEntry)m_Responses[i];

        if (entry.m_Message == message)
        {
          ++entry.m_Count;
          return;
        }
      }

      if (m_Responses.Count == 10)
        return;

      m_Responses.Add(new MessageEntry(message));
    }

    public void AddResponse(Gump gump)
    {
      m_Responses.Add(gump);
    }

    public void LogFailure(string message)
    {
      for (int i = 0; i < m_Failures.Count; ++i)
      {
        MessageEntry entry = (MessageEntry)m_Failures[i];

        if (entry.m_Message == message)
        {
          ++entry.m_Count;
          return;
        }
      }

      if (m_Failures.Count == 10)
        return;

      m_Failures.Add(new MessageEntry(message));
    }

    public void Flush(Mobile from, bool flushToLog)
    {
      if (m_Responses.Count > 0)
        for (int i = 0; i < m_Responses.Count; ++i)
        {
          object obj = m_Responses[i];

          if (obj is MessageEntry entry)
          {
            from.SendMessage(entry.ToString());

            if (flushToLog)
              CommandLogging.WriteLine(from, entry.ToString());
          }
          else if (obj is Gump gump)
          {
            from.SendGump(gump);
          }
        }
      else
        for (int i = 0; i < m_Failures.Count; ++i)
          from.SendMessage(((MessageEntry)m_Failures[i]).ToString());

      m_Responses.Clear();
      m_Failures.Clear();
    }

    private class MessageEntry
    {
      public int m_Count;
      public string m_Message;

      public MessageEntry(string message)
      {
        m_Message = message;
        m_Count = 1;
      }

      public override string ToString()
      {
        if (m_Count > 1)
          return $"{m_Message} ({m_Count})";

        return m_Message;
      }
    }
  }
}