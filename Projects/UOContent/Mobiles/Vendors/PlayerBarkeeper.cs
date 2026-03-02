using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Multis;
using Server.Network;
using Server.Prompts;

namespace Server.Mobiles;

public class ChangeRumorMessagePrompt : Prompt
{
    private readonly PlayerBarkeeper _barkeeper;
    private readonly int _rumorIndex;

    public ChangeRumorMessagePrompt(PlayerBarkeeper barkeeper, int rumorIndex)
    {
        _barkeeper = barkeeper;
        _rumorIndex = rumorIndex;
    }

    public override void OnCancel(Mobile from)
    {
        OnResponse(from, "");
    }

    public override void OnResponse(Mobile from, string text)
    {
        if (text.Length > 130)
        {
            text = text[..130];
        }

        _barkeeper.EndChangeRumor(from, _rumorIndex, text);
    }
}

public class ChangeRumorKeywordPrompt : Prompt
{
    private readonly PlayerBarkeeper _barkeeper;
    private readonly int _rumorIndex;

    public ChangeRumorKeywordPrompt(PlayerBarkeeper barkeeper, int rumorIndex)
    {
        _barkeeper = barkeeper;
        _rumorIndex = rumorIndex;
    }

    public override void OnCancel(Mobile from)
    {
        OnResponse(from, "");
    }

    public override void OnResponse(Mobile from, string text)
    {
        if (text.Length > 130)
        {
            text = text[..130];
        }

        _barkeeper.EndChangeKeyword(from, _rumorIndex, text);
    }
}

public class ChangeTipMessagePrompt : Prompt
{
    private readonly PlayerBarkeeper _barkeeper;

    public ChangeTipMessagePrompt(PlayerBarkeeper barkeeper) => _barkeeper = barkeeper;

    public override void OnCancel(Mobile from)
    {
        OnResponse(from, "");
    }

    public override void OnResponse(Mobile from, string text)
    {
        if (text.Length > 130)
        {
            text = text[..130];
        }

        _barkeeper.EndChangeTip(from, text);
    }
}

[SerializationGenerator(0)]
public partial class BarkeeperRumor
{
    [DirtyTrackingEntity]
    private PlayerBarkeeper _barkeeper;

    [SerializableField(0)]
    private string _message;

    [SerializableField(1)]
    private string _keyword;

    public BarkeeperRumor(PlayerBarkeeper barkeeper, string message, string keyword)
    {
        _message = message;
        _keyword = keyword;
        _barkeeper = barkeeper;
    }

    public BarkeeperRumor(PlayerBarkeeper barkeeper) => _barkeeper = barkeeper;
}

public class ManageBarkeeperEntry : ContextMenuEntry
{
    public ManageBarkeeperEntry() : base(6151, 12)
    {
    }

    public override void OnClick(Mobile from, IEntity target)
    {
        (target as PlayerBarkeeper)?.BeginManagement(from);
    }
}

[SerializationGenerator(2, false)]
public partial class PlayerBarkeeper : BaseVendor
{
    public static readonly BarkeeperRumor[] EmptyRumors = new BarkeeperRumor[RumorCount];
    private const int RumorCount = 3;
    private readonly List<SBInfo> _sbInfos = [];

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _owner;

    [SerializableField(2)]
    private BarkeeperRumor[] _rumors;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _tipMessage;

    private Timer _newsTimer;

    public PlayerBarkeeper(Mobile owner, BaseHouse house) : base("the barkeeper")
    {
        Owner = owner;
        House = house;

        LoadSBInfo();
    }

    [SerializableProperty(0)]
    public BaseHouse House
    {
        get => _house;
        set
        {
            _house?.PlayerBarkeepers.Remove(this);
            value?.PlayerBarkeepers.Add(this);

            _house = value;
            this.MarkDirty();
        }
    }

    public override bool IsActiveBuyer => false;
    public override bool IsActiveSeller => _sbInfos.Count > 0;

    public override bool DisallowAllMoves => true;
    public override bool NoHouseRestrictions => true;

    public override VendorShoeType ShoeType => Utility.RandomBool() ? VendorShoeType.ThighBoots : VendorShoeType.Boots;
    protected override List<SBInfo> SBInfos => _sbInfos;

    public override bool GetGender() => false;

    public override void InitOutfit()
    {
        base.InitOutfit();

        AddItem(new HalfApron(Utility.RandomBrightHue()));

        var pack = Backpack;

        pack?.Delete();
    }

    public override void InitBody()
    {
        base.InitBody();

        Hue = Body == 0x340 || Body == 0x402 ? 0 : 0x83F4; // hue is not random
        Backpack?.Delete();
    }

    public override bool HandlesOnSpeech(Mobile from) => InRange(from, 3) || base.HandlesOnSpeech(from);

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

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        House = null;
    }

    public override bool OnBeforeDeath()
    {
        if (!base.OnBeforeDeath())
        {
            return false;
        }

        var shoes = FindItemOnLayer(Layer.Shoes);

        if (shoes is Sandals)
        {
            shoes.Hue = 0;
        }

        return true;
    }

    public override void OnSpeech(SpeechEventArgs e)
    {
        base.OnSpeech(e);

        if (e.Handled || !InRange(e.Mobile, 3))
        {
            return;
        }

        if (_newsTimer == null && e.HasKeyword(0x30)) // *news*
        {
            var tce = GlobalTownCrierEntryList.Instance.GetRandomEntry();

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

        if (_rumors == null)
        {
            return;
        }

        for (var i = 0; i < _rumors.Length; ++i)
        {
            var rumor = _rumors[i];

            var keyword = rumor?.Keyword;

            if (keyword == null || (keyword = keyword.Trim()).Length == 0)
            {
                continue;
            }

            if (keyword.InsensitiveEquals(e.Speech))
            {
                var message = rumor.Message;

                if (message == null || (message = message.Trim()).Length == 0)
                {
                    continue;
                }

                PublicOverheadMessage(MessageType.Regular, 0x3B2, false, message);
            }
        }
    }

    public override bool CheckGold(Mobile from, Item dropped)
    {
        if (dropped is not Gold g)
        {
            return false;
        }

        if (g.Amount > 50)
        {
            PrivateOverheadMessage(
                MessageType.Regular,
                0x3B2,
                false,
                "I cannot accept so large a tip!",
                from.NetState
            );
        }
        else
        {
            var tip = TipMessage;

            if (tip == null || (tip = tip.Trim()).Length == 0)
            {
                PrivateOverheadMessage(
                    MessageType.Regular,
                    0x3B2,
                    false,
                    "It would not be fair of me to take your money and not offer you information in return.",
                    from.NetState
                );
            }
            else
            {
                Direction = GetDirectionTo(from);
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, tip, from.NetState);

                g.Delete();
                return true;
            }
        }

        return false;
    }

    public bool IsOwner(Mobile from) =>
        from?.Deleted == false && !Deleted && (from.AccessLevel > AccessLevel.GameMaster || Owner == from);

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (IsOwner(from) && from.InLOS(this))
        {
            list.Add(new ManageBarkeeperEntry());
        }
    }

    public void BeginManagement(Mobile from)
    {
        if (!IsOwner(from))
        {
            return;
        }

        from.SendGump(new BarkeeperGump(this));
    }

    public void Dismiss()
    {
        Delete();
    }

    public void BeginChangeRumor(Mobile from, int index)
    {
        if (index is < 0 or >= RumorCount)
        {
            return;
        }

        from.Prompt = new ChangeRumorMessagePrompt(this, index);
        PrivateOverheadMessage(
            MessageType.Regular,
            0x3B2,
            false,
            "Say what news you would like me to tell our guests.",
            from.NetState
        );
    }

    public void EndChangeRumor(Mobile from, int index, string text)
    {
        if (index is < 0 or >= RumorCount)
        {
            return;
        }

        Rumors ??= new BarkeeperRumor[RumorCount];

        if (Rumors[index] == null)
        {
            Rumors[index] = new BarkeeperRumor(this, text, null);
        }
        else
        {
            Rumors[index].Message = text;
        }

        this.MarkDirty();

        from.Prompt = new ChangeRumorKeywordPrompt(this, index);

        // What keyword should a guest say to me to get this news?
        PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1079267, from.NetState);
    }

    public void EndChangeKeyword(Mobile from, int index, string text)
    {
        if (index is < 0 or >= RumorCount)
        {
            return;
        }

        Rumors ??= new BarkeeperRumor[RumorCount];

        if (Rumors[index] == null)
        {
            Rumors[index] = new BarkeeperRumor(this, null, text);
        }
        else
        {
            Rumors[index].Keyword = text;
        }

        this.MarkDirty();

        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, "I'll pass on the message.", from.NetState);
    }

    public void RemoveRumor(int index)
    {
        if (index is >= 0 and < RumorCount && _rumors != null)
        {
            _rumors[index] = null;
            this.MarkDirty();
        }
    }

    public void BeginChangeTip(Mobile from)
    {
        from.Prompt = new ChangeTipMessagePrompt(this);

        // Say what you want me to tell guests when they give me a good tip.
        PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1079268, from.NetState);
    }

    public void EndChangeTip(Mobile from, string text)
    {
        TipMessage = text;
        PrivateOverheadMessage(
            MessageType.Regular,
            0x3B2,
            false,
            "I'll say that to anyone who gives me a good tip.",
            from.NetState
        );
    }

    public void RemoveTip() => TipMessage = null;

    public void BeginChangeTitle(Mobile from) => from.SendGump(new BarkeeperTitleGump(this));

    public void EndChangeTitle(string title)
    {
        Title = title;
        LoadSBInfo();
    }

    public void CancelChangeTitle(Mobile from)
    {
        from.SendGump(new BarkeeperGump(this));
    }

    public void BeginChangeAppearance(Mobile from)
    {
        from.SendGump(new PlayerVendorCustomizeGump(this, from));
    }

    public void ChangeGender()
    {
        Female = !Female;

        if (Female)
        {
            Body = 401;
            Name = NameList.RandomName("female");

            FacialHairItemID = 0;
        }
        else
        {
            Body = 400;
            Name = NameList.RandomName("male");
        }
    }

    public override void InitSBInfo()
    {
        if (Title is not ("the waiter" or "the barkeeper" or "the baker" or "the innkeeper" or "the chef"))
        {
            _sbInfos.Clear();
            return;
        }

        if (_sbInfos.Count == 0)
        {
            _sbInfos.Add(new SBPlayerBarkeeper());
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _house = (BaseHouse)reader.ReadEntity<Item>();
        _owner = reader.ReadEntity<Mobile>();

        _rumors = new BarkeeperRumor[reader.ReadEncodedInt()];

        for (var i = 0; i < _rumors.Length; ++i)
        {
            if (reader.ReadBool())
            {
                _rumors[i] = new BarkeeperRumor(this)
                {
                    Message = reader.ReadString(),
                    Keyword = reader.ReadString()
                };
            }
        }

        _tipMessage = reader.ReadString();
    }
}

public class BarkeeperTitleGump : StaticGump<BarkeeperTitleGump>
{
    private static readonly Entry[] _entries =
    [
        new(1076083, "the alchemist"),
        new(1077244, "the animal tamer"),
        new(1078440, "the apothecary"),
        new(1078441, "the artist"),
        new(1078442, "the baker", true),
        new(1073298, "the bard"),
        new(1076102, "the barkeep", true),
        new(1078443, "the beggar"),
        new(1073297, "the blacksmith"),
        new(1078444, "the bounty hunter"),
        new(1078446, "the brigand"),
        new(1078447, "the butler"),
        new(1060774, "the carpenter"),
        new(1078448, "the chef", true),
        new(1078449, "the commander"),
        new(1078450, "the curator"),
        new(1078451, "the drunkard"),
        new(1078452, "the farmer"),
        new(1078453, "the fisherman"),
        new(1078454, "the gambler"),
        new(1078455, "the gypsy"),
        new(1075996, "the herald"),
        new(1076107, "the herbalist"),
        new(1078465, "the hermit"),
        new(1078466, "the innkeeper", true),
        new(1078467, "the jailor"),
        new(1078468, "the jester"),
        new(1078469, "the librarian"),
        new(1073292, "the mage"),
        new(1078470, "the mercenary"),
        new(1060775, "the merchant"),
        new(1078472, "the messenger"),
        new(1076093, "the miner"),
        new(1078475, "the monk"),
        new(1078476, "the noble"),
        new(1073290, "the paladin"),
        new(1078479, "the peasant"),
        new(1078480, "the pirate"),
        new(1078481, "the prisoner"),
        new(1078482, "the prophet"),
        new(1078484, "the ranger"),
        new(1078487, "the sage"),
        new(1078488, "the sailor"),
        new(1078489, "the scholar"),
        new(1060773, "the scribe"),
        new(1078490, "the sentry"),
        new(1060795, "the servant"),
        new(1078491, "the shepherd"),
        new(1078492, "the soothsayer"),
        new(1078493, "the stoic"),
        new(1078494, "the storyteller"),
        new(1076134, "the tailor"),
        new(1076096, "the thief"),
        new(1076137, "the tinker"),
        new(1076097, "the town crier"),
        new(1073291, "the treasure hunter"),
        new(1076112, "the waiter", true),
        new(1077242, "the warrior"),
        new(1078496, "the watchman"),
        new(1078495, null) // No Title
    ];

    private static readonly int _pageCount = (_entries.Length + 19) / 20;

    private readonly PlayerBarkeeper _barkeeper;

    public override bool Singleton => true;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        RenderBackground(ref builder);

        for (var i = 0; i < _pageCount; ++i)
        {
            RenderPage(ref builder, i);
        }
    }

    public BarkeeperTitleGump(PlayerBarkeeper barkeeper) : base(0, 0) => _barkeeper = barkeeper;

    private static void RenderBackground(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(30, 40, 585, 410, 5054);

        builder.AddImage(30, 40, 9251);
        builder.AddImage(180, 40, 9251);
        builder.AddImage(30, 40, 9253);
        builder.AddImage(30, 130, 9253);
        builder.AddImage(598, 40, 9255);
        builder.AddImage(598, 130, 9255);
        builder.AddImage(30, 433, 9257);
        builder.AddImage(180, 433, 9257);
        builder.AddImage(30, 40, 9250);
        builder.AddImage(598, 40, 9252);
        builder.AddImage(598, 433, 9258);
        builder.AddImage(30, 433, 9256);

        builder.AddItem(30, 40, 6816);
        builder.AddItem(30, 125, 6817);
        builder.AddItem(30, 233, 6817);
        builder.AddItem(30, 341, 6817);
        builder.AddItem(580, 40, 6814);
        builder.AddItem(588, 125, 6815);
        builder.AddItem(588, 233, 6815);
        builder.AddItem(588, 341, 6815);

        builder.AddImage(560, 20, 1417);
        builder.AddItem(580, 44, 4033);

        builder.AddBackground(183, 25, 280, 30, 5054);

        builder.AddImage(180, 25, 10460);
        builder.AddImage(434, 25, 10460);

        builder.AddHtmlLocalized(223, 32, 200, 40, 1078366); // BARKEEP CUSTOMIZATION MENU
        builder.AddBackground(243, 433, 150, 30, 5054);

        builder.AddImage(240, 433, 10460);
        builder.AddImage(375, 433, 10460);

        builder.AddImage(80, 398, 2151);
        builder.AddItem(72, 406, 2543);

        builder.AddHtmlLocalized(110, 412, 180, 25, 1078445); // sells food and drink
    }

    private static void RenderPage(ref StaticGumpBuilder builder, int page)
    {
        var currentPage = page + 1;
        builder.AddPage(currentPage);

        if (_pageCount == 3 && currentPage is >= 1 and <= 3)
        {
            var pageCliloc = currentPage switch
            {
                1 => 1078439, // Page 1 of 3
                2 => 1078464, // Page 2 of 3
                3 => 1078483, // Page 3 of 3
            };

            builder.AddHtmlLocalized(430, 70, 180, 25, pageCliloc);
        }
        else
        {
            // Page ~1_CUR~ of ~2_MAX~
            builder.AddHtmlLocalized(430, 70, 180, 25, 1153561, $"{currentPage}\t{_pageCount}", 0x7FFF);
        }

        for (int count = 0, i = page * 20; count < 20 && i < _entries.Length; ++count, ++i)
        {
            var entry = _entries[i];

            var xOffset = Math.DivRem(count, 10, out var yOffset);

            builder.AddButton(80 + xOffset * 260, 100 + yOffset * 30, 4005, 4007, 2 + i);
            builder.AddHtmlLocalized(
                120 + xOffset * 260,
                100 + yOffset * 30,
                entry.Vendor ? 148 : 180,
                25,
                entry.Description,
                true
            );

            if (entry.Vendor)
            {
                builder.AddImage(270 + xOffset * 260, 98 + yOffset * 30, 2151);
                builder.AddItem(262 + xOffset * 260, 106 + yOffset * 30, 2543);
            }
        }

        builder.AddButton(340, 400, 4005, 4007, 0, GumpButtonType.Page, currentPage + 1 % _pageCount);
        builder.AddHtmlLocalized(380, 400, 180, 25, 1078456); // More Job Titles

        builder.AddButton(338, 437, 4014, 4016, 1);
        builder.AddHtmlLocalized(290, 440, 35, 40, 1005007); // Back
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_barkeeper.Deleted)
        {
            return;
        }

        var buttonID = info.ButtonID;

        if (buttonID-- <= 0)
        {
            return;
        }

        var from = sender.Mobile;

        if (buttonID-- <= 0)
        {
            _barkeeper.CancelChangeTitle(from);
            return;
        }

        if (buttonID < _entries.Length)
        {
            var entry = _entries[buttonID];
            _barkeeper.EndChangeTitle(entry.Title);
        }
    }

    private class Entry
    {
        public readonly int Description;
        public readonly string Title;
        public readonly bool Vendor;

        public Entry(int desc, string title, bool vendor = false)
        {
            Description = desc;
            Title = title;
            Vendor = vendor;
        }
    }
}

public class BarkeeperGump : DynamicGump
{
    private readonly PlayerBarkeeper _barkeeper;

    public override bool Singleton => true;

    public static void DisplayTo(Mobile from, PlayerBarkeeper barkeeper)
    {
        from.CloseGump<BarkeeperTitleGump>();
        from.SendGump(new BarkeeperGump(barkeeper));
    }

    public BarkeeperGump(PlayerBarkeeper barkeeper) : base(0, 0) => _barkeeper = barkeeper;

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        RenderBackground(ref builder);
        RenderCategories(ref builder);
        RenderMessageManagement(ref builder);
        RenderDismissConfirmation(ref builder);
        RenderMessageManagement_Message_AddOrChange(ref builder);
        RenderMessageManagement_Message_Remove(ref builder);
        RenderMessageManagement_Tip_AddOrChange(ref builder);
        RenderMessageManagement_Tip_Remove(ref builder);
        RenderAppearanceCategories(ref builder);
    }

    public static void RenderBackground(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(30, 40, 585, 410, 5054);

        builder.AddImage(30, 40, 9251);
        builder.AddImage(180, 40, 9251);
        builder.AddImage(30, 40, 9253);
        builder.AddImage(30, 130, 9253);
        builder.AddImage(598, 40, 9255);
        builder.AddImage(598, 130, 9255);
        builder.AddImage(30, 433, 9257);
        builder.AddImage(180, 433, 9257);
        builder.AddImage(30, 40, 9250);
        builder.AddImage(598, 40, 9252);
        builder.AddImage(598, 433, 9258);
        builder.AddImage(30, 433, 9256);

        builder.AddItem(30, 40, 6816);
        builder.AddItem(30, 125, 6817);
        builder.AddItem(30, 233, 6817);
        builder.AddItem(30, 341, 6817);
        builder.AddItem(580, 40, 6814);
        builder.AddItem(588, 125, 6815);
        builder.AddItem(588, 233, 6815);
        builder.AddItem(588, 341, 6815);

        builder.AddBackground(183, 25, 280, 30, 5054);

        builder.AddImage(180, 25, 10460);
        builder.AddImage(434, 25, 10460);
        builder.AddImage(560, 20, 1417);

        builder.AddHtmlLocalized(223, 32, 200, 40, 1078366); // BARKEEP CUSTOMIZATION MENU
        builder.AddBackground(243, 433, 150, 30, 5054);

        builder.AddImage(240, 433, 10460);
        builder.AddImage(375, 433, 10460);
    }

    public static void RenderCategories(ref DynamicGumpBuilder builder)
    {
        builder.AddPage(1);

        builder.AddButton(130, 120, 4005, 4007, 0, GumpButtonType.Page, 2);
        builder.AddHtmlLocalized(170, 120, 200, 40, 1078352); // Message Control

        builder.AddButton(130, 200, 4005, 4007, 0, GumpButtonType.Page, 8);
        builder.AddHtmlLocalized(170, 200, 200, 40, 1078353); // Customize your barkeep

        builder.AddButton(130, 280, 4005, 4007, 0, GumpButtonType.Page, 3);
        builder.AddHtmlLocalized(170, 280, 200, 40, 1078354); // Dismiss your barkeep

        builder.AddButton(338, 437, 4014, 4016, 0);
        builder.AddHtmlLocalized(290, 440, 35, 40, 1005007); // Back

        builder.AddItem(574, 43, 5360);
    }

    public static void RenderMessageManagement(ref DynamicGumpBuilder builder)
    {
        builder.AddPage(2);

        builder.AddButton(130, 120, 4005, 4007, 0, GumpButtonType.Page, 4);
        builder.AddHtmlLocalized(170, 120, 380, 20, 1078355); // Add or change a message and keyword

        builder.AddButton(130, 200, 4005, 4007, 0, GumpButtonType.Page, 5);
        builder.AddHtmlLocalized(170, 200, 380, 20, 1078356); // Remove a message and keyword from your barkeep

        builder.AddButton(130, 280, 4005, 4007, 0, GumpButtonType.Page, 6);
        builder.AddHtmlLocalized(170, 280, 380, 20, 1078357); // Add or change your barkeeper's tip message

        builder.AddButton(130, 360, 4005, 4007, 0, GumpButtonType.Page, 7);
        builder.AddHtmlLocalized(170, 360, 380, 20, 1078358); // Delete your barkeepers tip message

        builder.AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 1);
        builder.AddHtmlLocalized(290, 440, 35, 40, 1005007); // Back

        builder.AddItem(580, 46, 4030);
    }

    public static void RenderDismissConfirmation(ref DynamicGumpBuilder builder)
    {
        builder.AddPage(3);

        builder.AddHtmlLocalized(170, 160, 380, 20, 1078359); // Are you sure you want to dismiss your barkeeper?

        builder.AddButton(205, 280, 4005, 4007, GetButtonID(0, 0));
        builder.AddHtmlLocalized(240, 280, 100, 20, 1046362); // Yes

        builder.AddButton(395, 280, 4005, 4007, 0);
        builder.AddHtmlLocalized(430, 280, 100, 20, 1046363); // No

        builder.AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 1);
        builder.AddHtmlLocalized(290, 440, 35, 40, 1005007); // Back

        builder.AddItem(574, 43, 5360);
        builder.AddItem(584, 34, 6579);
    }

    public void RenderMessageManagement_Message_AddOrChange(ref DynamicGumpBuilder builder)
    {
        builder.AddPage(4);

        builder.AddHtmlLocalized(250, 60, 500, 25, 1078360); // Add or change a message

        var rumors = _barkeeper.Rumors ?? PlayerBarkeeper.EmptyRumors;

        for (var i = 0; i < rumors.Length; ++i)
        {
            var rumor = rumors[i];

            builder.AddHtml(100, 70 + i * 120, 50, 20, "Message");
            builder.AddHtml(100, 90 + i * 120, 450, 40, rumor?.Message ?? "No current message", background: true);
            builder.AddHtmlLocalized(100, 130 + i * 120, 50, 20, 1078361); // Keyword
            builder.AddHtml(100, 150 + i * 120, 450, 40, rumor?.Keyword ?? "None", background: true);

            builder.AddButton(60, 90 + i * 120, 4005, 4007, GetButtonID(1, i));
        }

        builder.AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 2);
        builder.AddHtmlLocalized(290, 440, 35, 40, 1005007); // Back

        builder.AddItem(580, 46, 4030);
    }

    public void RenderMessageManagement_Message_Remove(ref DynamicGumpBuilder builder)
    {
        builder.AddPage(5);

        builder.AddHtmlLocalized(190, 60, 500, 25, 1078362); // Choose the message you would like to remove

        var rumors = _barkeeper.Rumors ?? PlayerBarkeeper.EmptyRumors;

        for (var i = 0; i < rumors.Length; ++i)
        {
            var rumor = rumors[i];

            builder.AddHtml(100, 70 + i * 120, 50, 20, "Message");
            builder.AddHtml(100, 90 + i * 120, 450, 40, rumor?.Message ?? "No current message", background: true);
            builder.AddHtmlLocalized(100, 130 + i * 120, 50, 20, 1078361); // Keyword
            builder.AddHtml(100, 150 + i * 120, 450, 40, rumor?.Keyword ?? "None", background: true);

            builder.AddButton(60, 90 + i * 120, 4005, 4007, GetButtonID(2, i));
        }

        builder.AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 2);
        builder.AddHtmlLocalized(290, 440, 35, 40, 1005007); // Back

        builder.AddItem(580, 46, 4030);
    }

    private static int GetButtonID(int type, int index) => 1 + index * 6 + type;

    private void RenderMessageManagement_Tip_AddOrChange(ref DynamicGumpBuilder builder)
    {
        builder.AddPage(6);

        builder.AddHtmlLocalized(250, 95, 500, 20, 1078363); // Change this tip message
        builder.AddHtml(100, 190, 50, 20, "Message");
        builder.AddHtml(100, 210, 450, 40, _barkeeper.TipMessage ?? "No current message", background: true);

        builder.AddButton(60, 210, 4005, 4007, GetButtonID(3, 0));

        builder.AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 2);
        builder.AddHtmlLocalized(290, 440, 35, 40, 1005007); // Back

        builder.AddItem(580, 46, 4030);
    }

    private void RenderMessageManagement_Tip_Remove(ref DynamicGumpBuilder builder)
    {
        builder.AddPage(7);

        builder.AddHtmlLocalized(250, 95, 500, 20, 1078364); // Remove this tip message
        builder.AddHtml(100, 190, 50, 20, "Message");
        builder.AddHtml(100, 210, 450, 40, _barkeeper.TipMessage ?? "No current message", background: true);

        builder.AddButton(60, 210, 4005, 4007, GetButtonID(4, 0));

        builder.AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 2);
        builder.AddHtmlLocalized(290, 440, 35, 40, 1005007); // Back

        builder.AddItem(580, 46, 4030);
    }

    private void RenderAppearanceCategories(ref DynamicGumpBuilder builder)
    {
        builder.AddPage(8);

        builder.AddButton(130, 120, 4005, 4007, GetButtonID(5, 0));
        builder.AddHtml(170, 120, 120, 20, "Title");

        if ((int)_barkeeper.Body is not 0x340 and not 0x402)
        {
            builder.AddButton(130, 200, 4005, 4007, GetButtonID(5, 1));
            builder.AddHtmlLocalized(170, 200, 120, 20, 1077829); // Appearance

            builder.AddButton(130, 280, 4005, 4007, GetButtonID(5, 2));
            builder.AddHtmlLocalized(170, 280, 120, 20, 1078365); // Male / Female

            builder.AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 1);
            builder.AddHtmlLocalized(290, 440, 35, 40, 1005007); // Back
        }

        builder.AddItem(580, 44, 4033);
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;
        if (!_barkeeper.IsOwner(from))
        {
            return;
        }

        var index = info.ButtonID - 1;

        if (index < 0)
        {
            return;
        }

        index = Math.DivRem(index, 6, out var type);

        switch (type)
        {
            case 0: // Controls
                {
                    switch (index)
                    {
                        case 0: // Dismiss
                            {
                                _barkeeper.Dismiss();
                                break;
                            }
                    }

                    break;
                }
            case 1: // Change message
                {
                    _barkeeper.BeginChangeRumor(from, index);
                    break;
                }
            case 2: // Remove message
                {
                    _barkeeper.RemoveRumor(index);
                    break;
                }
            case 3: // Change tip
                {
                    _barkeeper.BeginChangeTip(from);
                    break;
                }
            case 4: // Remove tip
                {
                    _barkeeper.RemoveTip();
                    break;
                }
            case 5: // Appearance category selection
                {
                    switch (index)
                    {
                        case 0:
                            {
                                _barkeeper.BeginChangeTitle(from);
                                break;
                            }
                        case 1:
                            {
                                _barkeeper.BeginChangeAppearance(from);
                                break;
                            }
                        case 2:
                            {
                                _barkeeper.ChangeGender();
                                break;
                            }
                    }

                    break;
                }
        }
    }
}
