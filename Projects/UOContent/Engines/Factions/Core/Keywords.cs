using Server.Mobiles;
using Server.Network;

namespace Server.Factions
{
    public static class Keywords
    {
        public static void Initialize()
        {
            EventSink.Speech += EventSink_Speech;
        }

        private static void ShowScore_Sandbox(PlayerState pl)
        {
            pl?.Mobile.PublicOverheadMessage(
                MessageType.Regular,
                pl.Mobile.SpeechHue,
                true,
                pl.KillPoints.ToString("N0")
            );
        }

        private static void EventSink_Speech(SpeechEventArgs e)
        {
            var from = e.Mobile;
            var keywords = e.Keywords;

            for (var i = 0; i < keywords.Length; ++i)
            {
                switch (keywords[i])
                {
                    case 0x00E4: // *i wish to access the city treasury*
                        {
                            var town = Town.FromRegion(from.Region);

                            if (town?.IsFinance(from) != true || !from.Alive)
                            {
                                break;
                            }

                            if (FactionGump.Exists(from))
                            {
                                from.SendLocalizedMessage(1042160); // You already have a faction menu open.
                            }
                            else if (town.Owner != null && from is PlayerMobile mobile)
                            {
                                mobile.SendGump(new FinanceGump(mobile, town.Owner, town));
                            }

                            break;
                        }
                    case 0x0ED: // *i am sheriff*
                        {
                            var town = Town.FromRegion(from.Region);

                            if (town?.IsSheriff(from) != true || !from.Alive)
                            {
                                break;
                            }

                            if (FactionGump.Exists(from))
                            {
                                from.SendLocalizedMessage(1042160); // You already have a faction menu open.
                            }
                            else if (town.Owner != null)
                            {
                                from.SendGump(new SheriffGump((PlayerMobile)from, town.Owner, town));
                            }

                            break;
                        }
                    case 0x00EF: // *you are fired*
                        {
                            var town = Town.FromRegion(from.Region);

                            if (town == null)
                            {
                                break;
                            }

                            if (town.IsFinance(from) || town.IsSheriff(from))
                            {
                                town.BeginOrderFiring(from);
                            }

                            break;
                        }
                    case 0x00E5: // *i wish to resign as finance minister*
                        {
                            var pl = PlayerState.Find(from);

                            if (pl?.Finance != null)
                            {
                                pl.Finance.Finance = null;
                                from.SendLocalizedMessage(1005081); // You have been fired as Finance Minister
                            }

                            break;
                        }
                    case 0x00EE: // *i wish to resign as sheriff*
                        {
                            var pl = PlayerState.Find(from);

                            if (pl?.Sheriff != null)
                            {
                                pl.Sheriff.Sheriff = null;
                                from.SendLocalizedMessage(1010270); // You have been fired as Sheriff
                            }

                            break;
                        }
                    case 0x00E9: // *what is my faction term status*
                        {
                            var pl = PlayerState.Find(from);

                            if (pl?.IsLeaving == true)
                            {
                                if (Faction.CheckLeaveTimer(from))
                                {
                                    break;
                                }

                                var remaining = pl.Leaving + Faction.LeavePeriod - Core.Now;

                                if (remaining.TotalDays >= 1)
                                {
                                    // Your term of service will come to an end in ~1_DAYS~ days.
                                    from.SendLocalizedMessage(1042743, remaining.TotalDays.ToString("N0"));
                                }
                                else if (remaining.TotalHours >= 1)
                                {
                                    // Your term of service will come to an end in ~1_HOURS~ hours.
                                    from.SendLocalizedMessage(1042741, remaining.TotalHours.ToString("N0"));
                                }
                                else
                                {
                                    // Your term of service will come to an end in less than one hour.
                                    from.SendLocalizedMessage(1042742);
                                }
                            }
                            else if (pl != null)
                            {
                                // You are not in the process of quitting the faction.
                                from.SendLocalizedMessage(1042233);
                            }

                            break;
                        }
                    case 0x00EA: // *message faction*
                        {
                            var faction = Faction.Find(from);

                            if (faction?.IsCommander(from) != true)
                            {
                                break;
                            }

                            if (from.AccessLevel == AccessLevel.Player && !faction.FactionMessageReady)
                            {
                                // The required time has not yet passed since the last message was sent
                                from.SendLocalizedMessage(1010264);
                            }
                            else
                            {
                                faction.BeginBroadcast(from);
                            }

                            break;
                        }
                    case 0x00EC: // *showscore*
                        {
                            var pl = PlayerState.Find(from);

                            if (pl != null)
                            {
                                Timer.StartTimer(() => ShowScore_Sandbox(pl));
                            }

                            break;
                        }
                    case 0x0178: // i honor your leadership
                        {
                            Faction.Find(from)?.BeginHonorLeadership(from);
                            break;
                        }
                }
            }
        }
    }
}
