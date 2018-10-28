using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Engines.PartySystem
{
  public class DeclineTimer : Timer
  {
    private static Dictionary<Mobile, DeclineTimer> m_Table = new Dictionary<Mobile, DeclineTimer>();
    private Mobile m_Mobile, m_Leader;

    private DeclineTimer(Mobile m, Mobile leader) : base(TimeSpan.FromSeconds(30.0))
    {
      m_Mobile = m;
      m_Leader = leader;
    }

    public static void Start(Mobile m, Mobile leader)
    {
      DeclineTimer t = m_Table[m];

      t?.Stop();

      m_Table[m] = t = new DeclineTimer(m, leader);
      t.Start();
    }

    protected override void OnTick()
    {
      m_Table.Remove(m_Mobile);

      if (m_Mobile.Party == m_Leader && PartyCommands.Handler != null)
        PartyCommands.Handler.OnDecline(m_Mobile, m_Leader);
    }
  }
}
