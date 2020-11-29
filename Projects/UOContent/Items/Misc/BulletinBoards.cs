using System;
using System.Collections.Generic;
using Server.Network;
using Server.Targeting;

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

    public struct BulletinEquip
    {
        public int itemID;
        public int hue;

        public BulletinEquip(int itemID, int hue)
        {
            this.itemID = itemID;
            this.hue = hue;
        }
    }

    public class BulletinMessage : Item
    {
        public BulletinMessage(Mobile poster, BulletinMessage thread, string subject, string[] lines) : base(0xEB0)
        {
            Movable = false;

            Poster = poster;
            Subject = subject;
            Time = DateTime.UtcNow;
            LastPostTime = Time;
            Thread = thread;
            PostedName = Poster.Name;
            PostedBody = Poster.Body;
            PostedHue = Poster.Hue;
            Lines = lines;

            var list = new List<BulletinEquip>();

            for (var i = 0; i < poster.Items.Count; ++i)
            {
                var item = poster.Items[i];

                if (item.Layer >= Layer.OneHanded && item.Layer <= Layer.Mount)
                {
                    list.Add(new BulletinEquip(item.ItemID, item.Hue));
                }
            }

            PostedEquip = list.ToArray();
        }

        public BulletinMessage(Serial serial) : base(serial)
        {
        }

        public Mobile Poster { get; private set; }

        public BulletinMessage Thread { get; private set; }

        public string Subject { get; private set; }

        public DateTime Time { get; private set; }

        public DateTime LastPostTime { get; set; }

        public string PostedName { get; private set; }

        public int PostedBody { get; private set; }

        public int PostedHue { get; private set; }

        public BulletinEquip[] PostedEquip { get; private set; }

        public string[] Lines { get; private set; }

        public string GetTimeAsString() => Time.ToString("MMM dd, yyyy");

        public override bool CheckTarget(Mobile from, Target targ, object targeted) => false;

        public override bool IsAccessibleTo(Mobile check) => false;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(Poster);
            writer.Write(Subject);
            writer.Write(Time);
            writer.Write(LastPostTime);
            writer.Write(Thread != null);
            writer.Write(Thread);
            writer.Write(PostedName);
            writer.Write(PostedBody);
            writer.Write(PostedHue);

            writer.Write(PostedEquip.Length);

            for (var i = 0; i < PostedEquip.Length; ++i)
            {
                writer.Write(PostedEquip[i].itemID);
                writer.Write(PostedEquip[i].hue);
            }

            writer.Write(Lines.Length);

            for (var i = 0; i < Lines.Length; ++i)
            {
                writer.Write(Lines[i]);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                case 0:
                    {
                        Poster = reader.ReadMobile();
                        Subject = reader.ReadString();
                        Time = reader.ReadDateTime();
                        LastPostTime = reader.ReadDateTime();
                        var hasThread = reader.ReadBool();
                        Thread = reader.ReadItem() as BulletinMessage;
                        PostedName = reader.ReadString();
                        PostedBody = reader.ReadInt();
                        PostedHue = reader.ReadInt();

                        PostedEquip = new BulletinEquip[reader.ReadInt()];

                        for (var i = 0; i < PostedEquip.Length; ++i)
                        {
                            PostedEquip[i].itemID = reader.ReadInt();
                            PostedEquip[i].hue = reader.ReadInt();
                        }

                        Lines = new string[reader.ReadInt()];

                        for (var i = 0; i < Lines.Length; ++i)
                        {
                            Lines[i] = reader.ReadString();
                        }

                        if (hasThread && Thread == null)
                        {
                            Delete();
                        }

                        if (version == 0)
                        {
                            ValidationQueue<BulletinMessage>.Add(this);
                        }

                        break;
                    }
            }
        }

        public void Validate()
        {
            if ((Parent as BulletinBoard)?.Items.Contains(this) == false)
            {
                Delete();
            }
        }
    }

    public class BBDisplayBoard : Packet
    {
        public BBDisplayBoard(BaseBulletinBoard board) : base(0x71)
        {
            EnsureCapacity(38);

            var buffer = Utility.UTF8.GetBytes(board.BoardName ?? "");

            Stream.Write((byte)0x00);   // PacketID
            Stream.Write(board.Serial); // Bulletin board serial

            // Bulletin board name
            if (buffer.Length >= 29)
            {
                Stream.Write(buffer, 0, 29);
                Stream.Write((byte)0);
            }
            else
            {
                Stream.Write(buffer, 0, buffer.Length);
                Stream.Fill(30 - buffer.Length);
            }
        }
    }

    public class BBMessageHeader : Packet
    {
        public BBMessageHeader(BaseBulletinBoard board, BulletinMessage msg) : base(0x71)
        {
            var poster = SafeString(msg.PostedName);
            var subject = SafeString(msg.Subject);
            var time = SafeString(msg.GetTimeAsString());

            EnsureCapacity(22 + poster.Length + subject.Length + time.Length);

            Stream.Write((byte)0x01);   // PacketID
            Stream.Write(board.Serial); // Bulletin board serial
            Stream.Write(msg.Serial);   // Message serial

            var thread = msg.Thread;

            if (thread == null)
            {
                Stream.Write(0); // Thread serial--root
            }
            else
            {
                Stream.Write(thread.Serial); // Thread serial--parent
            }

            WriteString(poster);
            WriteString(subject);
            WriteString(time);
        }

        public void WriteString(string v)
        {
            var buffer = Utility.UTF8.GetBytes(v);
            var len = buffer.Length + 1;

            if (len > 255)
            {
                len = 255;
            }

            Stream.Write((byte)len);
            Stream.Write(buffer, 0, len - 1);
            Stream.Write((byte)0);
        }

        public string SafeString(string v) => v ?? string.Empty;
    }

    public class BBMessageContent : Packet
    {
        public BBMessageContent(BaseBulletinBoard board, BulletinMessage msg) : base(0x71)
        {
            var poster = SafeString(msg.PostedName);
            var subject = SafeString(msg.Subject);
            var time = SafeString(msg.GetTimeAsString());

            EnsureCapacity(22 + poster.Length + subject.Length + time.Length);

            Stream.Write((byte)0x02);   // PacketID
            Stream.Write(board.Serial); // Bulletin board serial
            Stream.Write(msg.Serial);   // Message serial

            WriteString(poster);
            WriteString(subject);
            WriteString(time);

            Stream.Write((short)msg.PostedBody);
            Stream.Write((short)msg.PostedHue);

            var len = msg.PostedEquip.Length;

            if (len > 255)
            {
                len = 255;
            }

            Stream.Write((byte)len);

            for (var i = 0; i < len; ++i)
            {
                var eq = msg.PostedEquip[i];

                Stream.Write((short)eq.itemID);
                Stream.Write((short)eq.hue);
            }

            len = msg.Lines.Length;

            if (len > 255)
            {
                len = 255;
            }

            Stream.Write((byte)len);

            for (var i = 0; i < len; ++i)
            {
                WriteString(msg.Lines[i], true);
            }
        }

        public void WriteString(string v)
        {
            WriteString(v, false);
        }

        public void WriteString(string v, bool padding)
        {
            var buffer = Utility.UTF8.GetBytes(v);
            var tail = padding ? 2 : 1;
            var len = buffer.Length + tail;

            if (len > 255)
            {
                len = 255;
            }

            Stream.Write((byte)len);
            Stream.Write(buffer, 0, len - tail);

            if (padding)
            {
                Stream.Write((short)0); // padding compensates for a client bug
            }
            else
            {
                Stream.Write((byte)0);
            }
        }

        public string SafeString(string v)
        {
            if (v == null)
            {
                return string.Empty;
            }

            return v;
        }
    }
}
