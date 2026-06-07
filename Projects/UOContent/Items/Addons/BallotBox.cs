using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Prompts;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class BallotBox : AddonComponent
    {
        public static readonly int MaxTopicLines = 6;

        [Constructible]
        public BallotBox() : base(0x9A8)
        {
            _topic = Array.Empty<string>();
            _yes = new List<Mobile>();
            _no = new List<Mobile>();
        }

        public override int LabelNumber => 1041006; // a ballot box

        [SerializableField(0, setter: "private")]
        private string[] _topic;

        [Tidy]
        [SerializableField(1, setter: "private")]
        private List<Mobile> _yes;

        [Tidy]
        [SerializableField(2, setter: "private")]
        private List<Mobile> _no;

        public void AddLineToTopic(string line)
        {
            if (Topic.Length >= MaxTopicLines)
            {
                return;
            }

            var newTopic = new string[Topic.Length + 1];
            Topic.CopyTo(newTopic, 0);
            newTopic[Topic.Length] = line;

            Topic = newTopic;

            ClearVotes();
        }

        public void ClearVotes()
        {
            ClearYes();
            ClearNo();
        }

        public bool IsOwner(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                return true;
            }

            var house = BaseHouse.FindHouseAt(this);
            return house?.IsOwner(from) == true;
        }

        public bool HasVoted(Mobile from) => Yes.Contains(from) || No.Contains(from);

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            SendLocalizedMessageTo(from, 500369); // I'm a ballot box, not a container!
            return false;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
            else
            {
                BallotBoxGump.DisplayTo(from, this, IsOwner(from));
            }
        }

        private class BallotBoxGump : DynamicGump
        {
            private readonly BallotBox _box;
            private readonly bool _isOwner;

            public override bool Singleton => true;

            private BallotBoxGump(BallotBox box, bool isOwner) : base(110, 70)
            {
                _box = box;
                _isOwner = isOwner;
            }

            public static void DisplayTo(Mobile from, BallotBox box, bool isOwner)
            {
                if (from?.NetState == null || box?.Deleted != false)
                {
                    return;
                }

                from.SendGump(new BallotBoxGump(box, isOwner));
            }

            protected override void BuildLayout(ref DynamicGumpBuilder builder)
            {
                builder.AddPage();
                builder.AddBackground(0, 0, 400, 350, 0xA28);

                if (_isOwner)
                {
                    builder.AddHtmlLocalized(0, 15, 400, 35, 1011000); // <center>Ballot Box Owner's Menu</center>
                }
                else
                {
                    builder.AddHtmlLocalized(0, 15, 400, 35, 1011001); // <center>Ballot Box -- Vote Here!</center>
                }

                builder.AddHtmlLocalized(0, 50, 400, 35, 1011002); // <center>Topic</center>

                var lineCount = _box.Topic.Length;
                builder.AddBackground(25, 90, 350, Math.Max(20 * lineCount, 20), 0x1400);

                for (var i = 0; i < lineCount; i++)
                {
                    var line = _box.Topic[i];

                    if (!string.IsNullOrEmpty(line))
                    {
                        builder.AddLabelCropped(30, 90 + i * 20, 340, 20, 0x3E3, line);
                    }
                }

                var yesCount = _box.Yes.Count;
                var noCount = _box.No.Count;
                var totalVotes = yesCount + noCount;

                builder.AddHtmlLocalized(0, 215, 400, 35, 1011003); // <center>votes</center>

                if (!_isOwner)
                {
                    builder.AddButton(20, 240, 0xFA5, 0xFA7, 3);
                }

                builder.AddHtmlLocalized(55, 242, 25, 35, 1011004); // aye:
                builder.AddLabel(78, 242, 0x0, $"[{yesCount}]");

                if (!_isOwner)
                {
                    builder.AddButton(20, 275, 0xFA5, 0xFA7, 4);
                }

                builder.AddHtmlLocalized(55, 277, 25, 35, 1011005); // nay:
                builder.AddLabel(78, 277, 0x0, $"[{noCount}]");

                if (totalVotes > 0)
                {
                    builder.AddImageTiled(130, 242, yesCount * 225 / totalVotes, 10, 0xD6);
                    builder.AddImageTiled(130, 277, noCount * 225 / totalVotes, 10, 0xD6);
                }

                builder.AddButton(45, 305, 0xFA5, 0xFA7, 0);
                builder.AddHtmlLocalized(80, 308, 40, 35, 1011008); // done

                if (_isOwner)
                {
                    builder.AddButton(120, 305, 0xFA5, 0xFA7, 1);
                    builder.AddHtmlLocalized(155, 308, 100, 35, 1011006); // change topic

                    builder.AddButton(240, 305, 0xFA5, 0xFA7, 2);
                    builder.AddHtmlLocalized(275, 308, 300, 100, 1011007); // reset votes
                }
            }

            public override void OnResponse(NetState sender, in RelayInfo info)
            {
                if (_box.Deleted || info.ButtonID == 0)
                {
                    return;
                }

                var from = sender.Mobile;

                if (from.Map != _box.Map || !from.InRange(_box.GetWorldLocation(), 2))
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                    return;
                }

                var isOwner = _box.IsOwner(from);

                switch (info.ButtonID)
                {
                    case 1: // change topic
                        {
                            if (isOwner)
                            {
                                _box.ClearTopic();
                                _box.ClearVotes();

                                from.SendLocalizedMessage(
                                    500370,
                                    "",
                                    0x35
                                ); // Enter a line of text for your ballot, and hit ENTER. Hit ESC after the last line is entered.
                                from.Prompt = new TopicPrompt(_box);
                            }

                            break;
                        }
                    case 2: // reset votes
                        {
                            if (isOwner)
                            {
                                _box.ClearVotes();
                                from.SendLocalizedMessage(500371); // Votes zeroed out.
                            }

                            goto default;
                        }
                    case 3: // aye
                        {
                            if (!isOwner)
                            {
                                if (_box.HasVoted(from))
                                {
                                    from.SendLocalizedMessage(500374); // You have already voted on this ballot.
                                }
                                else
                                {
                                    _box.AddToYes(from);
                                    from.SendLocalizedMessage(500373); // Your vote has been registered.
                                }
                            }

                            goto default;
                        }
                    case 4: // nay
                        {
                            if (!isOwner)
                            {
                                if (_box.HasVoted(from))
                                {
                                    from.SendLocalizedMessage(500374); // You have already voted on this ballot.
                                }
                                else
                                {
                                    _box.AddToNo(from);
                                    from.SendLocalizedMessage(500373); // Your vote has been registered.
                                }
                            }

                            goto default;
                        }
                    default:
                        {
                            DisplayTo(from, _box, isOwner);
                            break;
                        }
                }
            }
        }

        private class TopicPrompt : Prompt
        {
            private readonly BallotBox _box;

            public TopicPrompt(BallotBox box) => _box = box;

            public override void OnResponse(Mobile from, string text)
            {
                if (_box.Deleted || !_box.IsOwner(from))
                {
                    return;
                }

                if (from.Map != _box.Map || !from.InRange(_box.GetWorldLocation(), 2))
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                    return;
                }

                _box.AddLineToTopic(text.TrimEnd());

                if (_box.Topic.Length < MaxTopicLines)
                {
                    from.SendLocalizedMessage(500377, "", 0x35); // Next line or ESC to finish:
                    from.Prompt = new TopicPrompt(_box);
                }
                else
                {
                    from.SendLocalizedMessage(500376, "", 0x35); // Ballot entry complete.
                    BallotBoxGump.DisplayTo(from, _box, true);
                }
            }

            public override void OnCancel(Mobile from)
            {
                if (_box.Deleted || !_box.IsOwner(from))
                {
                    return;
                }

                if (from.Map != _box.Map || !from.InRange(_box.GetWorldLocation(), 2))
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                    return;
                }

                from.SendLocalizedMessage(500376, "", 0x35); // Ballot entry complete.
                BallotBoxGump.DisplayTo(from, _box, true);
            }
        }
    }

    [SerializationGenerator(0)]
    public partial class BallotBoxAddon : BaseAddon
    {
        public BallotBoxAddon()
        {
            AddComponent(new BallotBox(), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new BallotBoxDeed();
    }

    [SerializationGenerator(0)]
    public partial class BallotBoxDeed : BaseAddonDeed
    {
        [Constructible]
        public BallotBoxDeed()
        {
        }

        public override BaseAddon Addon => new BallotBoxAddon();

        public override int LabelNumber => 1044327; // ballot box
    }
}
