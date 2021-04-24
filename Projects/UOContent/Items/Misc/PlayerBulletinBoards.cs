using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Prompts;

namespace Server.Items
{
    public class PlayerBBSouth : BasePlayerBB
    {
        [Constructible]
        public PlayerBBSouth() : base(0x2311) => Weight = 15.0;

        public PlayerBBSouth(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1062421; // bulletin board (south)

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class PlayerBBEast : BasePlayerBB
    {
        [Constructible]
        public PlayerBBEast() : base(0x2312) => Weight = 15.0;

        public PlayerBBEast(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1062420; // bulletin board (east)

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public abstract class BasePlayerBB : Item, ISecurable
    {
        public BasePlayerBB(int itemID) : base(itemID)
        {
            Messages = new List<PlayerBBMessage>();
            Level = SecureLevel.Anyone;
        }

        public BasePlayerBB(Serial serial) : base(serial)
        {
        }

        public List<PlayerBBMessage> Messages { get; private set; }

        public PlayerBBMessage Greeting { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Title { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public SecureLevel Level { get; set; }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);
            SetSecureLevelEntry.AddTo(from, this, list);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);

            writer.Write((int)Level);

            writer.Write(Title);

            if (Greeting != null)
            {
                writer.Write(true);
                Greeting.Serialize(writer);
            }
            else
            {
                writer.Write(false);
            }

            writer.WriteEncodedInt(Messages.Count);

            for (var i = 0; i < Messages.Count; ++i)
            {
                Messages[i].Serialize(writer);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        Level = (SecureLevel)reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 1)
                        {
                            Level = SecureLevel.Anyone;
                        }

                        Title = reader.ReadString();

                        if (reader.ReadBool())
                        {
                            Greeting = new PlayerBBMessage(reader);
                        }

                        var count = reader.ReadEncodedInt();

                        Messages = new List<PlayerBBMessage>(count);

                        for (var i = 0; i < count; ++i)
                        {
                            Messages.Add(new PlayerBBMessage(reader));
                        }

                        break;
                    }
            }
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
            private readonly BasePlayerBB m_Board;
            private readonly bool m_Greeting;
            private readonly BaseHouse m_House;
            private readonly int m_Page;

            public PostPrompt(int page, BaseHouse house, BasePlayerBB board, bool greeting)
            {
                m_Page = page;
                m_House = house;
                m_Board = board;
                m_Greeting = greeting;
            }

            public override void OnCancel(Mobile from)
            {
                OnResponse(from, "");
            }

            public override void OnResponse(Mobile from, string text)
            {
                var page = m_Page;
                var house = m_House;
                var board = m_Board;

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

                if (m_Greeting && !house.IsOwner(from))
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

                    if (m_Greeting)
                    {
                        board.Greeting = message;
                    }
                    else
                    {
                        board.Messages.Add(message);

                        if (board.Messages.Count > 50)
                        {
                            board.Messages.RemoveAt(0);

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
            private readonly BasePlayerBB m_Board;
            private readonly BaseHouse m_House;
            private readonly int m_Page;

            public SetTitlePrompt(int page, BaseHouse house, BasePlayerBB board)
            {
                m_Page = page;
                m_House = house;
                m_Board = board;
            }

            public override void OnCancel(Mobile from)
            {
                OnResponse(from, "");
            }

            public override void OnResponse(Mobile from, string text)
            {
                var page = m_Page;
                var house = m_House;
                var board = m_Board;

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

    public class PlayerBBMessage
    {
        public PlayerBBMessage(DateTime time, Mobile poster, string message)
        {
            Time = time;
            Poster = poster;
            Message = message;
        }

        public PlayerBBMessage(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        Time = reader.ReadDateTime();
                        Poster = reader.ReadEntity<Mobile>();
                        Message = reader.ReadString();
                        break;
                    }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime Time { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Poster { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Message { get; set; }

        public void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(Time);
            writer.Write(Poster);
            writer.Write(Message);
        }
    }

    public class PlayerBBGump : Gump
    {
        private const int LabelColor = 0x7FFF;
        private const int LabelHue = 1153;
        private readonly BasePlayerBB m_Board;
        private readonly Mobile m_From;
        private readonly BaseHouse m_House;
        private readonly int m_Page;

        public PlayerBBGump(Mobile from, BaseHouse house, BasePlayerBB board, int page) : base(50, 10)
        {
            from.CloseGump<PlayerBBGump>();

            m_Page = page;
            m_From = from;
            m_House = house;
            m_Board = board;

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
            var page = m_Page;
            var from = m_From;
            var house = m_House;
            var board = m_Board;

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
                                    from.SendLocalizedMessage(
                                        1060750
                                    ); // That person is a friend, co-owner, or owner of this house, and therefore cannot be banished!
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
                                board.Messages.RemoveAt(page - 1);
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
}
