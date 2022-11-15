using System;
using System.Runtime.CompilerServices;
using ModernUO.Serialization;
using Server.Collections;
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

    [SerializationGenerator(0, false)]
    [Flippable(0x1E5E, 0x1E5F)]
    public partial class BulletinBoard : BaseBulletinBoard
    {
        [Constructible]
        public BulletinBoard() : base(0x1E5E)
        {
        }
    }

    [SerializationGenerator(0, false)]
    public abstract partial class BaseBulletinBoard : Item
    {
        [SerializableField(0)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private string _boardName;

        public BaseBulletinBoard(int itemID) : base(itemID)
        {
            BoardName = "bulletin board";
            Movable = false;
        }

        [AfterDeserialization(false)]
        public virtual void Cleanup()
        {
            var items = Items;

            for (var i = items.Count - 1; i >= 0; --i)
            {
                if (i >= items.Count)
                {
                    continue;
                }

                if (items[i] is not BulletinMessage msg || msg.Deleted)
                {
                    continue;
                }

                if (msg.Thread == null && BulletinBoardSystem.CheckDeletionTime(msg.LastPostTime))
                {
                    msg.Delete();
                    var queue = PooledRefQueue<Item>.Create();
                    var thread = msg;

                    do
                    {
                        BFSDelete(thread, ref queue);
                        if (queue.Count > 0)
                        {
                            thread = (BulletinMessage)queue.Dequeue();
                        }
                    } while (thread != null);
                    queue.Dispose();
                }
            }
        }

        private void BFSDelete(BulletinMessage msg, ref PooledRefQueue<Item> queue)
        {
            var items = Items;

            for (var i = items.Count - 1; i >= 0; --i)
            {
                if (i >= items.Count)
                {
                    continue;
                }

                if (items[i] is not BulletinMessage check || check.Deleted)
                {
                    continue;
                }

                if (check.Thread == msg)
                {
                    check.Delete();
                    queue.Enqueue(check);
                }
            }
        }

        public virtual bool GetLastPostTime(Mobile poster, bool onlyCheckRoot, out DateTime lastPostTime)
        {
            lastPostTime = DateTime.MinValue;
            var items = Items;
            var wasSet = false;

            for (var i = 0; i < items.Count; ++i)
            {
                if (items[i] is not BulletinMessage msg || msg.Poster != poster)
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
            if (!CheckRange(from))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                return;
            }

            Cleanup();

            var state = from.NetState;

            state.SendBBDisplayBoard(this);
            state.SendContainerContent(from, this);
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
    }
}
