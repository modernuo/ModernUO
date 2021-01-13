using System;
using System.Collections.Generic;
using Server.Network;

namespace Server.Items
{
    [Flippable(0x1E5E, 0x1E5F)]
    public class BulletinBoard : BaseBulletinBoard
    {
        [Constructible]
        public BulletinBoard() : base(0x1E5E)
        {
        }

        public BulletinBoard(Serial serial) : base(serial)
        {
        }

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

    public abstract class BaseBulletinBoard : Item
    {
        // Threads will be removed six hours after the last post was made
        private static readonly TimeSpan ThreadDeletionTime = TimeSpan.FromHours(6.0);

        // A player may only create a thread once every two minutes
        private static readonly TimeSpan ThreadCreateTime = TimeSpan.FromMinutes(2.0);

        // A player may only reply once every thirty seconds
        private static readonly TimeSpan ThreadReplyTime = TimeSpan.FromSeconds(30.0);

        public BaseBulletinBoard(int itemID) : base(itemID)
        {
            BoardName = "bulletin board";
            Movable = false;
        }

        public BaseBulletinBoard(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string BoardName { get; set; }

        public static bool CheckTime(DateTime time, TimeSpan range) => time + range < DateTime.UtcNow;

        public static string FormatTS(TimeSpan ts)
        {
            var totalSeconds = (int)ts.TotalSeconds;
            var seconds = totalSeconds % 60;
            var minutes = totalSeconds / 60;

            if (minutes != 0 && seconds != 0)
            {
                return $"{minutes} minute{(minutes == 1 ? "" : "s")} and {seconds} second{(seconds == 1 ? "" : "s")}";
            }

            if (minutes != 0)
            {
                return $"{minutes} minute{(minutes == 1 ? "" : "s")}";
            }

            return $"{seconds} second{(seconds == 1 ? "" : "s")}";
        }

        public virtual void Cleanup()
        {
            var items = Items;

            for (var i = items.Count - 1; i >= 0; --i)
            {
                if (i >= items.Count)
                {
                    continue;
                }

                if (!(items[i] is BulletinMessage msg))
                {
                    continue;
                }

                if (msg.Thread == null && CheckTime(msg.LastPostTime, ThreadDeletionTime))
                {
                    msg.Delete();
                    RecurseDelete(msg); // A root-level thread has expired
                }
            }
        }

        private void RecurseDelete(BulletinMessage msg)
        {
            var found = new List<Item>();
            var items = Items;

            for (var i = items.Count - 1; i >= 0; --i)
            {
                if (i >= items.Count)
                {
                    continue;
                }

                if (!(items[i] is BulletinMessage check))
                {
                    continue;
                }

                if (check.Thread == msg)
                {
                    check.Delete();
                    found.Add(check);
                }
            }

            for (var i = 0; i < found.Count; ++i)
            {
                RecurseDelete((BulletinMessage)found[i]);
            }
        }

        public virtual bool GetLastPostTime(Mobile poster, bool onlyCheckRoot, ref DateTime lastPostTime)
        {
            var items = Items;
            var wasSet = false;

            for (var i = 0; i < items.Count; ++i)
            {
                if (!(items[i] is BulletinMessage msg) || msg.Poster != poster)
                {
                    continue;
                }

                if (onlyCheckRoot && msg.Thread != null)
                {
                    continue;
                }

                if (msg.Time > lastPostTime)
                {
                    wasSet = true;
                    lastPostTime = msg.Time;
                }
            }

            return wasSet;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (CheckRange(from))
            {
                Cleanup();

                var state = from.NetState;

                state.Send(new BBDisplayBoard(this));
                state.SendContainerContent(from, this);
            }
            else
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
        }

        public virtual bool CheckRange(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                return true;
            }

            return from.Map == Map && from.InRange(GetWorldLocation(), 2);
        }

        public void PostMessage(Mobile from, BulletinMessage thread, string subject, string[] lines)
        {
            if (thread != null)
            {
                thread.LastPostTime = DateTime.UtcNow;
            }

            AddItem(new BulletinMessage(from, thread, subject, lines));
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(BoardName);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        BoardName = reader.ReadString();
                        break;
                    }
            }
        }

        public static void Initialize()
        {
            IncomingPackets.Register(0x71, 0, true, BBClientRequest);
        }

        public static void BBClientRequest(NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            int packetID = reader.ReadByte();

            if (!(World.FindItem(reader.ReadUInt32()) is BaseBulletinBoard board) || !board.CheckRange(from))
            {
                return;
            }

            switch (packetID)
            {
                case 3:
                    BBRequestContent(from, board, reader);
                    break;
                case 4:
                    BBRequestHeader(from, board, reader);
                    break;
                case 5:
                    BBPostMessage(from, board, reader);
                    break;
                case 6:
                    BBRemoveMessage(from, board, reader);
                    break;
            }
        }

        public static void BBRequestContent(Mobile from, BaseBulletinBoard board, CircularBufferReader reader)
        {
            if (!(World.FindItem(reader.ReadUInt32()) is BulletinMessage msg) || msg.Parent != board)
            {
                return;
            }

            from.Send(new BBMessageContent(board, msg));
        }

        public static void BBRequestHeader(Mobile from, BaseBulletinBoard board, CircularBufferReader reader)
        {
            if (!(World.FindItem(reader.ReadUInt32()) is BulletinMessage msg) || msg.Parent != board)
            {
                return;
            }

            from.Send(new BBMessageHeader(board, msg));
        }

        public static void BBPostMessage(Mobile from, BaseBulletinBoard board, CircularBufferReader reader)
        {
            var thread = World.FindItem(reader.ReadUInt32()) as BulletinMessage;

            if (thread != null && thread.Parent != board)
            {
                thread = null;
            }

            var breakout = 0;

            while (thread?.Thread != null && breakout++ < 10)
            {
                thread = thread.Thread;
            }

            var lastPostTime = DateTime.MinValue;

            if (board.GetLastPostTime(from, thread == null, ref lastPostTime))
            {
                if (!CheckTime(lastPostTime, thread == null ? ThreadCreateTime : ThreadReplyTime))
                {
                    if (thread == null)
                    {
                        from.SendMessage("You must wait {0} before creating a new thread.", FormatTS(ThreadCreateTime));
                    }
                    else
                    {
                        from.SendMessage("You must wait {0} before replying to another thread.", FormatTS(ThreadReplyTime));
                    }

                    return;
                }
            }

            var subject = reader.ReadUTF8Safe(reader.ReadByte());

            if (subject.Length == 0)
            {
                return;
            }

            var lines = new string[reader.ReadByte()];

            if (lines.Length == 0)
            {
                return;
            }

            for (var i = 0; i < lines.Length; ++i)
            {
                lines[i] = reader.ReadUTF8Safe(reader.ReadByte());
            }

            board.PostMessage(from, thread, subject, lines);
        }

        public static void BBRemoveMessage(Mobile from, BaseBulletinBoard board, CircularBufferReader reader)
        {
            if (!(World.FindItem(reader.ReadUInt32()) is BulletinMessage msg) || msg.Parent != board)
            {
                return;
            }

            if (from.AccessLevel < AccessLevel.GameMaster && msg.Poster != from)
            {
                return;
            }

            msg.Delete();
        }
    }
}
