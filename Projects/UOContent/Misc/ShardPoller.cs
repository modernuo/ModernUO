using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using Server.Gumps;
using Server.Network;
using Server.Prompts;

namespace Server.Misc
{
    public class ShardPoller : Item
    {
        private static readonly List<ShardPoller> m_ActivePollers = new();

        private bool m_Active;
        private string m_Title;

        [Constructible(AccessLevel.Administrator)]
        public ShardPoller() : base(0x1047)
        {
            Duration = TimeSpan.FromHours(24.0);
            Options = Array.Empty<ShardPollOption>();
            Addresses = Array.Empty<IPAddress>();

            Movable = false;
        }

        public ShardPoller(Serial serial) : base(serial)
        {
        }

        public ShardPollOption[] Options { get; set; }

        public IPAddress[] Addresses { get; set; }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public string Title
        {
            get => m_Title;
            set => m_Title = ShardPollPrompt.UrlToHref(value);
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan Duration { get; set; }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public DateTime StartTime { get; set; }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan TimeRemaining =>
            StartTime == DateTime.MinValue || !m_Active
                ? TimeSpan.Zero
                : Utility.Max(StartTime + Duration - Core.Now, TimeSpan.Zero);

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool Active
        {
            get => m_Active;
            set
            {
                if (m_Active == value)
                {
                    return;
                }

                m_Active = value;

                if (m_Active)
                {
                    StartTime = Core.Now;
                    m_ActivePollers.Add(this);
                }
                else
                {
                    m_ActivePollers.Remove(this);
                }
            }
        }

        public override string DefaultName => "shard poller";

        public bool HasAlreadyVoted(NetState ns)
        {
            for (var i = 0; i < Options.Length; ++i)
            {
                if (Options[i].HasAlreadyVoted(ns))
                {
                    return true;
                }
            }

            return false;
        }

        public void AddVote(NetState ns, ShardPollOption option)
        {
            option.AddVote(ns);
        }

        public void RemoveOption(ShardPollOption option)
        {
            var index = Array.IndexOf(Options, option);

            if (index < 0)
            {
                return;
            }

            var old = Options;
            Options = new ShardPollOption[old.Length - 1];

            for (var i = 0; i < index; ++i)
            {
                Options[i] = old[i];
            }

            for (var i = index; i < Options.Length; ++i)
            {
                Options[i] = old[i + 1];
            }
        }

        public void AddOption(ShardPollOption option)
        {
            var old = Options;
            Options = new ShardPollOption[old.Length + 1];

            for (var i = 0; i < old.Length; ++i)
            {
                Options[i] = old[i];
            }

            Options[old.Length] = option;
        }

        public static void Initialize()
        {
            EventSink.Login += EventSink_Login;
        }

        private static void EventSink_Login(Mobile m)
        {
            if (m_ActivePollers.Count == 0)
            {
                return;
            }

            Timer.StartTimer(TimeSpan.FromSeconds(1.0), () => EventSink_Login_Callback(m));
        }

        private static void EventSink_Login_Callback(Mobile from)
        {
            var ns = from.NetState;

            if (ns == null)
            {
                return;
            }

            ShardPollGump spg = null;

            for (var i = 0; i < m_ActivePollers.Count; ++i)
            {
                var poller = m_ActivePollers[i];

                if (poller.Deleted || !poller.Active)
                {
                    continue;
                }

                if (poller.TimeRemaining > TimeSpan.Zero)
                {
                    if (poller.HasAlreadyVoted(ns))
                    {
                        continue;
                    }

                    if (spg == null)
                    {
                        spg = new ShardPollGump(from, poller, false, null);
                        from.SendGump(spg);
                    }
                    else
                    {
                        spg.QueuePoll(poller);
                    }
                }
                else
                {
                    poller.Active = false;
                }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.Administrator)
            {
                from.SendGump(new ShardPollGump(from, this, true, null));
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_Title);
            writer.Write(Duration);
            writer.Write(StartTime);
            writer.Write(m_Active);

            writer.Write(Options.Length);

            for (var i = 0; i < Options.Length; ++i)
            {
                Options[i].Serialize(writer);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Title = reader.ReadString();
                        Duration = reader.ReadTimeSpan();
                        StartTime = reader.ReadDateTime();
                        m_Active = reader.ReadBool();

                        Options = new ShardPollOption[reader.ReadInt()];

                        for (var i = 0; i < Options.Length; ++i)
                        {
                            Options[i] = new ShardPollOption(reader);
                        }

                        if (m_Active)
                        {
                            m_ActivePollers.Add(this);
                        }

                        break;
                    }
            }
        }

        public override void OnDelete()
        {
            base.OnDelete();

            Active = false;
        }
    }

    public class ShardPollOption
    {
        private string m_Title;

        public ShardPollOption(string title)
        {
            m_Title = title;
            LineBreaks = GetBreaks(m_Title);
            Voters = Array.Empty<IPAddress>();
        }

        public ShardPollOption(IGenericReader reader)
        {
            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Title = reader.ReadString();
                        LineBreaks = GetBreaks(m_Title);

                        Voters = new IPAddress[reader.ReadInt()];

                        for (var i = 0; i < Voters.Length; ++i)
                        {
                            Voters[i] = Utility.Intern(reader.ReadIPAddress());
                        }

                        break;
                    }
            }
        }

        public string Title
        {
            get => m_Title;
            set
            {
                m_Title = value;
                LineBreaks = GetBreaks(m_Title);
            }
        }

        public int LineBreaks { get; private set; }

        public int Votes => Voters.Length;
        public IPAddress[] Voters { get; set; }

        public bool HasAlreadyVoted(NetState ns)
        {
            if (ns == null)
            {
                return false;
            }

            var ipAddress = ns.Address;

            for (var i = 0; i < Voters.Length; ++i)
            {
                if (Utility.IPMatchClassC(Voters[i], ipAddress))
                {
                    return true;
                }
            }

            return false;
        }

        public void AddVote(NetState ns)
        {
            if (ns == null)
            {
                return;
            }

            var old = Voters;
            Voters = new IPAddress[old.Length + 1];

            for (var i = 0; i < old.Length; ++i)
            {
                Voters[i] = old[i];
            }

            Voters[old.Length] = ns.Address;
        }

        public int ComputeHeight()
        {
            var height = LineBreaks * 18;

            if (height > 30)
            {
                return height;
            }

            return 30;
        }

        public int GetBreaks(string title)
        {
            if (title == null)
            {
                return 1;
            }

            var count = 0;
            var index = -1;

            do
            {
                ++count;
                index = title.IndexOfOrdinal("<br>", index + 1);
            } while (index >= 0);

            return count;
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.Write(0); // version

            writer.Write(m_Title);

            writer.Write(Voters.Length);

            for (var i = 0; i < Voters.Length; ++i)
            {
                writer.Write(Voters[i]);
            }
        }
    }

    public class ShardPollGump : Gump
    {
        private const int LabelColor32 = 0xFFFFFF;
        private readonly Mobile m_From;
        private readonly ShardPoller m_Poller;
        private Queue<ShardPoller> m_Polls;

        public ShardPollGump(Mobile from, ShardPoller poller, bool editing, Queue<ShardPoller> polls) : base(50, 50)
        {
            m_From = from;
            m_Poller = poller;
            Editing = editing;
            m_Polls = polls;

            Closable = false;

            AddPage(0);

            var totalVotes = 0;
            var totalOptionHeight = 0;

            for (var i = 0; i < poller.Options.Length; ++i)
            {
                totalVotes += poller.Options[i].Votes;
                totalOptionHeight += poller.Options[i].ComputeHeight() + 5;
            }

            var isViewingResults = editing && poller.Active;
            var isCompleted = totalVotes > 0 && !poller.Active;

            if (editing && !isViewingResults)
            {
                totalOptionHeight += 35;
            }

            var height = 115 + totalOptionHeight;

            AddBackground(1, 1, 398, height - 2, 3600);
            AddAlphaRegion(16, 15, 369, height - 31);

            AddItem(308, 30, 0x1E5E);

            string title;

            if (editing)
            {
                title = isCompleted ? "Poll Completed" : "Poll Editor";
            }
            else
            {
                title = "Shard Poll";
            }

            AddHtml(22, 22, 294, 20, Color(Center(title), LabelColor32));

            if (editing)
            {
                AddHtml(22, 22, 294, 20, Color($"{totalVotes} total", LabelColor32));
                AddButton(287, 23, 0x2622, 0x2623, 2);
            }

            AddHtml(22, 50, 294, 40, Color(poller.Title, 0x99CC66));

            AddImageTiled(32, 88, 264, 1, 9107);
            AddImageTiled(42, 90, 264, 1, 9157);

            var y = 100;

            for (var i = 0; i < poller.Options.Length; ++i)
            {
                var option = poller.Options[i];
                var text = option.Title;

                if (editing && totalVotes > 0)
                {
                    var perc = option.Votes / (double)totalVotes;

                    text = $"[{option.Votes}: {(int)(perc * 100)}%] {text}";
                }

                var optHeight = option.ComputeHeight();

                y += optHeight / 2;

                if (isViewingResults)
                {
                    AddImage(24, y - 15, 0x25FE);
                }
                else
                {
                    AddRadio(24, y - 15, 0x25F9, 0x25FC, false, 1 + i);
                }

                AddHtml(60, y - 9 * option.LineBreaks, 250, 18 * option.LineBreaks, Color(text, LabelColor32));

                y += optHeight / 2;
                y += 5;
            }

            if (editing && !isViewingResults)
            {
                AddRadio(24, y + 15 - 15, 0x25F9, 0x25FC, false, 1 + poller.Options.Length);
                AddHtml(60, y + 15 - 9, 250, 18, Color("Create new option.", 0x99CC66));
            }

            AddButton(314, height - 73, 247, 248, 1);
            AddButton(314, height - 47, 242, 241, 0);
        }

        public bool Editing { get; }

        public void QueuePoll(ShardPoller poller)
        {
            m_Polls ??= new Queue<ShardPoller>(4);

            m_Polls.Enqueue(poller);
        }

        public string Center(string text) => $"<CENTER>{text}</CENTER>";

        public string Color(string text, int color) => $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_Polls?.Count > 0)
            {
                var shardPoller = m_Polls.Dequeue();

                if (shardPoller != null)
                {
                    Timer.StartTimer(
                        TimeSpan.FromSeconds(1.0),
                        () => m_From.SendGump(new ShardPollGump(m_From, shardPoller, false, m_Polls))
                    );
                }
            }

            if (info.ButtonID == 1)
            {
                var switches = info.Switches;

                if (switches.Length == 0)
                {
                    return;
                }

                var switched = switches[0] - 1;
                ShardPollOption opt = null;

                if (switched >= 0 && switched < m_Poller.Options.Length)
                {
                    opt = m_Poller.Options[switched];
                }

                if (opt == null && !Editing)
                {
                    return;
                }

                if (Editing)
                {
                    if (!m_Poller.Active)
                    {
                        if (opt == null)
                        {
                            m_From.SendMessage($"Enter a title for the option. Escape to cancel.");
                        }
                        else
                        {
                            m_From.SendMessage($"Enter a title for the option. Escape to cancel. Use \"DEL\" to delete.");
                        }

                        m_From.Prompt = new ShardPollPrompt(m_Poller, opt);
                    }
                    else
                    {
                        m_From.SendMessage("You may not edit an active poll. Deactivate it first.");
                        m_From.SendGump(new ShardPollGump(m_From, m_Poller, Editing, m_Polls));
                    }
                }
                else
                {
                    if (!m_Poller.Active)
                    {
                        m_From.SendMessage("The poll has been deactivated.");
                    }
                    else if (m_Poller.HasAlreadyVoted(sender))
                    {
                        m_From.SendMessage("You have already voted on this poll.");
                    }
                    else
                    {
                        m_Poller.AddVote(sender, opt);
                    }
                }
            }
            else if (info.ButtonID == 2 && Editing)
            {
                m_From.SendGump(new ShardPollGump(m_From, m_Poller, Editing, m_Polls));
                m_From.SendGump(new PropertiesGump(m_From, m_Poller));
            }
        }
    }

    public class ShardPollPrompt : Prompt
    {
        private static readonly Regex m_UrlRegex =
            new(@"\[url(?:=(.*?))?\](.*?)\[/url\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly ShardPollOption m_Option;
        private readonly ShardPoller m_Poller;

        public ShardPollPrompt(ShardPoller poller, ShardPollOption opt)
        {
            m_Poller = poller;
            m_Option = opt;
        }

        public override void OnCancel(Mobile from)
        {
            from.SendGump(new ShardPollGump(from, m_Poller, true, null));
        }

        private static string UrlRegex_Match(Match m)
        {
            if (m.Groups[1].Success)
            {
                if (m.Groups[2].Success)
                {
                    return $"<a href=\"{m.Groups[1].Value}\">{m.Groups[2].Value}</a>";
                }
            }
            else if (m.Groups[2].Success)
            {
                return $"<a href=\"{m.Groups[2].Value}\">{m.Groups[2].Value}</a>";
            }

            return m.Value;
        }

        public static string UrlToHref(string text)
        {
            if (text == null)
            {
                return null;
            }

            return m_UrlRegex.Replace(text, UrlRegex_Match);
        }

        public override void OnResponse(Mobile from, string text)
        {
            if (m_Poller.Active)
            {
                from.SendMessage("You may not edit an active poll. Deactivate it first.");
            }
            else if (text == "DEL")
            {
                if (m_Option != null)
                {
                    m_Poller.RemoveOption(m_Option);
                }
            }
            else
            {
                text = UrlToHref(text);

                if (m_Option == null)
                {
                    m_Poller.AddOption(new ShardPollOption(text));
                }
                else
                {
                    m_Option.Title = text;
                }
            }

            from.SendGump(new ShardPollGump(from, m_Poller, true, null));
        }
    }
}
