using System;
using System.Text;
using Server.Mobiles;

namespace Server.Engines.ConPVP
{
    public class Participant
    {
        public Participant(DuelContext context, int count)
        {
            Context = context;
            // m_Stakes = new StakesContainer( context, this );
            Resize(count);
        }

        public int Count => Players.Length;
        public DuelPlayer[] Players { get; private set; }

        public DuelContext Context { get; }

        public TourneyParticipant TourneyPart { get; set; }

        public int FilledSlots
        {
            get
            {
                var count = 0;

                for (var i = 0; i < Players.Length; ++i)
                {
                    if (Players[i] != null)
                    {
                        ++count;
                    }
                }

                return count;
            }
        }

        public bool HasOpenSlot
        {
            get
            {
                for (var i = 0; i < Players.Length; ++i)
                {
                    if (Players[i] == null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool Eliminated
        {
            get
            {
                for (var i = 0; i < Players.Length; ++i)
                {
                    if (Players[i]?.Eliminated == false)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public string NameList
        {
            get
            {
                var sb = new StringBuilder();

                for (var i = 0; i < Players.Length; ++i)
                {
                    if (Players[i] == null)
                    {
                        continue;
                    }

                    var mob = Players[i].Mobile;

                    if (sb.Length > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(mob.Name);
                }

                return sb.Length == 0 ? "Empty" : sb.ToString();
            }
        }

        public DuelPlayer Find(Mobile mob)
        {
            if (mob is PlayerMobile pm)
            {
                if (pm.DuelContext == Context && pm.DuelPlayer.Participant == this)
                {
                    return pm.DuelPlayer;
                }

                return null;
            }

            for (var i = 0; i < Players.Length; ++i)
            {
                if (Players[i]?.Mobile == mob)
                {
                    return Players[i];
                }
            }

            return null;
        }

        public bool Contains(Mobile mob) => Find(mob) != null;

        public void Broadcast(int hue, string message, string nonLocalOverhead, string localOverhead)
        {
            for (var i = 0; i < Players.Length; ++i)
            {
                if (Players[i] != null)
                {
                    if (message != null)
                    {
                        Players[i].Mobile.SendMessage(hue, message);
                    }

                    if (nonLocalOverhead != null)
                    {
                        Players[i]
                            .Mobile.NonlocalOverheadMessage(
                                MessageType.Regular,
                                hue,
                                false,
                                string.Format(
                                    nonLocalOverhead,
                                    Players[i].Mobile.Name,
                                    Players[i].Mobile.Female ? "her" : "his"
                                )
                            );
                    }

                    if (localOverhead != null)
                    {
                        Players[i].Mobile.LocalOverheadMessage(MessageType.Regular, hue, false, localOverhead);
                    }
                }
            }
        }

        public void Nullify(DuelPlayer player)
        {
            if (player == null)
            {
                return;
            }

            var index = Array.IndexOf(Players, player);

            if (index == -1)
            {
                return;
            }

            Players[index] = null;
        }

        public void Remove(DuelPlayer player)
        {
            if (player == null)
            {
                return;
            }

            var index = Array.IndexOf(Players, player);

            if (index == -1)
            {
                return;
            }

            var old = Players;
            Players = new DuelPlayer[old.Length - 1];

            for (var i = 0; i < index; ++i)
            {
                Players[i] = old[i];
            }

            for (var i = index + 1; i < old.Length; ++i)
            {
                Players[i - 1] = old[i];
            }
        }

        public void Remove(Mobile player)
        {
            Remove(Find(player));
        }

        public void Add(Mobile player)
        {
            if (Contains(player))
            {
                return;
            }

            for (var i = 0; i < Players.Length; ++i)
            {
                if (Players[i] == null)
                {
                    Players[i] = new DuelPlayer(player, this);
                    return;
                }
            }

            Resize(Players.Length + 1);
            Players[^1] = new DuelPlayer(player, this);
        }

        public void Resize(int count)
        {
            var old = Players;
            Players = new DuelPlayer[count];

            if (old != null)
            {
                var ct = 0;

                for (var i = 0; i < old.Length; ++i)
                {
                    if (old[i] != null && ct < count)
                    {
                        Players[ct++] = old[i];
                    }
                }
            }
        }
    }

    public class DuelPlayer
    {
        private bool m_Eliminated;

        public DuelPlayer(Mobile mob, Participant p)
        {
            Mobile = mob;
            Participant = p;

            if (mob is PlayerMobile mobile)
            {
                mobile.DuelPlayer = this;
            }
        }

        public Mobile Mobile { get; }

        public bool Ready { get; set; }

        public bool Eliminated
        {
            get => m_Eliminated;
            set
            {
                m_Eliminated = value;
                if (Participant.Context.m_Tournament != null && m_Eliminated)
                {
                    Participant.Context.m_Tournament.OnEliminated(this);
                    Mobile.SendEverything();
                }
            }
        }

        public Participant Participant { get; set; }
    }
}
