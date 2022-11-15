using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.ContextMenus;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Prompts;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PlayerBBSouth : BasePlayerBB
{
    [Constructible]
    public PlayerBBSouth() : base(0x2311) => Weight = 15.0;

    public override int LabelNumber => 1062421; // bulletin board (south)
}

[SerializationGenerator(0, false)]
public partial class PlayerBBEast : BasePlayerBB
{
    [Constructible]
    public PlayerBBEast() : base(0x2312) => Weight = 15.0;

    public override int LabelNumber => 1062420; // bulletin board (east)
}

[SerializationGenerator(0, false)]
public abstract partial class BasePlayerBB : Item, ISecurable
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SecureLevel _level;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _title;

    [CanBeNull]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private PlayerBBMessage _greeting;

    [SerializableField(3)]
    private List<PlayerBBMessage> _messages;

    public BasePlayerBB(int itemID) : base(itemID)
    {
        _messages = new List<PlayerBBMessage>();
        _level = SecureLevel.Anyone;
    }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, list);
        SetSecureLevelEntry.AddTo(from, this, list);
    }

    public static bool CheckAccess(BaseHouse house, Mobile from)
    {
        if (house.Public || !house.IsAosRules)
        {
            return !house.IsBanned(from);
        }

        return house.HasAccess(from);
    }

    public override void OnDoubleClick(Mobile from)
    {
        var house = BaseHouse.FindHouseAt(this);

        if (house?.HasLockedDownItem(this) != true)
        {
            from.SendLocalizedMessage(1062396); // This bulletin board must be locked down in a house to be usable.
        }
        else if (!from.InRange(GetWorldLocation(), 2) || !from.InLOS(this))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
        else if (CheckAccess(house, from))
        {
            from.SendGump(new PlayerBBGump(from, house, this, 0));
        }
    }

    public class PostPrompt : Prompt
    {
        private BasePlayerBB _board;
        private bool _greeting;
        private BaseHouse _house;
        private int _page;

        public PostPrompt(int page, BaseHouse house, BasePlayerBB board, bool greeting)
        {
            _page = page;
            _house = house;
            _board = board;
            _greeting = greeting;
        }

        public override void OnCancel(Mobile from)
        {
            OnResponse(from, "");
        }

        public override void OnResponse(Mobile from, string text)
        {
            var page = _page;
            var house = _house;
            var board = _board;

            if (house?.HasLockedDownItem(board) != true)
            {
                from.SendLocalizedMessage(1062396); // This bulletin board must be locked down in a house to be usable.
                return;
            }

            if (!from.InRange(board.GetWorldLocation(), 2) || !from.InLOS(board))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                return;
            }

            if (!CheckAccess(house, from))
            {
                from.SendLocalizedMessage(1062398); // You are not allowed to post to this bulletin board.
                return;
            }

            if (_greeting && !house.IsOwner(from))
            {
                return;
            }

            text = text.Trim();

            if (text.Length > 255)
            {
                text = text[..255];
            }

            if (text.Length > 0)
            {
                var message = new PlayerBBMessage(Core.Now, from, text);

                if (_greeting)
                {
                    board.Greeting = message;
                }
                else
                {
                    board.Add(board.Messages, message);

                    if (board.Messages.Count > 50)
                    {
                        board.RemoveAt(board.Messages, 0);

                        if (page > 0)
                        {
                            --page;
                        }
                    }
                }
            }

            from.SendGump(new PlayerBBGump(from, house, board, page));
        }
    }

    public class SetTitlePrompt : Prompt
    {
        private BasePlayerBB _board;
        private BaseHouse _house;
        private int _page;

        public SetTitlePrompt(int page, BaseHouse house, BasePlayerBB board)
        {
            _page = page;
            _house = house;
            _board = board;
        }

        public override void OnCancel(Mobile from)
        {
            OnResponse(from, "");
        }

        public override void OnResponse(Mobile from, string text)
        {
            var page = _page;
            var house = _house;
            var board = _board;

            if (house?.HasLockedDownItem(board) != true)
            {
                from.SendLocalizedMessage(1062396); // This bulletin board must be locked down in a house to be usable.
                return;
            }

            if (!from.InRange(board.GetWorldLocation(), 2) || !from.InLOS(board))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                return;
            }

            if (!CheckAccess(house, from))
            {
                from.SendLocalizedMessage(1062398); // You are not allowed to post to this bulletin board.
                return;
            }

            text = text.Trim();

            if (text.Length > 255)
            {
                text = text[..255];
            }

            if (text.Length > 0)
            {
                board.Title = text;
            }

            from.SendGump(new PlayerBBGump(from, house, board, page));
        }
    }
}

[PropertyObject]
[SerializationGenerator(0)]
public partial class PlayerBBMessage
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _time;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _poster;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _message;

    public PlayerBBMessage()
    {
    }

    public PlayerBBMessage(DateTime time, Mobile poster, string message)
    {
        _time = time;
        _poster = poster;
        _message = message;
    }
}

public class PlayerBBGump : Gump
{
    private const int LabelColor = 0x7FFF;
    private const int LabelHue = 1153;
    private BasePlayerBB _board;
    private Mobile _from;
    private BaseHouse _house;
    private int _page;

    public PlayerBBGump(Mobile from, BaseHouse house, BasePlayerBB board, int page) : base(50, 10)
    {
        from.CloseGump<PlayerBBGump>();

        _page = page;
        _from = from;
        _house = house;
        _board = board;

        AddPage(0);

        AddImage(30, 30, 5400);

        AddButton(393, 145, 2084, 2084, 4); // Scroll up
        AddButton(390, 371, 2085, 2085, 5); // Scroll down

        AddButton(32, 183, 5412, 5413, 1); // Post message

        if (house.IsOwner(from))
        {
            AddButton(63, 90, 5601, 5605, 2);
            AddHtmlLocalized(81, 89, 230, 20, 1062400, LabelColor); // Set title

            AddButton(63, 109, 5601, 5605, 3);
            AddHtmlLocalized(81, 108, 230, 20, 1062401, LabelColor); // Post greeting
        }

        var title = board.Title;

        if (title != null)
        {
            AddHtml(183, 68, 180, 23, title);
        }

        AddHtmlLocalized(385, 89, 60, 20, 1062409, LabelColor); // Post

        AddLabel(440, 89, LabelHue, page.ToString());
        AddLabel(455, 89, LabelHue, "/");
        AddLabel(470, 89, LabelHue, board.Messages.Count.ToString());

        var message = board.Greeting;

        if (page >= 1 && page <= board.Messages.Count)
        {
            message = board.Messages[page - 1];
        }

        AddImageTiled(150, 220, 240, 1, 2700); // Separator

        AddHtmlLocalized(150, 180, 100, 20, 1062405, 16715); // Posted On:
        AddHtmlLocalized(150, 200, 100, 20, 1062406, 16715); // Posted By:

        if (message != null)
        {
            AddHtml(255, 180, 150, 20, message.Time.ToString("yyyy-MM-dd HH:mm:ss"));

            var poster = message.Poster;
            var name = (poster?.Name?.Trim()).DefaultIfNullOrEmpty("Someone");

            AddHtml(255, 200, 150, 20, name);

            AddHtml(150, 240, 250, 100, message.Message ?? "");

            if (message != board.Greeting && house.IsOwner(from))
            {
                AddButton(130, 395, 1209, 1210, 6);
                AddHtmlLocalized(150, 393, 150, 20, 1062410, LabelColor); // Banish Poster

                AddButton(310, 395, 1209, 1210, 7);
                AddHtmlLocalized(330, 393, 150, 20, 1062411, LabelColor); // Delete Message
            }

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                AddButton(135, 242, 1209, 1210, 8); // Post props
            }
        }
    }

    public override void OnResponse(NetState sender, RelayInfo info)
    {
        var page = _page;
        var from = _from;
        var house = _house;
        var board = _board;

        if (house?.HasLockedDownItem(board) != true)
        {
            from.SendLocalizedMessage(1062396); // This bulletin board must be locked down in a house to be usable.
            return;
        }

        if (!from.InRange(board.GetWorldLocation(), 2) || !from.InLOS(board))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        if (!BasePlayerBB.CheckAccess(house, from))
        {
            from.SendLocalizedMessage(1062398); // You are not allowed to post to this bulletin board.
            return;
        }

        switch (info.ButtonID)
        {
            case 1: // Post message
                {
                    from.Prompt = new BasePlayerBB.PostPrompt(page, house, board, false);
                    from.SendLocalizedMessage(1062397); // Please enter your message:

                    break;
                }
            case 2: // Set title
                {
                    if (house.IsOwner(from))
                    {
                        from.Prompt = new BasePlayerBB.SetTitlePrompt(page, house, board);
                        from.SendLocalizedMessage(1062402); // Enter new title:
                    }

                    break;
                }
            case 3: // Post greeting
                {
                    if (house.IsOwner(from))
                    {
                        from.Prompt = new BasePlayerBB.PostPrompt(page, house, board, true);
                        from.SendLocalizedMessage(1062404); // Enter new greeting (this will always be the first post):
                    }

                    break;
                }
            case 4: // Scroll up
                {
                    if (page == 0)
                    {
                        page = board.Messages.Count;
                    }
                    else
                    {
                        page -= 1;
                    }

                    from.SendGump(new PlayerBBGump(from, house, board, page));

                    break;
                }
            case 5: // Scroll down
                {
                    page += 1;
                    page %= board.Messages.Count + 1;

                    from.SendGump(new PlayerBBGump(from, house, board, page));

                    break;
                }
            case 6: // Banish poster
                {
                    if (house.IsOwner(from))
                    {
                        if (page >= 1 && page <= board.Messages.Count)
                        {
                            var message = board.Messages[page - 1];
                            var poster = message.Poster;

                            if (poster == null)
                            {
                                from.SendGump(new PlayerBBGump(from, house, board, page));
                                return;
                            }

                            if (poster.AccessLevel > AccessLevel.Player && from.AccessLevel <= poster.AccessLevel)
                            {
                                from.SendLocalizedMessage(501354); // Uh oh...a bigger boot may be required.
                            }
                            else if (house.IsFriend(poster))
                            {
                                // That person is a friend, co-owner, or owner of this house, and therefore cannot be banished!
                                from.SendLocalizedMessage(1060750);
                            }
                            else if (poster is PlayerVendor)
                            {
                                from.SendLocalizedMessage(501351); // You cannot eject a vendor.
                            }
                            else if (house.Bans.Count >= BaseHouse.MaxBans)
                            {
                                from.SendLocalizedMessage(501355); // The ban limit for this house has been reached!
                            }
                            else if (house.IsBanned(poster))
                            {
                                from.SendLocalizedMessage(501356); // This person is already banned!
                            }
                            else if (poster is BaseCreature creature && creature.NoHouseRestrictions)
                            {
                                from.SendLocalizedMessage(1062040); // You cannot ban that.
                            }
                            else
                            {
                                if (!house.Bans.Contains(poster))
                                {
                                    house.Bans.Add(poster);
                                }

                                from.SendLocalizedMessage(1062417); // That person has been banned from this house.

                                if (house.IsInside(poster) && !BasePlayerBB.CheckAccess(house, poster))
                                {
                                    poster.MoveToWorld(house.BanLocation, house.Map);
                                }
                            }
                        }

                        from.SendGump(new PlayerBBGump(from, house, board, page));
                    }

                    break;
                }
            case 7: // Delete message
                {
                    if (house.IsOwner(from))
                    {
                        if (page >= 1 && page <= board.Messages.Count)
                        {
                            board.RemoveAt(board.Messages, page - 1);
                        }

                        from.SendGump(new PlayerBBGump(from, house, board, 0));
                    }

                    break;
                }
            case 8: // Post props
                {
                    if (from.AccessLevel >= AccessLevel.GameMaster)
                    {
                        var message = board.Greeting;

                        if (page >= 1 && page <= board.Messages.Count)
                        {
                            message = board.Messages[page - 1];
                        }

                        from.SendGump(new PlayerBBGump(from, house, board, page));
                        from.SendGump(new PropertiesGump(from, message));
                    }

                    break;
                }
        }
    }
}
