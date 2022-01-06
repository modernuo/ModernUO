using System.Collections.Generic;
using Server.Ethics;
using Server.Factions;

namespace Server.Engines.ConPVP
{
    public class TourneyPyramid
    {
        public TourneyPyramid() => Levels = new List<PyramidLevel>();

        public List<PyramidLevel> Levels { get; set; }

        public void AddLevel(
            int partsPerMatch, List<TourneyParticipant> participants, GroupingType groupType, TourneyType tourneyType
        )
        {
            var copy = new List<TourneyParticipant>(participants);

            if (groupType is GroupingType.Nearest or GroupingType.HighVsLow)
            {
                copy.Sort();
            }

            var level = new PyramidLevel();

            switch (tourneyType)
            {
                case TourneyType.RedVsBlue:
                    {
                        var parts = new TourneyParticipant[2];

                        for (var i = 0; i < parts.Length; ++i)
                        {
                            parts[i] = new TourneyParticipant(new List<Mobile>());
                        }

                        for (var i = 0; i < copy.Count; ++i)
                        {
                            var players = copy[i].Players;

                            for (var j = 0; j < players.Count; ++j)
                            {
                                var mob = players[j];

                                if (mob.Kills >= 5)
                                {
                                    parts[0].Players.Add(mob);
                                }
                                else
                                {
                                    parts[1].Players.Add(mob);
                                }
                            }
                        }

                        level.Matches.Add(new TourneyMatch(new List<TourneyParticipant>(parts)));
                        break;
                    }
                case TourneyType.Faction:
                    {
                        var parts = new TourneyParticipant[partsPerMatch];

                        for (var i = 0; i < parts.Length; ++i)
                        {
                            parts[i] = new TourneyParticipant(new List<Mobile>());
                        }

                        for (var i = 0; i < copy.Count; ++i)
                        {
                            var players = copy[i].Players;

                            for (var j = 0; j < players.Count; ++j)
                            {
                                var mob = players[j];

                                var index = -1;

                                if (partsPerMatch == 4)
                                {
                                    var fac = Faction.Find(mob);

                                    if (fac != null)
                                    {
                                        index = fac.Definition.Sort;
                                    }
                                }
                                else if (partsPerMatch == 2)
                                {
                                    if (Ethic.Evil.IsEligible(mob))
                                    {
                                        index = 0;
                                    }
                                    else if (Ethic.Hero.IsEligible(mob))
                                    {
                                        index = 1;
                                    }
                                }

                                if (index < 0 || index >= partsPerMatch)
                                {
                                    index = i % partsPerMatch;
                                }

                                parts[index].Players.Add(mob);
                            }
                        }

                        level.Matches.Add(new TourneyMatch(new List<TourneyParticipant>(parts)));
                        break;
                    }
                case TourneyType.RandomTeam:
                    {
                        var parts = new TourneyParticipant[partsPerMatch];

                        for (var i = 0; i < partsPerMatch; ++i)
                        {
                            parts[i] = new TourneyParticipant(new List<Mobile>());
                        }

                        for (var i = 0; i < copy.Count; ++i)
                        {
                            parts[i % parts.Length].Players.AddRange(copy[i].Players);
                        }

                        level.Matches.Add(new TourneyMatch(new List<TourneyParticipant>(parts)));
                        break;
                    }
                case TourneyType.FreeForAll:
                    {
                        level.Matches.Add(new TourneyMatch(copy));
                        break;
                    }
                case TourneyType.Standard:
                    {
                        if (partsPerMatch >= 2 && participants.Count % partsPerMatch == 1)
                        {
                            var lowAdvances = int.MaxValue;

                            for (var i = 0; i < participants.Count; ++i)
                            {
                                var p = participants[i];

                                if (p.FreeAdvances < lowAdvances)
                                {
                                    lowAdvances = p.FreeAdvances;
                                }
                            }

                            var toAdvance = new List<TourneyParticipant>();

                            for (var i = 0; i < participants.Count; ++i)
                            {
                                var p = participants[i];

                                if (p.FreeAdvances == lowAdvances)
                                {
                                    toAdvance.Add(p);
                                }
                            }

                            if (toAdvance.Count == 0)
                            {
                                toAdvance = copy; // sanity
                            }

                            var random = toAdvance.RandomElement();

                            random.AddLog(
                                "Advanced automatically due to an odd number of challengers."
                            );
                            level.FreeAdvance = random;
                            ++level.FreeAdvance.FreeAdvances;
                            copy.Remove(random);
                        }

                        while (copy.Count >= partsPerMatch)
                        {
                            var thisMatch = new List<TourneyParticipant>();

                            for (var i = 0; i < partsPerMatch; ++i)
                            {
                                var idx = groupType switch
                                {
                                    GroupingType.HighVsLow => i * (copy.Count - 1) / (partsPerMatch - 1),
                                    GroupingType.Nearest   => 0,
                                    GroupingType.Random    => Utility.Random(copy.Count),
                                    _                      => 0
                                };

                                thisMatch.Add(copy[idx]);
                                copy.RemoveAt(idx);
                            }

                            level.Matches.Add(new TourneyMatch(thisMatch));
                        }

                        if (copy.Count > 1)
                        {
                            level.Matches.Add(new TourneyMatch(copy));
                        }

                        break;
                    }
            }

            Levels.Add(level);
        }
    }

    public class PyramidLevel
    {
        public List<TourneyMatch> Matches { get; set; } = new();
        public TourneyParticipant FreeAdvance { get; set; }
    }
}
