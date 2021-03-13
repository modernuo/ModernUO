using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Network;

namespace Server.Items
{
    public static class BulletinBoardSystem
    {
        // Threads will be removed six hours after the last post was made
        public static TimeSpan ThreadDeletionTime { get; private set; }

        // A player may only create a thread once every two minutes
        public static TimeSpan ThreadCreateTime { get; private set; }

        // A player may only reply once every thirty seconds
        public static TimeSpan ThreadReplyTime { get; private set; }

        public static void Configure()
        {
            ThreadCreateTime = ServerConfiguration.GetOrUpdateSetting("bulletinboards.creationTimeDelay", TimeSpan.FromMinutes(2.0));
            ThreadDeletionTime = ServerConfiguration.GetOrUpdateSetting("bulletinboards.expireDuration", TimeSpan.FromHours(6.0));
            ThreadReplyTime = ServerConfiguration.GetOrUpdateSetting("bulletinboards.replyDelay", TimeSpan.FromSeconds(30.0));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckCreateTime(DateTime time) => time + ThreadCreateTime < Core.Now;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool CheckDeletionTime(DateTime time) => time + ThreadDeletionTime < Core.Now;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckReplyTime(DateTime time) => time + ThreadReplyTime < Core.Now;
    }

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

                if (msg.Thread == null && BulletinBoardSystem.CheckDeletionTime(msg.LastPostTime))
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

                state.SendBBDisplayBoard(this);
                state.SendContainerContent(from, this);
            }
            else
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
        }

        public virtual bool CheckRange(Mobile from) =>
            from.AccessLevel >= AccessLevel.GameMaster || from.Map == Map && from.InRange(GetWorldLocation(), 2);

        public void PostMessage(Mobile from, BulletinMessage thread, string subject, string[] lines)
        {
            if (thread != null)
            {
                thread.LastPostTime = Core.Now;
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
    }
}
