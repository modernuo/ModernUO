using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Multis;
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
    public const int RumorCount = 3;
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

        BarkeeperGump.DisplayTo(from, this);
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

    public void CancelChangeTitle(Mobile from) => BarkeeperGump.DisplayTo(from, this);

    public void BeginChangeAppearance(Mobile from)
    {
        PlayerVendorCustomizeGump.DisplayTo(from, this);
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
