using System;
using System.Collections.Generic;
using System.Text;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Prompts;

namespace Server.Mobiles;

public interface ITownCrierEntryList
{
    List<TownCrierEntry> Entries { get; }
    TownCrierEntry GetRandomEntry();
    TownCrierEntry AddEntry(string[] lines, TimeSpan duration);
    void RemoveEntry(TownCrierEntry entry);
}

public class GlobalTownCrierEntryList : ITownCrierEntryList
{
    private static GlobalTownCrierEntryList m_Instance;

    public static GlobalTownCrierEntryList Instance => m_Instance ??= new GlobalTownCrierEntryList();

    public bool IsEmpty => Entries == null || Entries.Count == 0;

    public List<TownCrierEntry> Entries { get; private set; }

    public TownCrierEntry GetRandomEntry()
    {
        for (var i = (Entries?.Count ?? 0) - 1; i >= 0; --i)
        {
            if (i >= Entries!.Count)
            {
                continue;
            }

            var tce = Entries[i];

            if (tce.Expired)
            {
                RemoveEntry(tce);
            }
        }

        return Entries?.RandomElement();
    }

    public TownCrierEntry AddEntry(string[] lines, TimeSpan duration)
    {
        Entries ??= new List<TownCrierEntry>();

        var tce = new TownCrierEntry(lines, duration);

        Entries.Add(tce);

        var instances = TownCrier.Instances;

        for (var i = 0; i < instances.Count; ++i)
        {
            instances[i].ForceBeginAutoShout();
        }

        return tce;
    }

    public void RemoveEntry(TownCrierEntry tce)
    {
        if (Entries == null)
        {
            return;
        }

        Entries.Remove(tce);

        if (Entries.Count == 0)
        {
            Entries = null;
        }
    }

    public static void Initialize()
    {
        CommandSystem.Register("TownCriers", AccessLevel.GameMaster, TownCriers_OnCommand);
    }

    [Usage("TownCriers"), Description("Manages the global town crier list.")]
    public static void TownCriers_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendGump(new TownCrierGump(e.Mobile, Instance));
    }
}

public class TownCrierEntry
{
    public TownCrierEntry(string[] lines, TimeSpan duration)
    {
        Lines = lines;

        ExpireTime = Core.Now + duration.Clamp(TimeSpan.Zero, TimeSpan.FromDays(365));
    }

    public string[] Lines { get; }

    public DateTime ExpireTime { get; }

    public bool Expired => Core.Now >= ExpireTime;
}

public class TownCrierDurationPrompt : Prompt
{
    private readonly ITownCrierEntryList m_Owner;

    public TownCrierDurationPrompt(ITownCrierEntryList owner) => m_Owner = owner;

    public override void OnResponse(Mobile from, string text)
    {
        if (!TimeSpan.TryParse(text, out var ts))
        {
            from.SendMessage("Value was not properly formatted. Use: <hours:minutes:seconds>");
            from.SendGump(new TownCrierGump(from, m_Owner));
            return;
        }

        if (ts < TimeSpan.Zero)
        {
            ts = TimeSpan.Zero;
        }

        from.SendMessage($"Duration set to: {ts}");
        from.SendMessage("Enter the first line to shout:");

        from.Prompt = new TownCrierLinesPrompt(m_Owner, null, new List<string>(), ts);
    }

    public override void OnCancel(Mobile from)
    {
        from.SendLocalizedMessage(502980); // Message entry cancelled.
        from.SendGump(new TownCrierGump(from, m_Owner));
    }
}

public class TownCrierLinesPrompt : Prompt
{
    private readonly TimeSpan m_Duration;
    private readonly TownCrierEntry m_Entry;
    private readonly List<string> m_Lines;
    private readonly ITownCrierEntryList m_Owner;

    public TownCrierLinesPrompt(ITownCrierEntryList owner, TownCrierEntry entry, List<string> lines, TimeSpan duration)
    {
        m_Owner = owner;
        m_Entry = entry;
        m_Lines = lines;
        m_Duration = duration;
    }

    public override void OnResponse(Mobile from, string text)
    {
        m_Lines.Add(text);

        from.SendMessage("Enter the next line to shout, or press <ESC> if the message is finished.");
        from.Prompt = new TownCrierLinesPrompt(m_Owner, m_Entry, m_Lines, m_Duration);
    }

    public override void OnCancel(Mobile from)
    {
        if (m_Entry != null)
        {
            m_Owner.RemoveEntry(m_Entry);
        }

        if (m_Lines.Count > 0)
        {
            m_Owner.AddEntry(m_Lines.ToArray(), m_Duration);
            from.SendMessage("Message has been set.");
        }
        else
        {
            if (m_Entry != null)
            {
                from.SendMessage("Message deleted.");
            }
            else
            {
                from.SendLocalizedMessage(502980); // Message entry cancelled.
            }
        }

        from.SendGump(new TownCrierGump(from, m_Owner));
    }
}

public class TownCrierGump : Gump
{
    private readonly Mobile m_From;
    private readonly ITownCrierEntryList m_Owner;

    public TownCrierGump(Mobile from, ITownCrierEntryList owner) : base(50, 50)
    {
        m_From = from;
        m_Owner = owner;

        from.CloseGump<TownCrierGump>();

        AddPage(0);

        var entries = owner.Entries;

        owner.GetRandomEntry(); // force expiration checks

        var count = entries?.Count ?? 0;

        AddImageTiled(0, 0, 300, 38 + (count == 0 ? 20 : count * 85), 0xA40);
        AddAlphaRegion(1, 1, 298, 36 + (count == 0 ? 20 : count * 85));

        AddHtml(8, 8, 300 - 8 - 30, 20, "<basefont color=#FFFFFF><center>TOWN CRIER MESSAGES</center></basefont>");

        AddButton(300 - 8 - 30, 8, 0xFAB, 0xFAD, 1);

        if (count == 0)
        {
            AddHtml(8, 30, 284, 20, "<basefont color=#FFFFFF>The crier has no news.</basefont>");
        }
        else
        {
            for (var i = 0; i < entries!.Count; ++i)
            {
                var tce = entries[i];

                var toExpire = Utility.Max(tce.ExpireTime - Core.Now, TimeSpan.Zero);

                var sb = new StringBuilder();

                sb.Append("[Expires: ");

                if (toExpire.TotalHours >= 1)
                {
                    sb.Append((int)toExpire.TotalHours);
                    sb.Append(':');
                    sb.Append(toExpire.Minutes.ToString("D2"));
                }
                else
                {
                    sb.Append(toExpire.Minutes);
                }

                sb.Append(':');
                sb.Append(toExpire.Seconds.ToString("D2"));

                sb.Append("] ");

                for (var j = 0; j < tce.Lines.Length; ++j)
                {
                    if (j > 0)
                    {
                        sb.Append("<br>");
                    }

                    sb.Append(tce.Lines[j]);
                }

                AddHtml(8, 35 + i * 85, 254, 80, sb.ToString(), true, true);

                AddButton(300 - 8 - 26, 35 + i * 85, 0x15E1, 0x15E5, 2 + i);
            }
        }
    }

    public override void OnResponse(NetState sender, RelayInfo info)
    {
        if (info.ButtonID == 1)
        {
            m_From.SendMessage("Enter the duration for the new message. Format: <hours:minutes:seconds>");
            m_From.Prompt = new TownCrierDurationPrompt(m_Owner);
        }
        else if (info.ButtonID > 1)
        {
            var entries = m_Owner.Entries;
            var index = info.ButtonID - 2;

            if (index < entries?.Count)
            {
                var tce = entries[index];
                var ts = Utility.Max(tce.ExpireTime - Core.Now, TimeSpan.Zero);

                m_From.SendMessage($"Editing entry #{index + 1}.");
                m_From.SendMessage("Enter the first line to shout:");
                m_From.Prompt = new TownCrierLinesPrompt(m_Owner, tce, new List<string>(), ts);
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class TownCrier : Mobile, ITownCrierEntryList
{
    private Timer _autoShoutTimer;
    private Timer _newsTimer;

    [Constructible]
    public TownCrier()
    {
        Instances.Add(this);

        InitStats(100, 100, 25);

        Title = "the town crier";
        Hue = Race.Human.RandomSkinHue();

        if (!Core.AOS)
        {
            NameHue = 0x35;
        }

        if (Female = Utility.RandomBool())
        {
            Body = 0x191;
            Name = NameList.RandomName("female");
        }
        else
        {
            Body = 0x190;
            Name = NameList.RandomName("male");
        }

        AddItem(new FancyShirt(Utility.RandomBlueHue()));

        var skirt = Utility.Random(2) switch
        {
            0 => (Item)new Skirt(),
            1 => new Kilt(),
            _ => new Kilt()
        };

        skirt.Hue = Utility.RandomGreenHue();

        AddItem(skirt);

        AddItem(new FeatheredHat(Utility.RandomGreenHue()));

        var boots = Utility.Random(2) switch
        {
            0 => (Item)new Boots(),
            1 => new ThighBoots(),
            _ => new ThighBoots()
        };

        AddItem(boots);

        Utility.AssignRandomHair(this);
    }

    public TownCrier(Serial serial) : base(serial)
    {
        Instances.Add(this);
    }

    public static List<TownCrier> Instances { get; } = new();

    public List<TownCrierEntry> Entries { get; private set; }

    public TownCrierEntry GetRandomEntry()
    {
        var count = Entries?.Count ?? 0;
        for (var i = count - 1; i >= 0; --i)
        {
            if (i >= Entries!.Count)
            {
                continue;
            }

            var tce = Entries[i];

            if (tce.Expired)
            {
                RemoveEntry(tce);
            }
        }

        var entry = GlobalTownCrierEntryList.Instance.GetRandomEntry();
        if (count > 0 && entry != null && Utility.RandomBool())
        {
            return entry;
        }

        return Entries?.RandomElement();
    }

    public TownCrierEntry AddEntry(string[] lines, TimeSpan duration)
    {
        Entries ??= new List<TownCrierEntry>();

        var tce = new TownCrierEntry(lines, duration);

        Entries.Add(tce);

        ForceBeginAutoShout();

        return tce;
    }

    public void RemoveEntry(TownCrierEntry tce)
    {
        if (Entries == null)
        {
            return;
        }

        Entries.Remove(tce);

        if (Entries.Count == 0)
        {
            Entries = null;
        }

        if (Entries == null && GlobalTownCrierEntryList.Instance.IsEmpty)
        {
            _autoShoutTimer?.Stop();
            _autoShoutTimer = null;
        }
    }

    public void ForceBeginAutoShout()
    {
        _autoShoutTimer ??= Timer.DelayCall(TimeSpan.FromSeconds(5.0), TimeSpan.FromMinutes(1.0), AutoShout_Callback);
    }

    private void AutoShout_Callback()
    {
        var tce = GetRandomEntry();

        if (tce == null)
        {
            _autoShoutTimer.Stop();
            _autoShoutTimer = null;
        }
        else if (_newsTimer == null)
        {
            _newsTimer = Timer.DelayCall(
                TimeSpan.FromSeconds(1.0),
                TimeSpan.FromSeconds(3.0),
                tce.Lines.Length,
                () => ShoutNews_Callback(tce)
            );

            PublicOverheadMessage(MessageType.Regular, 0x3B2, 502976); // Hear ye! Hear ye!
        }
    }

    private void ShoutNews_Callback(TownCrierEntry tce)
    {
        var index = _newsTimer.Index;
        if (index >= tce.Lines.Length)
        {
            _newsTimer.Stop();
            _newsTimer = null;
        }
        else
        {
            PublicOverheadMessage(MessageType.Regular, 0x3B2, false, tce.Lines[index]);
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.AccessLevel >= AccessLevel.GameMaster)
        {
            from.SendGump(new TownCrierGump(from, this));
        }
        else
        {
            base.OnDoubleClick(from);
        }
    }

    public override bool HandlesOnSpeech(Mobile from) => _newsTimer == null && from.Alive && InRange(from, 12);

    public override void OnSpeech(SpeechEventArgs e)
    {
        if (_newsTimer == null && e.HasKeyword(0x30) && e.Mobile.Alive && InRange(e.Mobile, 12)) // *news*
        {
            Direction = GetDirectionTo(e.Mobile);

            var tce = GetRandomEntry();

            if (tce == null)
            {
                PublicOverheadMessage(MessageType.Regular, 0x3B2, 1005643); // I have no news at this time.
            }
            else
            {
                _newsTimer = Timer.DelayCall(
                    TimeSpan.FromSeconds(1.0),
                    TimeSpan.FromSeconds(3.0),
                    tce.Lines.Length,
                    () => ShoutNews_Callback(tce)
                );

                PublicOverheadMessage(MessageType.Regular, 0x3B2, 502978); // Some of the latest news!
            }
        }
    }

    public override bool CanBeDamaged() => false;

    public override void OnDelete()
    {
        Instances.Remove(this);
        base.OnDelete();
    }
}
