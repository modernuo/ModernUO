using System;
using System.Collections.Generic;
using System.Linq;
using Server.Factions;
using Server.Network;
using Server.Targeting;

namespace Server.Engines.PartySystem
{
  public class Party : IParty
  {
    public const int Capacity = 10;

    public Party(Mobile leader)
    {
      Leader = leader;

      Members = new List<PartyMemberInfo>();
      Candidates = new List<Mobile>();
      Listeners = new List<Mobile>();

      Members.Add(new PartyMemberInfo(leader));
    }

    public int Count => Members.Count;
    public bool Active => Members.Count > 1;
    public Mobile Leader{ get; }

    public List<PartyMemberInfo> Members{ get; }

    public List<Mobile> Candidates{ get; }

    public List<Mobile> Listeners { get; } // staff listening

    public PartyMemberInfo this[int index] => Members[index];

    public PartyMemberInfo this[Mobile m] => Members.FirstOrDefault(t => t.Mobile == m);

    public void OnStamChanged(Mobile m)
    {
      for (int i = 0; i < Members.Count; ++i)
      {
        Mobile c = Members[i].Mobile;

        if (c != m && m.Map == c.Map && Utility.InUpdateRange(c, m) && c.CanSee(m))
          Packets.SendNormalizedMobileStam(c.NetState, c);
      }
    }

    public void OnManaChanged(Mobile m)
    {
      for (int i = 0; i < Members.Count; ++i)
      {
        Mobile c = Members[i].Mobile;

        if (c != m && m.Map == c.Map && Utility.InUpdateRange(c, m) && c.CanSee(m))
          Packets.SendNormalizedMobileMana(c.NetState, c);
      }
    }

    public void OnStatsQuery(Mobile beholder, Mobile beheld)
    {
      if (beholder != beheld && Contains(beholder) && beholder.Map == beheld.Map &&
          Utility.InUpdateRange(beholder, beheld))
      {
        if (!beholder.CanSee(beheld))
          Packets.SendMobileStatusCompact(beholder.NetState, beheld, beheld.CanBeRenamedBy(beholder));

        Packets.SendNormalizedMobileAttributes(beholder.NetState, beheld);
      }
    }

    public static void Initialize()
    {
      EventSink.Logout += EventSink_Logout;
      EventSink.Login += EventSink_Login;
      EventSink.PlayerDeath += EventSink_PlayerDeath;

      CommandSystem.Register("ListenToParty", AccessLevel.GameMaster, ListenToParty_OnCommand);
    }

    public static void ListenToParty_OnCommand(CommandEventArgs e)
    {
      e.Mobile.BeginTarget(-1, false, TargetFlags.None, ListenToParty_OnTarget);
      e.Mobile.SendMessage("Target a partied player.");
    }

    public static void ListenToParty_OnTarget(Mobile from, object obj)
    {
      if (obj is Mobile mobile)
      {
        Party p = Get(mobile);

        if (p == null)
        {
          from.SendMessage("They are not in a party.");
        }
        else if (p.Listeners.Contains(from))
        {
          p.Listeners.Remove(from);
          from.SendMessage("You are no longer listening to that party.");
        }
        else
        {
          p.Listeners.Add(from);
          from.SendMessage("You are now listening to that party.");
        }
      }
    }

    public static void EventSink_PlayerDeath(PlayerDeathEventArgs e)
    {
      Mobile from = e.Mobile;
      Party p = Get(from);

      if (p != null)
      {
        Mobile m = from.LastKiller;

        if (m == from)
          p.SendPublicMessage(from, "I killed myself !!");
        else if (m == null)
          p.SendPublicMessage(from, "I was killed !!");
        else
          p.SendPublicMessage(from, $"I was killed by {m.Name} !!");
      }
    }

    public static void EventSink_Login(LoginEventArgs e)
    {
      Mobile from = e.Mobile;
      Party p = Get(from);

      if (p != null)
        new RejoinTimer(from).Start();
      else
        from.Party = null;
    }

    public static void EventSink_Logout(LogoutEventArgs e)
    {
      Mobile from = e.Mobile;
      Party p = Get(from);

      p?.Remove(from);

      from.Party = null;
    }

    public static Party Get(Mobile m) => m?.Party as Party;

    public void Add(Mobile m)
    {
      PartyMemberInfo mi = this[m];

      if (mi == null)
      {
        Members.Add(new PartyMemberInfo(m));
        m.Party = this;

        for (int i = 0; i < Members.Count; ++i)
        {
          Mobile f = Members[i].Mobile;
          NetState ns = f.NetState;

          PartyPackets.SendPartyMemberList(ns, this);

          if (f != m)
          {
            Packets.SendMobileStatusCompact(ns, m, m.CanBeRenamedBy(f));
            Packets.SendNormalizedMobileAttributes(ns, m);
            Packets.SendMobileStatusCompact(ns, f, f.CanBeRenamedBy(m));
            Packets.SendNormalizedMobileAttributes(ns, f);
          }
        }
      }
    }

    public void OnAccept(Mobile from, bool force = false)
    {
      Faction ourFaction = Faction.Find(Leader);
      Faction theirFaction = Faction.Find(from);

      if (!force && ourFaction != null && theirFaction != null && ourFaction != theirFaction)
        return;

      //  : joined the party.
      PartyPackets.SendPartyMessageLocalizedAffixToAll(this, Serial.MinusOne, -1, MessageType.Label,
        0x3B2, 3, 1008094, "",AffixType.Prepend | AffixType.System, from.Name);

      from.SendLocalizedMessage(1005445); // You have been added to the party.

      Candidates.Remove(from);
      Add(from);
    }

    public void OnDecline(Mobile from, Mobile leader)
    {
      //  : Does not wish to join the party.
      leader.SendLocalizedMessage(1008091, false, from.Name);

      from.SendLocalizedMessage(1008092); // You notify them that you do not wish to join the party.

      Candidates.Remove(from);
      PartyPackets.SendPartyEmptyList(from.NetState, from);

      if (Candidates.Count == 0 && Members.Count <= 1)
      {
        for (int i = 0; i < Members.Count; ++i)
        {
          Mobile m = this[i].Mobile;
          PartyPackets.SendPartyEmptyList(m.NetState, m);
          m.Party = null;
        }

        Members.Clear();
      }
    }

    public void Remove(Mobile m)
    {
      if (m == Leader)
        Disband();
      else
      {
        for (int i = 0; i < Members.Count; ++i)
          if (Members[i].Mobile == m)
          {
            Members.RemoveAt(i);

            m.Party = null;
            PartyPackets.SendPartyEmptyList(m.NetState, m);

            m.SendLocalizedMessage(1005451); // You have been removed from the party.

            PartyPackets.SendPartyRemoveMemberToAll(m, this);
            // A player has been removed from your party.
            PartyPackets.SendPartyMessageLocalizedToAll(this, Serial.MinusOne, -1, MessageType.Regular, 0x3B2, 3, 1005452, "System");

            break;
          }

        if (Members.Count == 1)
        {
          // The last person has left the party...
          PartyPackets.SendPartyMessageLocalizedToAll(this, Serial.MinusOne, -1, MessageType.Regular, 0x3B2, 3, 1005450, "System");
          Disband();
        }
      }
    }

    public bool Contains(Mobile m) => this[m] != null;

    public void Disband()
    {
      // Your party has disbanded.
      PartyPackets.SendPartyMessageLocalizedToAll(this, Serial.MinusOne, -1, MessageType.Regular, 0x3B2, 3, 1005449, "System");

      for (int i = 0; i < Members.Count; ++i)
      {
        Mobile m = this[i].Mobile;
        PartyPackets.SendPartyEmptyList(m.NetState, m);
        m.Party = null;
      }

      Members.Clear();
    }

    public static void Invite(Mobile from, Mobile target)
    {
      Faction ourFaction = Faction.Find(from);
      Faction theirFaction = Faction.Find(target);

      if (ourFaction != null && theirFaction != null && ourFaction != theirFaction)
      {
        from.SendLocalizedMessage(1008088); // You cannot have players from opposing factions in the same party!
        target.SendLocalizedMessage(1008093); // The party cannot have members from opposing factions.
        return;
      }

      Party p = Get(from);

      if (p == null)
        from.Party = p = new Party(from);

      if (!p.Candidates.Contains(target))
        p.Candidates.Add(target);

      //  : You are invited to join the party. Type /accept to join or /decline to decline the offer.
      Packets.SendMessageLocalizedAffix(target.NetState, Serial.MinusOne, -1, MessageType.Label, 0x3B2, 3, 1008089, "",
        AffixType.Prepend | AffixType.System, from.Name);

      from.SendLocalizedMessage(1008090); // You have invited them to join the party.

      PartyPackets.SendPartyInvitation(target.NetState, from);
      target.Party = from;

      DeclineTimer.Start(target, from);
    }

    public void SendPublicMessage(Mobile from, string text)
    {
      PartyPackets.SendPartyTextMessageToAll(this, true, from, text);

      for (int i = 0; i < Listeners.Count; ++i)
      {
        Mobile mob = Listeners[i];

        if (mob.Party != this)
          Listeners[i].SendMessage("[{0}]: {1}", from.Name, text);
      }

      SendToStaffMessage(from, "[Party]: {0}", text);
    }

    public void SendPrivateMessage(Mobile from, Mobile to, string text)
    {
      PartyPackets.SendPartyTextMessage(to.NetState, false, from, text);

      for (int i = 0; i < Listeners.Count; ++i)
      {
        Mobile mob = Listeners[i];

        if (mob.Party != this)
          Listeners[i].SendMessage("[{0}]->[{1}]: {2}", from.Name, to.Name, text);
      }

      SendToStaffMessage(from, "[Party]->[{0}]: {1}", to.Name, text);
    }

    private void SendToStaffMessage(Mobile from, string text)
    {
      foreach (NetState ns in from.GetClientsInRange(8))
      {
        Mobile mob = ns.Mobile;

        if (mob?.AccessLevel >= AccessLevel.GameMaster && mob.AccessLevel > from.AccessLevel &&
            mob.Party != this && !Listeners.Contains(mob))
          Packets.SendUnicodeMessage(ns, from.Serial, from.Body, MessageType.Regular, from.SpeechHue, 3, from.Language, from.Name, text);
      }
    }

    private void SendToStaffMessage(Mobile from, string format, params object[] args)
    {
      SendToStaffMessage(from, string.Format(format, args));
    }

    private class RejoinTimer : Timer
    {
      private Mobile m_Mobile;

      public RejoinTimer(Mobile m) : base(TimeSpan.FromSeconds(1.0)) => m_Mobile = m;

      protected override void OnTick()
      {
        Party p = Get(m_Mobile);

        if (p == null)
          return;

        m_Mobile.SendLocalizedMessage(1005437); // You have rejoined the party.
        PartyPackets.SendPartyMemberList(m_Mobile.NetState, p);

        foreach (PartyMemberInfo mi in p.Members)
        {
          Mobile m = mi.Mobile;

          if (m != m_Mobile)
          {
            NetState ns = m.NetState;
            Packets.SendMessageLocalizedAffix(ns, Serial.MinusOne, -1, MessageType.Label, 0x3B2, 3,
              1008087, "", AffixType.Prepend | AffixType.System, m_Mobile.Name);
            Packets.SendMobileStatusCompact(ns, m_Mobile, m_Mobile.CanBeRenamedBy(m));
            Packets.SendNormalizedMobileAttributes(ns, m_Mobile);
            Packets.SendMobileStatusCompact(ns, m, m.CanBeRenamedBy(m_Mobile));
            Packets.SendNormalizedMobileAttributes(ns, m);
          }
        }
      }
    }
  }
}
