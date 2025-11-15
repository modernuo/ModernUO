using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using ModernUO.CodeGeneratedEvents;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Prompts;

namespace Server.Misc;

[SerializationGenerator(1, false)]
public partial class ShardPoller : Item
{
    private static readonly List<ShardPoller> _activePollers = new();

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private TimeSpan _duration;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private DateTime _startTime;

    [SerializableField(4)]
    private ShardPollOption[] _options;

    [Constructible(AccessLevel.Administrator)]
    public ShardPoller() : base(0x1047)
    {
        _duration = TimeSpan.FromHours(24.0);
        _options = [];

        Movable = false;
    }

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public string Title
    {
        get => _title;
        set
        {
            _title = ShardPollPrompt.UrlToHref(value);
            this.MarkDirty();
        }
    }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public TimeSpan TimeRemaining =>
        StartTime == DateTime.MinValue || !_active
            ? TimeSpan.Zero
            : Utility.Max(StartTime + Duration - Core.Now, TimeSpan.Zero);

    [SerializableProperty(3)]
    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public bool Active
    {
        get => _active;
        set
        {
            if (_active == value)
            {
                return;
            }

            _active = value;

            if (_active)
            {
                StartTime = Core.Now;
                _activePollers.Add(this);
            }
            else
            {
                _activePollers.Remove(this);
            }

            this.MarkDirty();
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
        Array.Copy(old, Options, index);
        Array.Copy(old, index + 1, Options, index, Options.Length - index);

    }

    public void AddOption(ShardPollOption option)
    {
        var old = Options;
        Options = new ShardPollOption[old.Length + 1];
        Array.Copy(old, Options, old.Length);

        Options[^1] = option;
    }

    [OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
    public static void OnLogin(PlayerMobile pm)
    {
        if (_activePollers.Count == 0)
        {
            return;
        }

        Timer.StartTimer(TimeSpan.FromSeconds(1.0), () => EventSink_Login_Callback(pm));
    }

    private static void EventSink_Login_Callback(Mobile from)
    {
        var ns = from.NetState;

        if (ns == null)
        {
            return;
        }

        ShardPollGump spg = null;

        for (var i = 0; i < _activePollers.Count; ++i)
        {
            var poller = _activePollers[i];

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

    private void Deserialize(IGenericReader reader, int version)
    {
        _title = reader.ReadString();
        _duration = reader.ReadTimeSpan();
        _startTime = reader.ReadDateTime();
        _active = reader.ReadBool();

        _options = new ShardPollOption[reader.ReadInt()];

        for (var i = 0; i < _options.Length; ++i)
        {
            var option = _options[i] = new ShardPollOption();
            option.Deserialize(reader);
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_active)
        {
            _activePollers.Add(this);
        }
    }

    public override void OnDelete()
    {
        base.OnDelete();

        Active = false;
    }
}

[SerializationGenerator(1, false)]
public partial class ShardPollOption
{
    private int _lineBreaks = -1;

    [SerializableField(1)]
    private IPAddress[] _voters;

    public ShardPollOption() => _voters = [];

    public ShardPollOption(string title)
    {
        _title = title;
        _voters = [];
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _title = reader.ReadString();

        _voters = new IPAddress[reader.ReadInt()];

        for (var i = 0; i < _voters.Length; ++i)
        {
            _voters[i] = Utility.Intern(reader.ReadIPAddress());
        }
    }

    [SerializableProperty(0)]
    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            _lineBreaks = -1;
        }
    }

    public int Votes => Voters.Length;

    public bool HasAlreadyVoted(NetState ns)
    {
        if (ns == null)
        {
            return false;
        }

        var ipAddress = ns.Address;

        for (var i = 0; i < Voters.Length; ++i)
        {
            if (Voters[i].MatchClassC(ipAddress))
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
        var height = GetBreaks() * 18;

        if (height > 30)
        {
            return height;
        }

        return 30;
    }

    public int GetBreaks()
    {
        var title = _title;
        if (title == null)
        {
            return 1;
        }

        if (_lineBreaks > -1)
        {
            return _lineBreaks;
        }

        var count = 0;
        var index = -1;

        do
        {
            ++count;
            index = title.IndexOfOrdinal("<br>", index + 1);
        } while (index >= 0);

        return _lineBreaks = count;
    }
}

public class ShardPollGump : Gump
{
    private const int LabelColor32 = 0xFFFFFF;
    private readonly Mobile _from;
    private readonly ShardPoller _poller;
    private Queue<ShardPoller> _polls;

    public ShardPollGump(Mobile from, ShardPoller poller, bool editing, Queue<ShardPoller> polls) : base(50, 50)
    {
        _from = from;
        _poller = poller;
        Editing = editing;
        _polls = polls;

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

        AddHtml(22, 22, 294, 20, title.Center(LabelColor32));

        if (editing)
        {
            AddHtml(22, 22, 294, 20, Html.Color($"{totalVotes} total", LabelColor32));
            AddButton(287, 23, 0x2622, 0x2623, 2);
        }

        AddHtml(22, 50, 294, 40, poller.Title.Color(0x99CC66));

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

            var lineBreaks = option.GetBreaks();

            AddHtml(60, y - 9 * lineBreaks, 250, 18 * lineBreaks, text.Color(LabelColor32));

            y += optHeight / 2;
            y += 5;
        }

        if (editing && !isViewingResults)
        {
            AddRadio(24, y + 15 - 15, 0x25F9, 0x25FC, false, 1 + poller.Options.Length);
            AddHtml(60, y + 15 - 9, 250, 18, "Create new option.".Color(0x99CC66));
        }

        AddButton(314, height - 73, 247, 248, 1);
        AddButton(314, height - 47, 242, 241, 0);
    }

    public bool Editing { get; }

    public void QueuePoll(ShardPoller poller)
    {
        _polls ??= new Queue<ShardPoller>(4);
        _polls.Enqueue(poller);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_polls?.Count > 0)
        {
            var shardPoller = _polls.Dequeue();

            if (shardPoller != null)
            {
                Timer.StartTimer(
                    TimeSpan.FromSeconds(1.0),
                    () => _from.SendGump(new ShardPollGump(_from, shardPoller, false, _polls))
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

            if (switched >= 0 && switched < _poller.Options.Length)
            {
                opt = _poller.Options[switched];
            }

            if (opt == null && !Editing)
            {
                return;
            }

            if (Editing)
            {
                if (!_poller.Active)
                {
                    if (opt == null)
                    {
                        _from.SendMessage("Enter a title for the option. Escape to cancel.");
                    }
                    else
                    {
                        _from.SendMessage("Enter a title for the option. Escape to cancel. Use \"DEL\" to delete.");
                    }

                    _from.Prompt = new ShardPollPrompt(_poller, opt);
                }
                else
                {
                    _from.SendMessage("You may not edit an active poll. Deactivate it first.");
                    _from.SendGump(new ShardPollGump(_from, _poller, Editing, _polls));
                }
            }
            else
            {
                if (!_poller.Active)
                {
                    _from.SendMessage("The poll has been deactivated.");
                }
                else if (_poller.HasAlreadyVoted(sender))
                {
                    _from.SendMessage("You have already voted on this poll.");
                }
                else
                {
                    _poller.AddVote(sender, opt);
                }
            }
        }
        else if (info.ButtonID == 2 && Editing)
        {
            _from.SendGump(new ShardPollGump(_from, _poller, Editing, _polls));
            _from.SendGump(new PropertiesGump(_from, _poller));
        }
    }
}

public partial class ShardPollPrompt : Prompt
{
    private readonly ShardPollOption _option;
    private readonly ShardPoller _poller;

    public ShardPollPrompt(ShardPoller poller, ShardPollOption opt)
    {
        _poller = poller;
        _option = opt;
    }

    public override void OnCancel(Mobile from)
    {
        from.SendGump(new ShardPollGump(from, _poller, true, null));
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

    public static string UrlToHref(string text) => text == null ? null : UrlHrefRegex().Replace(text, UrlRegex_Match);

    public override void OnResponse(Mobile from, string text)
    {
        if (_poller.Active)
        {
            from.SendMessage("You may not edit an active poll. Deactivate it first.");
        }
        else if (text == "DEL")
        {
            if (_option != null)
            {
                _poller.RemoveOption(_option);
            }
        }
        else
        {
            text = UrlToHref(text);

            if (_option == null)
            {
                _poller.AddOption(new ShardPollOption(text));
            }
            else
            {
                _option.Title = text;
            }
        }

        from.SendGump(new ShardPollGump(from, _poller, true, null));
    }

    [GeneratedRegex(@"\[url(?:=(.*?))?\](.*?)\[/url\]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UrlHrefRegex();
}
