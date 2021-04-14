using System;
using System.Collections.Generic;
using System.Net;
using Server.Mobiles;

namespace Server.Factions
{
    public class Election
    {
        public const int MaxCandidates = 10;
        public const int CandidateRank = 5;
        public static readonly TimeSpan PendingPeriod = TimeSpan.FromDays(5.0);
        public static readonly TimeSpan CampaignPeriod = TimeSpan.FromDays(1.0);
        public static readonly TimeSpan VotingPeriod = TimeSpan.FromDays(3.0);

        private Timer m_Timer;

        public Election(Faction faction)
        {
            Faction = faction;
            Candidates = new List<Candidate>();

            StartTimer();
        }

        public Election(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        Faction = Faction.ReadReference(reader);

                        LastStateTime = reader.ReadDateTime();
                        CurrentState = (ElectionState)reader.ReadEncodedInt();

                        Candidates = new List<Candidate>();

                        var count = reader.ReadEncodedInt();

                        for (var i = 0; i < count; ++i)
                        {
                            var cd = new Candidate(reader);

                            if (cd.Mobile != null)
                            {
                                Candidates.Add(cd);
                            }
                        }

                        break;
                    }
            }

            StartTimer();
        }

        public Faction Faction { get; }

        public List<Candidate> Candidates { get; }

        public ElectionState State
        {
            get => CurrentState;
            set
            {
                CurrentState = value;
                LastStateTime = Core.Now;
            }
        }

        public DateTime LastStateTime { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public ElectionState CurrentState { get; private set; }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan NextStateTime
        {
            get
            {
                var period = CurrentState switch
                {
                    ElectionState.Pending  => PendingPeriod,
                    ElectionState.Election => VotingPeriod,
                    ElectionState.Campaign => CampaignPeriod,
                    _                      => PendingPeriod
                };

                var until = Utility.Max(LastStateTime + period - Core.Now, TimeSpan.Zero);

                return until;
            }
            set
            {
                var period = CurrentState switch
                {
                    ElectionState.Pending  => PendingPeriod,
                    ElectionState.Election => VotingPeriod,
                    ElectionState.Campaign => CampaignPeriod,
                    _                      => PendingPeriod
                };

                LastStateTime = Core.Now - period + value;
            }
        }

        public void StartTimer()
        {
            m_Timer = Timer.DelayCall(TimeSpan.FromMinutes(1.0), TimeSpan.FromMinutes(1.0), Slice);
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            Faction.WriteReference(writer, Faction);

            writer.Write(LastStateTime);
            writer.WriteEncodedInt((int)CurrentState);

            writer.WriteEncodedInt(Candidates.Count);

            for (var i = 0; i < Candidates.Count; ++i)
            {
                Candidates[i].Serialize(writer);
            }
        }

        public void AddCandidate(Mobile mob)
        {
            if (IsCandidate(mob))
            {
                return;
            }

            Candidates.Add(new Candidate(mob));
            mob.SendLocalizedMessage(1010117); // You are now running for office.
        }

        public void RemoveVoter(Mobile mob)
        {
            if (CurrentState == ElectionState.Election)
            {
                for (var i = 0; i < Candidates.Count; ++i)
                {
                    var voters = Candidates[i].Voters;

                    for (var j = 0; j < voters.Count; ++j)
                    {
                        var voter = voters[j];

                        if (voter.From == mob)
                        {
                            voters.RemoveAt(j--);
                        }
                    }
                }
            }
        }

        public void RemoveCandidate(Mobile mob)
        {
            var cd = FindCandidate(mob);

            if (cd == null)
            {
                return;
            }

            Candidates.Remove(cd);
            mob.SendLocalizedMessage(1038031);

            if (CurrentState == ElectionState.Election)
            {
                if (Candidates.Count == 1)
                {
                    // There are no longer any valid candidates in the Faction Commander election.
                    Faction.Broadcast(1038031);

                    var winner = Candidates[0];

                    var winMob = winner.Mobile;
                    var pl = PlayerState.Find(winMob);

                    if (pl == null || pl.Faction != Faction || winMob == Faction.Commander)
                    {
                        Faction.Broadcast(1038026); // Faction leadership has not changed.
                    }
                    else
                    {
                        Faction.Broadcast(1038028); // The faction has a new commander.
                        Faction.Commander = winMob;
                    }

                    Candidates.Clear();
                    State = ElectionState.Pending;
                }
                else if (Candidates.Count == 0) // well, I guess this'll never happen
                {
                    // There are no longer any valid candidates in the Faction Commander election.
                    Faction.Broadcast(1038031);

                    Candidates.Clear();
                    State = ElectionState.Pending;
                }
            }
        }

        public bool IsCandidate(Mobile mob) => FindCandidate(mob) != null;

        public bool CanVote(Mobile mob) => CurrentState == ElectionState.Election && !HasVoted(mob);

        public bool HasVoted(Mobile mob) => FindVoter(mob) != null;

        public Candidate FindCandidate(Mobile mob)
        {
            for (var i = 0; i < Candidates.Count; ++i)
            {
                if (Candidates[i].Mobile == mob)
                {
                    return Candidates[i];
                }
            }

            return null;
        }

        public Candidate FindVoter(Mobile mob)
        {
            for (var i = 0; i < Candidates.Count; ++i)
            {
                var voters = Candidates[i].Voters;

                for (var j = 0; j < voters.Count; ++j)
                {
                    var voter = voters[j];

                    if (voter.From == mob)
                    {
                        return Candidates[i];
                    }
                }
            }

            return null;
        }

        public bool CanBeCandidate(Mobile mob)
        {
            if (IsCandidate(mob))
            {
                return false;
            }

            if (Candidates.Count >= MaxCandidates)
            {
                return false;
            }

            if (CurrentState != ElectionState.Campaign)
            {
                return false; // sanity..
            }

            var pl = PlayerState.Find(mob);

            return pl != null && pl.Faction == Faction && pl.Rank.Rank >= CandidateRank;
        }

        public void Slice()
        {
            if (Faction.Election != this)
            {
                m_Timer?.Stop();

                m_Timer = null;

                return;
            }

            switch (CurrentState)
            {
                case ElectionState.Pending:
                    {
                        if (LastStateTime + PendingPeriod > Core.Now)
                        {
                            break;
                        }

                        Faction.Broadcast(1038023); // Campaigning for the Faction Commander election has begun.

                        Candidates.Clear();
                        State = ElectionState.Campaign;

                        break;
                    }
                case ElectionState.Campaign:
                    {
                        if (LastStateTime + CampaignPeriod > Core.Now)
                        {
                            break;
                        }

                        if (Candidates.Count == 0)
                        {
                            Faction.Broadcast(1038025); // Nobody ran for office.
                            State = ElectionState.Pending;
                        }
                        else if (Candidates.Count == 1)
                        {
                            Faction.Broadcast(1038029); // Only one member ran for office.

                            var winner = Candidates[0];

                            var mob = winner.Mobile;
                            var pl = PlayerState.Find(mob);

                            if (pl == null || pl.Faction != Faction || mob == Faction.Commander)
                            {
                                Faction.Broadcast(1038026); // Faction leadership has not changed.
                            }
                            else
                            {
                                Faction.Broadcast(1038028); // The faction has a new commander.
                                Faction.Commander = mob;
                            }

                            Candidates.Clear();
                            State = ElectionState.Pending;
                        }
                        else
                        {
                            Faction.Broadcast(1038030);
                            State = ElectionState.Election;
                        }

                        break;
                    }
                case ElectionState.Election:
                    {
                        if (LastStateTime + VotingPeriod > Core.Now)
                        {
                            break;
                        }

                        Faction.Broadcast(1038024); // The results for the Faction Commander election are in

                        Candidate winner = null;

                        for (var i = 0; i < Candidates.Count; ++i)
                        {
                            var cd = Candidates[i];

                            var pl = PlayerState.Find(cd.Mobile);

                            if (pl == null || pl.Faction != Faction)
                            {
                                continue;
                            }

                            // cd.CleanMuleVotes();

                            if (winner == null || cd.Votes > winner.Votes)
                            {
                                winner = cd;
                            }
                        }

                        if (winner == null)
                        {
                            Faction.Broadcast(1038026); // Faction leadership has not changed.
                        }
                        else if (winner.Mobile == Faction.Commander)
                        {
                            Faction.Broadcast(1038027); // The incumbent won the election.
                        }
                        else
                        {
                            Faction.Broadcast(1038028); // The faction has a new commander.
                            Faction.Commander = winner.Mobile;
                        }

                        Candidates.Clear();
                        State = ElectionState.Pending;

                        break;
                    }
            }
        }
    }

    public class Voter
    {
        public Voter(Mobile from, Mobile candidate)
        {
            From = from;
            Candidate = candidate;

            if (From.NetState != null)
            {
                Address = From.NetState.Address;
            }
            else
            {
                Address = IPAddress.None;
            }

            Time = Core.Now;
        }

        public Voter(IGenericReader reader, Mobile candidate)
        {
            Candidate = candidate;

            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        From = reader.ReadEntity<Mobile>();
                        Address = Utility.Intern(reader.ReadIPAddress());
                        Time = reader.ReadDateTime();

                        break;
                    }
            }
        }

        public Mobile From { get; }

        public Mobile Candidate { get; }

        public IPAddress Address { get; }

        public DateTime Time { get; }

        public object[] AcquireFields()
        {
            var gameTime = TimeSpan.Zero;

            if (From is PlayerMobile mobile)
            {
                gameTime = mobile.GameTime;
            }

            var kp = 0;

            var pl = PlayerState.Find(From);

            if (pl != null)
            {
                kp = pl.KillPoints;
            }

            var sk = From.Skills.Total;

            var factorSkills = 50 + sk * 100 / 10000;
            var factorKillPts = 100 + kp * 2;
            var factorGameTime = 50 + (int)(gameTime.Ticks * 100 / TimeSpan.TicksPerDay);

            var totalFactor = Math.Clamp(factorSkills * factorKillPts * Math.Max(factorGameTime, 100) / 10000, 0, 100);

            return new object[] { From, Address, Time, totalFactor };
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0);

            writer.Write(From);
            writer.Write(Address);
            writer.Write(Time);
        }
    }

    public class Candidate
    {
        public Candidate(Mobile mob)
        {
            Mobile = mob;
            Voters = new List<Voter>();
        }

        public Candidate(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 1:
                    {
                        Mobile = reader.ReadEntity<Mobile>();

                        var count = reader.ReadEncodedInt();
                        Voters = new List<Voter>(count);

                        for (var i = 0; i < count; ++i)
                        {
                            var voter = new Voter(reader, Mobile);

                            if (voter.From != null)
                            {
                                Voters.Add(voter);
                            }
                        }

                        break;
                    }
                case 0:
                    {
                        Mobile = reader.ReadEntity<Mobile>();

                        var mobs = reader.ReadEntityList<Mobile>();
                        Voters = new List<Voter>(mobs.Count);

                        for (var i = 0; i < mobs.Count; ++i)
                        {
                            Voters.Add(new Voter(mobs[i], Mobile));
                        }

                        break;
                    }
            }
        }

        public Mobile Mobile { get; }

        public List<Voter> Voters { get; }

        public int Votes => Voters.Count;

        public void CleanMuleVotes()
        {
            for (var i = 0; i < Voters.Count; ++i)
            {
                var voter = Voters[i];

                if ((int)voter.AcquireFields()[3] < 90)
                {
                    Voters.RemoveAt(i--);
                }
            }
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(1); // version

            writer.Write(Mobile);

            writer.WriteEncodedInt(Voters.Count);

            for (var i = 0; i < Voters.Count; ++i)
            {
                Voters[i].Serialize(writer);
            }
        }
    }

    public enum ElectionState
    {
        Pending,
        Campaign,
        Election
    }
}
