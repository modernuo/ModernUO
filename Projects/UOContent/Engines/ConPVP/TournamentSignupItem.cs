using System.Collections.Generic;
using Server.Factions;
using Server.Mobiles;

namespace Server.Engines.ConPVP
{
    public class TournamentSignupItem : Item
    {
        [Constructible]
        public TournamentSignupItem() : base(4029) => Movable = false;

        public TournamentSignupItem(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TournamentController Tournament { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Registrar { get; set; }

        public override string DefaultName => "tournament signup book";

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that
            }
            else
            {
                var tourney = Tournament?.Tournament;

                if (tourney == null)
                {
                    return;
                }

                if (Registrar != null)
                {
                    Registrar.Direction = Registrar.GetDirectionTo(this);
                }

                switch (tourney.Stage)
                {
                    case TournamentStage.Fighting:
                        {
                            if (Registrar != null)
                            {
                                if (tourney.HasParticipant(from))
                                {
                                    Registrar.PrivateOverheadMessage(
                                        MessageType.Regular,
                                        0x35,
                                        false,
                                        "Excuse me? You are already signed up.",
                                        from.NetState
                                    );
                                }
                                else
                                {
                                    Registrar.PrivateOverheadMessage(
                                        MessageType.Regular,
                                        0x22,
                                        false,
                                        "The tournament has already begun. You are too late to signup now.",
                                        from.NetState
                                    );
                                }
                            }

                            break;
                        }
                    case TournamentStage.Inactive:
                        {
                            Registrar?.PrivateOverheadMessage(
                                MessageType.Regular,
                                0x35,
                                false,
                                "The tournament is closed.",
                                from.NetState
                            );

                            break;
                        }
                    case TournamentStage.Signup:
                        {
                            var ladder = Ladder.Instance;
                            var entry = ladder?.Find(from);

                            if (entry != null && Ladder.GetLevel(entry.Experience) < tourney.LevelRequirement)
                            {
                                Registrar?.PrivateOverheadMessage(
                                    MessageType.Regular,
                                    0x35,
                                    false,
                                    "You have not yet proven yourself a worthy dueler.",
                                    from.NetState
                                );

                                break;
                            }

                            if (tourney.IsFactionRestricted && Faction.Find(from) == null)
                            {
                                Registrar?.PrivateOverheadMessage(
                                    MessageType.Regular,
                                    0x35,
                                    false,
                                    "Only those who have declared their faction allegiance may participate.",
                                    from.NetState
                                );

                                break;
                            }

                            if (from.HasGump<AcceptTeamGump>())
                            {
                                Registrar?.PrivateOverheadMessage(
                                    MessageType.Regular,
                                    0x22,
                                    false,
                                    "You must first respond to the offer I've given you.",
                                    from.NetState
                                );
                            }
                            else if (from.HasGump<AcceptDuelGump>())
                            {
                                Registrar?.PrivateOverheadMessage(
                                    MessageType.Regular,
                                    0x22,
                                    false,
                                    "You must first cancel your duel offer.",
                                    from.NetState
                                );
                            }
                            else if (from is PlayerMobile mobile && mobile.DuelContext != null)
                            {
                                Registrar?.PrivateOverheadMessage(
                                    MessageType.Regular,
                                    0x22,
                                    false,
                                    "You are already participating in a duel.",
                                    mobile.NetState
                                );
                            }
                            else if (!tourney.HasParticipant(from))
                            {
                                from.CloseGump<ConfirmSignupGump>();
                                from.SendGump(new ConfirmSignupGump(from, Registrar, tourney, new List<Mobile> { from }));
                            }
                            else
                            {
                                Registrar?.PrivateOverheadMessage(
                                    MessageType.Regular,
                                    0x35,
                                    false,
                                    "You have already entered this tournament.",
                                    from.NetState
                                );
                            }

                            break;
                        }
                }
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);

            writer.Write(Tournament);
            writer.Write(Registrar);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        Tournament = reader.ReadEntity<TournamentController>();
                        Registrar = reader.ReadEntity<Mobile>();
                        break;
                    }
            }
        }
    }
}
