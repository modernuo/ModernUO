using System;
using System.Collections.Generic;
using Server.Accounting;
using Server.Network;

namespace Server.Gumps
{
    public class BanDurationGump : Gump
    {
        private readonly List<Account> m_List;

        public BanDurationGump(Account a) : this(new List<Account> { a })
        {
        }

        public BanDurationGump(List<Account> list) : base((640 - 500) / 2, (480 - 305) / 2)
        {
            m_List = list;

            var width = 500;
            var height = 305;

            AddPage(0);

            AddBackground(0, 0, width, height, 5054);

            // AddImageTiled( 10, 10, width - 20, 20, 2624 );
            // AddAlphaRegion( 10, 10, width - 20, 20 );
            AddHtml(10, 10, width - 20, 20, "<CENTER>Ban Duration</CENTER>");

            // AddImageTiled( 10, 40, width - 20, height - 50, 2624 );
            // AddAlphaRegion( 10, 40, width - 20, height - 50 );

            AddButtonLabeled(15, 45, 1, "Infinite");
            AddButtonLabeled(15, 65, 2, "From D:H:M:S");

            AddInput(3, 0, "Days");
            AddInput(4, 1, "Hours");
            AddInput(5, 2, "Minutes");
            AddInput(6, 3, "Seconds");

            AddHtml(170, 45, 240, 20, "Comments:");
            AddTextField(170, 65, 315, height - 80, 10);
        }

        public void AddButtonLabeled(int x, int y, int buttonID, string text)
        {
            AddButton(x, y - 1, 4005, 4007, buttonID);
            AddHtml(x + 35, y, 240, 20, text);
        }

        public void AddTextField(int x, int y, int width, int height, int index)
        {
            AddBackground(x - 2, y - 2, width + 4, height + 4, 0x2486);
            AddTextEntry(x + 2, y + 2, width - 4, height - 4, 0, index, "");
        }

        public void AddInput(int bid, int idx, string name)
        {
            var x = 15;
            var y = 95 + idx * 50;

            AddButtonLabeled(x, y, bid, name);
            AddTextField(x + 35, y + 20, 100, 20, idx);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var from = sender.Mobile;

            if (from.AccessLevel < AccessLevel.Administrator)
            {
                return;
            }

            var d = info.GetTextEntry(0);
            var h = info.GetTextEntry(1);
            var m = info.GetTextEntry(2);
            var s = info.GetTextEntry(3);

            var c = info.GetTextEntry(10);

            TimeSpan duration;
            bool shouldSet;

            switch (info.ButtonID)
            {
                case 0:
                    {
                        for (var i = 0; i < m_List.Count; ++i)
                        {
                            var a = m_List[i];

                            a.SetUnspecifiedBan(from);
                        }

                        from.SendMessage("Duration unspecified.");
                        return;
                    }
                case 1: // infinite
                    {
                        duration = TimeSpan.MaxValue;
                        shouldSet = true;
                        break;
                    }
                case 2: // From D:H:M:S
                    {
                        if (d != null && h != null && m != null && s != null)
                        {
                            try
                            {
                                duration = new TimeSpan(
                                    Utility.ToInt32(d.Text),
                                    Utility.ToInt32(h.Text),
                                    Utility.ToInt32(m.Text),
                                    Utility.ToInt32(s.Text)
                                );
                                shouldSet = true;

                                break;
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        duration = TimeSpan.Zero;
                        shouldSet = false;

                        break;
                    }
                case 3: // From D
                    {
                        if (d != null)
                        {
                            try
                            {
                                duration = TimeSpan.FromDays(Utility.ToDouble(d.Text));
                                shouldSet = true;

                                break;
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        duration = TimeSpan.Zero;
                        shouldSet = false;

                        break;
                    }
                case 4: // From H
                    {
                        if (h != null)
                        {
                            try
                            {
                                duration = TimeSpan.FromHours(Utility.ToDouble(h.Text));
                                shouldSet = true;

                                break;
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        duration = TimeSpan.Zero;
                        shouldSet = false;

                        break;
                    }
                case 5: // From M
                    {
                        if (m != null)
                        {
                            try
                            {
                                duration = TimeSpan.FromMinutes(Utility.ToDouble(m.Text));
                                shouldSet = true;

                                break;
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        duration = TimeSpan.Zero;
                        shouldSet = false;

                        break;
                    }
                case 6: // From S
                    {
                        if (s != null)
                        {
                            try
                            {
                                duration = TimeSpan.FromSeconds(Utility.ToDouble(s.Text));
                                shouldSet = true;

                                break;
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        duration = TimeSpan.Zero;
                        shouldSet = false;

                        break;
                    }
                default: return;
            }

            if (shouldSet)
            {
                var comment = c?.Text.Trim().DefaultIfNullOrEmpty(null);

                for (var i = 0; i < m_List.Count; ++i)
                {
                    var a = m_List[i];

                    a.SetBanTags(from, Core.Now, duration);

                    if (comment != null)
                    {
                        a.Comments.Add(
                            new AccountComment(
                                from.RawName,
                                $"Duration: {(duration == TimeSpan.MaxValue ? "Infinite" : duration.ToString())}, Comment: {comment}"
                            )
                        );
                    }
                }

                if (duration == TimeSpan.MaxValue)
                {
                    from.SendMessage("Ban Duration: Infinite");
                }
                else
                {
                    from.SendMessage($"Ban Duration: {duration}");
                }
            }
            else
            {
                from.SendMessage("Time values were improperly formatted.");
                from.SendGump(new BanDurationGump(m_List));
            }
        }
    }
}
