using System.Collections.Generic;
using Server.Ethics;
using Server.Factions;

namespace Server.Engines.ConPVP
{
  public class TourneyPyramid
  {
    public TourneyPyramid() => Levels = new List<PyramidLevel>();

    public List<PyramidLevel> Levels { get; set; }

    public void AddLevel(int partsPerMatch, List<TourneyParticipant> participants, GroupingType groupType, TourneyType tourneyType)
    {
      List<TourneyParticipant> copy = new List<TourneyParticipant>(participants);

      if (groupType == GroupingType.Nearest || groupType == GroupingType.HighVsLow)
        copy.Sort();

      PyramidLevel level = new PyramidLevel();

      switch (tourneyType)
      {
        case TourneyType.RedVsBlue:
          {
            TourneyParticipant[] parts = new TourneyParticipant[2];

            for (int i = 0; i < parts.Length; ++i)
              parts[i] = new TourneyParticipant(new List<Mobile>());

            for (int i = 0; i < copy.Count; ++i)
            {
              List<Mobile> players = copy[i].Players;

              for (int j = 0; j < players.Count; ++j)
              {
                Mobile mob = players[j];

                if (mob.Kills >= 5)
                  parts[0].Players.Add(mob);
                else
                  parts[1].Players.Add(mob);
              }
            }

            level.Matches.Add(new TourneyMatch(new List<TourneyParticipant>(parts)));
            break;
          }
        case TourneyType.Faction:
          {
            TourneyParticipant[] parts = new TourneyParticipant[partsPerMatch];

            for (int i = 0; i < parts.Length; ++i)
              parts[i] = new TourneyParticipant(new List<Mobile>());

            for (int i = 0; i < copy.Count; ++i)
            {
              List<Mobile> players = copy[i].Players;

              for (int j = 0; j < players.Count; ++j)
              {
                Mobile mob = players[j];

                int index = -1;

                if (partsPerMatch == 4)
                {
                  Faction fac = Faction.Find(mob);

                  if (fac != null)
                    index = fac.Definition.Sort;
                }
                else if (partsPerMatch == 2)
                {
                  if (Ethic.Evil.IsEligible(mob))
                    index = 0;
                  else if (Ethic.Hero.IsEligible(mob)) index = 1;
                }

                if (index < 0 || index >= partsPerMatch) index = i % partsPerMatch;

                parts[index].Players.Add(mob);
              }
            }

            level.Matches.Add(new TourneyMatch(new List<TourneyParticipant>(parts)));
            break;
          }
        case TourneyType.RandomTeam:
          {
            TourneyParticipant[] parts = new TourneyParticipant[partsPerMatch];

            for (int i = 0; i < partsPerMatch; ++i)
              parts[i] = new TourneyParticipant(new List<Mobile>());

            for (int i = 0; i < copy.Count; ++i)
              parts[i % parts.Length].Players.AddRange(copy[i].Players);

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
              int lowAdvances = int.MaxValue;

              for (int i = 0; i < participants.Count; ++i)
              {
                TourneyParticipant p = participants[i];

                if (p.FreeAdvances < lowAdvances)
                  lowAdvances = p.FreeAdvances;
              }

              List<TourneyParticipant> toAdvance = new List<TourneyParticipant>();

              for (int i = 0; i < participants.Count; ++i)
              {
                TourneyParticipant p = participants[i];

                if (p.FreeAdvances == lowAdvances)
                  toAdvance.Add(p);
              }

              if (toAdvance.Count == 0)
                toAdvance = copy; // sanity

              var random = toAdvance.RandomElement();

              random.AddLog(
                "Advanced automatically due to an odd number of challengers.");
              level.FreeAdvance = random;
              ++level.FreeAdvance.FreeAdvances;
              copy.Remove(random);
            }

            while (copy.Count >= partsPerMatch)
            {
              List<TourneyParticipant> thisMatch = new List<TourneyParticipant>();

              for (int i = 0; i < partsPerMatch; ++i)
              {
                var idx = groupType switch
                {
                  GroupingType.HighVsLow => i * (copy.Count - 1) / (partsPerMatch - 1),
                  GroupingType.Nearest => 0,
                  GroupingType.Random => Utility.Random(copy.Count),
                  _ => 0
                };

                thisMatch.Add(copy[idx]);
                copy.RemoveAt(idx);
              }

              level.Matches.Add(new TourneyMatch(thisMatch));
            }

            if (copy.Count > 1)
              level.Matches.Add(new TourneyMatch(copy));

            break;
          }
      }

      Levels.Add(level);
    }
  }

  public class PyramidLevel
  {
    public List<TourneyMatch> Matches { get; set; } = new List<TourneyMatch>();
    public TourneyParticipant FreeAdvance { get; set; }
  }
}
