using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Engines.ConPVP
{
    public class TourneyParticipant : IComparable<TourneyParticipant>
    {
        public TourneyParticipant(Mobile owner)
        {
            Log = new List<string>();
            Players = new List<Mobile> { owner };
        }

        public TourneyParticipant(List<Mobile> players)
        {
            Log = new List<string>();
            Players = players;
        }

        public List<Mobile> Players { get; set; }

        public List<string> Log { get; set; }

        public int FreeAdvances { get; set; }

        public int TotalLadderXP
        {
            get
            {
                var ladder = Ladder.Instance;

                if (ladder == null)
                {
                    return 0;
                }

                var total = 0;

                for (var i = 0; i < Players.Count; ++i)
                {
                    var mob = Players[i];
                    var entry = ladder.Find(mob);

                    if (entry != null)
                    {
                        total += entry.Experience;
                    }
                }

                return total;
            }
        }

        public string NameList
        {
            get
            {
                var sb = new StringBuilder();

                for (var i = 0; i < Players.Count; ++i)
                {
                    if (Players[i] == null)
                    {
                        continue;
                    }

                    var mob = Players[i];

                    if (sb.Length > 0)
                    {
                        if (Players.Count == 2)
                        {
                            sb.Append(" and ");
                        }
                        else if (i + 1 < Players.Count)
                        {
                            sb.Append(", ");
                        }
                        else
                        {
                            sb.Append(", and ");
                        }
                    }

                    sb.Append(mob.Name);
                }

                return sb.Length == 0 ? "Empty" : sb.ToString();
            }
        }

        public int CompareTo(TourneyParticipant p) => p.TotalLadderXP - TotalLadderXP;

        public void AddLog(string text) => Log.Add(text);

        public void WonMatch(TourneyMatch match)
        {
            AddLog("Match won.");
        }

        public void LostMatch(TourneyMatch match)
        {
            AddLog("Match lost.");
        }
    }
}
