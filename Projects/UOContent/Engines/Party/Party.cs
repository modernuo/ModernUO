using System;
using System.Collections.Generic;
using Server.Factions;
using Server.Network;
using Server.Targeting;

namespace Server.Engines.PartySystem
{
    public class Party : IParty
    {
        public const int Capacity = 10;
        private readonly HashSet<Mobile> m_Listeners; // staff listening

        public Party(Mobile leader)
        {
            Leader = leader;

            Members = new List<PartyMemberInfo>();
            Candidates = new List<Mobile>();
            m_Listeners = new HashSet<Mobile>();

            Members.Add(new PartyMemberInfo(leader));
        }

        public int Count => Members.Count;
        public bool Active => Members.Count > 1;
        public Mobile Leader { get; }

        public List<PartyMemberInfo> Members { get; }

        public List<Mobile> Candidates { get; }

        public PartyMemberInfo this[int index] => Members[index];

        public PartyMemberInfo this[Mobile m]
        {
            get
            {
                for (var i = 0; i < Members.Count; ++i)
                {
                    if (Members[i].Mobile == m)
                    {
                        return Members[i];
                    }
                }

                return null;
            }
        }

        public void OnStamChanged(Mobile m)
        {
            Span<byte> p = stackalloc byte[OutgoingMobilePackets.MobileAttributePacketLength];
            OutgoingMobilePackets.CreateMobileStam(p, m, true);

            for (var i = 0; i < Members.Count; ++i)
            {
                var c = Members[i].Mobile;
                var ns = c.NetState;

                if (c != m && ns != null && m.Map == c.Map && Utility.InUpdateRange(c.Location, m.Location) && c.CanSee(m))
                {
                    ns.Send(p);
                }
            }
        }

        public void OnManaChanged(Mobile m)
        {
            Span<byte> p = stackalloc byte[OutgoingMobilePackets.MobileAttributePacketLength];
            OutgoingMobilePackets.CreateMobileMana(p, m, true);

            for (var i = 0; i < Members.Count; ++i)
            {
                var c = Members[i].Mobile;
                var ns = c.NetState;

                if (c != m && ns != null && m.Map == c.Map && Utility.InUpdateRange(c.Location, m.Location) && c.CanSee(m))
                {
                    ns.Send(p);
                }
            }
        }

        public void OnStatsQuery(Mobile beholder, Mobile beheld)
        {
            if (beholder != beheld && Contains(beholder) && beholder.Map == beheld.Map &&
                Utility.InUpdateRange(beholder.Location, beheld.Location))
            {
                if (!beholder.CanSee(beheld))
                {
                    beholder.NetState.SendMobileStatusCompact(beheld, beheld.CanBeRenamedBy(beholder));
                }

                beholder.NetState.SendMobileAttributes(beheld, true);
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
                var p = Get(mobile);

                if (p == null)
                {
                    from.SendMessage("They are not in a party.");
                }
                else if (p.m_Listeners.Contains(from))
                {
                    p.m_Listeners.Remove(from);
                    from.SendMessage("You are no longer listening to that party.");
                }
                else
                {
                    p.m_Listeners.Add(from);
                    from.SendMessage("You are now listening to that party.");
                }
            }
        }

        public static void EventSink_PlayerDeath(Mobile from)
        {
            var p = Get(from);

            if (p != null)
            {
                var m = from.LastKiller;

                if (m == from)
                {
                    p.SendPublicMessage(from, "I killed myself !!");
                }
                else if (m == null)
                {
                    p.SendPublicMessage(from, "I was killed !!");
                }
                else
                {
                    p.SendPublicMessage(from, $"I was killed by {m.Name} !!");
                }
            }
        }

        public static void EventSink_Login(Mobile from)
        {
            var p = Get(from);

            if (p != null)
            {
                new RejoinTimer(from).Start();
            }
            else
            {
                from.Party = null;
            }
        }

        public static void EventSink_Logout(Mobile from)
        {
            var p = Get(from);

            p?.Remove(from);

            from.Party = null;
        }

        public static Party Get(Mobile m) => m?.Party as Party;

        public void Add(Mobile m)
        {
            var mi = this[m];
            if (mi != null)
            {
                return;
            }

            var ns = m.NetState;
            Members.Add(new PartyMemberInfo(m));
            m.Party = this;

            Span<byte> memberList =
                stackalloc byte[PartyPackets.GetPartyMemberListPacketLength(Count)].InitializePacket();
            Span<byte> attrsPacket = stackalloc byte[OutgoingMobilePackets.MobileAttributesPacketLength].InitializePacket();

            for (var i = 0; i < Members.Count; ++i)
            {
                var f = Members[i].Mobile;

                PartyPackets.CreatePartyMemberList(memberList, this);
                f.NetState?.Send(memberList);

                if (f != m)
                {
                    f.NetState.SendMobileStatusCompact(m, m.CanBeRenamedBy(f));
                    OutgoingMobilePackets.CreateMobileAttributes(attrsPacket, m, true);
                    f.NetState?.Send(attrsPacket);
                    ns.SendMobileStatusCompact(f, f.CanBeRenamedBy(m));
                    ns.SendMobileAttributes(f, true);
                }
            }
        }

        public void OnAccept(Mobile from, bool force = false)
        {
            var ourFaction = Faction.Find(Leader);
            var theirFaction = Faction.Find(from);

            if (!force && ourFaction != null && theirFaction != null && ourFaction != theirFaction)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLocalizedAffixLength(from.Name, "")];
            var length = OutgoingMessagePackets.CreateMessageLocalizedAffix(
                buffer,
                Serial.MinusOne,
                -1,
                MessageType.Label,
                0x3B2,
                3,
                1008094,
                "",
                AffixType.Prepend | AffixType.System,
                from.Name
            );

            // : joined the party.
            buffer = buffer[..length];
            SendToAll(buffer);
            SendToAllListeners(buffer);

            from.SendLocalizedMessage(1005445); // You have been added to the party.

            Candidates.Remove(from);
            Add(from);
        }

        public void OnDecline(Mobile from, Mobile leader)
        {
            // : Does not wish to join the party.
            leader.SendLocalizedMessage(1008091, false, from.Name);

            from.SendLocalizedMessage(1008092); // You notify them that you do not wish to join the party.

            Candidates.Remove(from);
            from.NetState.SendPartyRemoveMember(from.Serial);

            if (Candidates.Count != 0 || Members.Count > 1)
            {
                return;
            }

            for (var i = 0; i < Members.Count; ++i)
            {
                var m = this[i].Mobile;
                m.NetState.SendPartyRemoveMember(m.Serial);
                m.Party = null;
            }

            Members.Clear();
        }

        public void Remove(Mobile m)
        {
            if (m == Leader)
            {
                Disband();
                return;
            }

            var removed = false;
            for (var i = 0; i < Members.Count; i++)
            {
                if (Members[i].Mobile == m)
                {
                    Members.RemoveAt(i);
                    removed = true;
                    break;
                }
            }

            if (removed)
            {
                Span<byte> removeMember =
                    stackalloc byte[PartyPackets.GetPartyRemoveMemberPacketLength(Count)].InitializePacket();

                // Send empty party to the player removed
                m.NetState.SendPartyRemoveMember(m.Serial);
                m.Party = null;

                m.SendLocalizedMessage(1005451); // You have been removed from the party.

                PartyPackets.CreatePartyRemoveMember(removeMember, m.Serial, this);
                SendToAll(removeMember);
                SendToAll(1005452); // A player has been removed from your party.
            }

            if (Members.Count == 1)
            {
                SendToAll(1005450); // The last person has left the party...
                Disband();
            }
        }

        public bool Contains(Mobile m) => this[m] != null;

        public void Disband()
        {
            SendToAll(1005449); // Your party has disbanded.

            for (var i = 0; i < Members.Count; ++i)
            {
                var m = this[i].Mobile;
                m.NetState.SendPartyRemoveMember(m.Serial);
                m.Party = null;
            }

            Members.Clear();
        }

        public static void Invite(Mobile from, Mobile target)
        {
            var ourFaction = Faction.Find(from);
            var theirFaction = Faction.Find(target);

            if (ourFaction != null && theirFaction != null && ourFaction != theirFaction)
            {
                from.SendLocalizedMessage(1008088);   // You cannot have players from opposing factions in the same party!
                target.SendLocalizedMessage(1008093); // The party cannot have members from opposing factions.
                return;
            }

            var p = Get(from);

            if (p == null)
            {
                from.Party = p = new Party(from);
            }

            if (!p.Candidates.Contains(target))
            {
                p.Candidates.Add(target);
            }

            // : You are invited to join the party. Type /accept to join or /decline to decline the offer.
            target.NetState.SendMessageLocalizedAffix(
                Serial.MinusOne,
                -1,
                MessageType.Label,
                0x3B2,
                3,
                1008089,
                "",
                AffixType.Prepend | AffixType.System,
                from.Name
            );

            from.SendLocalizedMessage(1008090); // You have invited them to join the party.

            target.NetState.SendPartyInvitation(from.Serial);
            target.Party = from;

            DeclineTimer.Start(target, from);
        }

        public void SendToAll(int number, string args = "", int hue = 0x3B2)
        {
            Span<byte> buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLocalizedLength(args)].InitializePacket();

            var length = OutgoingMessagePackets.CreateMessageLocalized(
                buffer,
                Serial.MinusOne, -1, MessageType.Regular, hue, 3, number, "System", args
            );

            buffer = buffer[..length];

            SendToAll(buffer);
            SendToAllListeners(buffer);
        }

        public void SendPublicMessage(Mobile from, string text)
        {
            Span<byte> textMessagePacket =
                stackalloc byte[PartyPackets.GetPartyTextMessagePacketLength(text)].InitializePacket();
            PartyPackets.CreatePartyTextMessage(textMessagePacket, from.Serial, text, true);
            SendToAll(textMessagePacket);

            SendToAllListeners($"[{from.Name}]: {text}");
            SendToStaffMessage(from, $"[Party]: {text}");
        }

        public void SendPrivateMessage(Mobile from, Mobile to, string text)
        {
            to.NetState.SendPartyTextMessage(from.Serial, text, false);
            SendToAllListeners($"[{from.Name}]->[{to.Name}]: {text}");
            SendToStaffMessage(from, $"[Party]->[{to.Name}]: {text}");
        }

        private void SendToStaffMessage(Mobile from, string text)
        {
            Span<byte> buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLength(text)].InitializePacket();

            foreach (var ns in from.GetClientsInRange(8))
            {
                var mob = ns.Mobile;

                if (mob?.AccessLevel >= AccessLevel.GameMaster && mob.AccessLevel > from.AccessLevel &&
                    mob.Party != this && !m_Listeners.Contains(mob))
                {
                    var length = OutgoingMessagePackets.CreateMessage(
                        buffer,
                        from.Serial,
                        from.Body,
                        MessageType.Regular,
                        from.SpeechHue,
                        3,
                        false,
                        from.Language,
                        from.Name,
                        text
                    );

                    if (length != buffer.Length)
                    {
                        buffer = buffer[..length]; // Adjust to the actual size
                    }

                    ns.Send(buffer);
                }
            }
        }

        public void SendToAllListeners(string text)
        {
            if (m_Listeners.Count == 0)
            {
                return;
            }

            Span<byte> buffer =
                stackalloc byte[OutgoingMessagePackets.GetMaxMessageLength(text)].InitializePacket();
            OutgoingMessagePackets.CreateMessage(
                buffer, Serial.MinusOne, -1, MessageType.Regular, 0x3B2,
                3, false, "ENU", "System", text
            );
            SendToAllListeners(buffer);
        }

        public void SendToAllListeners(Span<byte> span)
        {
            foreach (var mob in m_Listeners)
            {
                if (mob.Party != this)
                {
                    mob.NetState?.Send(span);
                }
            }
        }

        public void SendToAll(Span<byte> span)
        {
            for (var i = 0; i < Members.Count; ++i)
            {
                Members[i].Mobile.NetState?.Send(span);
            }
        }

        private class RejoinTimer : Timer
        {
            private readonly Mobile m_Mobile;

            public RejoinTimer(Mobile m) : base(TimeSpan.FromSeconds(1.0)) => m_Mobile = m;

            protected override void OnTick()
            {
                var p = Get(m_Mobile);

                if (p == null)
                {
                    return;
                }

                m_Mobile.SendLocalizedMessage(1005437); // You have rejoined the party.
                m_Mobile.NetState.SendPartyMemberList(p);

                Span<byte> buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLocalizedAffixLength(m_Mobile.Name, "")].InitializePacket();
                Span<byte> attrsPacket = stackalloc byte[OutgoingMobilePackets.MobileAttributesPacketLength].InitializePacket();

                var ns = m_Mobile.NetState;

                foreach (var mi in p.Members)
                {
                    var m = mi.Mobile;

                    if (m == m_Mobile)
                    {
                        continue;
                    }

                    var length = OutgoingMessagePackets.CreateMessageLocalizedAffix(
                        buffer,
                        Serial.MinusOne,
                        -1,
                        MessageType.Label,
                        0x3B2,
                        3,
                        1008087,
                        "",
                        AffixType.Prepend | AffixType.System,
                        m_Mobile.Name
                    );

                    if (length != buffer.Length)
                    {
                        buffer = buffer[..length]; // Adjust to the actual size
                    }

                    m.NetState?.Send(buffer);
                    m.NetState.SendMobileStatusCompact(m_Mobile, m_Mobile.CanBeRenamedBy(m));
                    OutgoingMobilePackets.CreateMobileAttributes(attrsPacket, m_Mobile, true);
                    m.NetState?.Send(attrsPacket);
                    ns.SendMobileStatusCompact(m, m.CanBeRenamedBy(m_Mobile));
                    ns.SendMobileAttributes(m, true);
                }
            }
        }
    }
}
