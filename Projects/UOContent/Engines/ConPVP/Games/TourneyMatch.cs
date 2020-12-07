using System;
using System.Collections.Generic;
using System.Text;
using Server.Network;

namespace Server.Engines.ConPVP
{
    public class TourneyMatch
    {
        public TourneyMatch(List<TourneyParticipant> participants)
        {
            Participants = participants;

            for (var i = 0; i < participants.Count; ++i)
            {
                var part = participants[i];

                var sb = new StringBuilder();

                sb.Append("Matched in a duel against ");

                if (participants.Count > 2)
                {
                    sb.AppendFormat(
                        "{0} other {1}: ",
                        participants.Count - 1,
                        part.Players.Count == 1 ? "players" : "teams"
                    );
                }

                var hasAppended = false;

                for (var j = 0; j < participants.Count; ++j)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    if (hasAppended)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(participants[j].NameList);
                    hasAppended = true;
                }

                sb.Append('.');

                part.AddLog(sb.ToString());
            }
        }

        public List<TourneyParticipant> Participants { get; set; }

        public TourneyParticipant Winner { get; set; }

        public DuelContext Context { get; set; }

        public bool InProgress => Context?.Registered == true;

        public void Start(Arena arena, Tournament tourney)
        {
            var first = Participants[0];

            var dc = new DuelContext(first.Players[0], tourney.Ruleset.Layout, false);
            dc.Ruleset.Options.SetAll(false);
            dc.Ruleset.Options.Or(tourney.Ruleset.Options);

            for (var i = 0; i < Participants.Count; ++i)
            {
                var tourneyPart = Participants[i];
                var duelPart = new Participant(dc, tourneyPart.Players.Count)
                {
                    TourneyPart = tourneyPart
                };

                for (var j = 0; j < tourneyPart.Players.Count; ++j)
                {
                    duelPart.Add(tourneyPart.Players[j]);
                }

                for (var j = 0; j < duelPart.Players.Length; ++j)
                {
                    if (duelPart.Players[j] != null)
                    {
                        duelPart.Players[j].Ready = true;
                    }
                }

                dc.Participants.Add(duelPart);
            }

            if (tourney.EventController != null)
            {
                dc.m_EventGame = tourney.EventController.Construct(dc);
            }

            dc.m_Tournament = tourney;
            dc.m_Match = this;

            dc.m_OverrideArena = arena;

            if (tourney.SuddenDeath > TimeSpan.Zero &&
                (tourney.SuddenDeathRounds == 0 || tourney.Pyramid.Levels.Count <= tourney.SuddenDeathRounds))
            {
                dc.StartSuddenDeath(tourney.SuddenDeath);
            }

            dc.SendReadyGump(0);

            if (dc.StartedBeginCountdown)
            {
                Context = dc;

                for (var i = 0; i < Participants.Count; ++i)
                {
                    var p = Participants[i];

                    for (var j = 0; j < p.Players.Count; ++j)
                    {
                        var mob = p.Players[j];

                        foreach (var view in mob.GetMobilesInRange(18))
                        {
                            if (!mob.CanSee(view))
                            {
                                mob.NetState.SendRemoveEntity(view.Serial);
                            }
                        }

                        mob.LocalOverheadMessage(
                            MessageType.Emote,
                            0x3B2,
                            false,
                            "* Your mind focuses intently on the fight and all other distractions fade away *"
                        );
                    }
                }
            }
            else
            {
                dc.Unregister();
                dc.StopCountdown();
            }
        }
    }
}
