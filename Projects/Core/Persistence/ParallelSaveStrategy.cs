/***************************************************************************
 *                          ParallelSaveStrategy.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Server.Guilds;

namespace Server
{
    public sealed class ParallelSaveStrategy : SaveStrategy, IDisposable
    {
        private readonly Queue<Item> _decayQueue;

        private readonly int processorCount;

        private Consumer[] consumers;
        private int cycle;

        private bool disposedValue; // To detect redundant calls

        private bool finished;
        private SequentialFileWriterStream guildData, guildIndex;

        private SequentialFileWriterStream itemData, itemIndex;

        private SequentialFileWriterStream mobileData, mobileIndex;

        public ParallelSaveStrategy(int processorCount)
        {
            this.processorCount = processorCount;

            _decayQueue = new Queue<Item>();
        }

        public override string Name => "Parallel";

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ParallelSaveStrategy()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        private int GetThreadCount() => processorCount - 1;

        public override void Save(bool permitBackgroundWrite)
        {
            OpenFiles();

            consumers = new Consumer[GetThreadCount()];

            for (var i = 0; i < consumers.Length; ++i) consumers[i] = new Consumer(this, 256);

            IEnumerable<ISerializable> collection = new Producer();

            foreach (var value in collection)
                while (!Enqueue(value))
                    if (!Commit())
                        Thread.Sleep(0);

            finished = true;

            SaveTypeDatabases();

            WaitHandle.WaitAll(
                Array.ConvertAll<Consumer, WaitHandle>(
                    consumers,
                    input => input.completionEvent
                )
            );

            Commit();

            CloseFiles();
        }

        public override void ProcessDecay()
        {
            while (_decayQueue.Count > 0)
            {
                var item = _decayQueue.Dequeue();

                if (item.OnDecay()) item.Delete();
            }
        }

        private void SaveTypeDatabases()
        {
            SaveTypeDatabase(World.ItemTypesPath, World.m_ItemTypes);
            SaveTypeDatabase(World.MobileTypesPath, World.m_MobileTypes);
        }

        private void SaveTypeDatabase(string path, List<Type> types)
        {
            var bfw = new BinaryFileWriter(path, false);

            bfw.Write(types.Count);

            foreach (var type in types) bfw.Write(type.FullName);

            bfw.Flush();

            bfw.Close();
        }

        private void OpenFiles()
        {
            itemData = new SequentialFileWriterStream(World.ItemDataPath);
            itemIndex = new SequentialFileWriterStream(World.ItemIndexPath);

            mobileData = new SequentialFileWriterStream(World.MobileDataPath);
            mobileIndex = new SequentialFileWriterStream(World.MobileIndexPath);

            guildData = new SequentialFileWriterStream(World.GuildDataPath);
            guildIndex = new SequentialFileWriterStream(World.GuildIndexPath);

            WriteCount(itemIndex, World.Items.Count);
            WriteCount(mobileIndex, World.Mobiles.Count);
            WriteCount(guildIndex, BaseGuild.List.Count);
        }

        private void WriteCount(SequentialFileWriterStream indexFile, int count)
        {
            var buffer = new byte[4];

            buffer[0] = (byte)count;
            buffer[1] = (byte)(count >> 8);
            buffer[2] = (byte)(count >> 16);
            buffer[3] = (byte)(count >> 24);

            indexFile.Write(buffer, 0, buffer.Length);
        }

        private void CloseFiles()
        {
            itemData.Close();
            itemIndex.Close();

            mobileData.Close();
            mobileIndex.Close();

            guildData.Close();
            guildIndex.Close();

            World.NotifyDiskWriteComplete();
        }

        private void OnSerialized(ConsumableEntry entry)
        {
            var value = entry.value;
            var writer = entry.writer;

            if (value is Item item)
                Save(item, writer);
            else if (value is Mobile mob)
                Save(mob, writer);
            else if (value is BaseGuild guild)
                Save(guild, writer);
        }

        private void Save(Item item, BinaryMemoryWriter writer)
        {
            writer.CommitTo(itemData, itemIndex, item.TypeRef, item.Serial);

            if (item.Decays && item.Parent == null && item.Map != Map.Internal &&
                DateTime.UtcNow > item.LastMoved + item.DecayTime) _decayQueue.Enqueue(item);
        }

        private void Save(Mobile mob, BinaryMemoryWriter writer)
        {
            writer.CommitTo(mobileData, mobileIndex, mob.TypeRef, mob.Serial);
        }

        private void Save(BaseGuild guild, BinaryMemoryWriter writer)
        {
            writer.CommitTo(guildData, guildIndex, 0, guild.Serial);
        }

        private bool Enqueue(ISerializable value)
        {
            for (var i = 0; i < consumers.Length; ++i)
            {
                var consumer = consumers[cycle++ % consumers.Length];

                if (consumer.tail - consumer.head < consumer.buffer.Length)
                {
                    consumer.buffer[consumer.tail % consumer.buffer.Length].value = value;
                    consumer.tail++;

                    return true;
                }
            }

            return false;
        }

        private bool Commit()
        {
            var committed = false;

            for (var i = 0; i < consumers.Length; ++i)
            {
                var consumer = consumers[i];

                while (consumer.head < consumer.done)
                {
                    OnSerialized(consumer.buffer[consumer.head % consumer.buffer.Length]);
                    consumer.head++;

                    committed = true;
                }
            }

            return committed;
        }

        public void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        private sealed class Producer : IEnumerable<ISerializable>
        {
            private readonly IEnumerable<BaseGuild> guilds;
            private readonly IEnumerable<Item> items;
            private readonly IEnumerable<Mobile> mobiles;

            public Producer()
            {
                items = World.Items.Values;
                mobiles = World.Mobiles.Values;
                guilds = BaseGuild.List.Values;
            }

            public IEnumerator<ISerializable> GetEnumerator()
            {
                foreach (var item in items) yield return item;

                foreach (var mob in mobiles) yield return mob;

                foreach (var guild in guilds) yield return guild;
            }

            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        }

        private struct ConsumableEntry
        {
            public ISerializable value;
            public BinaryMemoryWriter writer;
        }

        private sealed class Consumer
        {
            public readonly ConsumableEntry[] buffer;

            public readonly ManualResetEvent completionEvent;
            private readonly ParallelSaveStrategy owner;

            private readonly Thread thread;
            public int head, done, tail;

            public Consumer(ParallelSaveStrategy owner, int bufferSize)
            {
                this.owner = owner;

                buffer = new ConsumableEntry[bufferSize];

                for (var i = 0; i < buffer.Length; ++i) buffer[i].writer = new BinaryMemoryWriter();

                completionEvent = new ManualResetEvent(false);

                thread = new Thread(Processor);

                thread.Name = "Parallel Serialization Thread";

                thread.Start();
            }

            private void Processor()
            {
                try
                {
                    while (!owner.finished)
                    {
                        Process();
                        Thread.Sleep(0);
                    }

                    Process();

                    completionEvent.Set();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            private void Process()
            {
                ConsumableEntry entry;

                while (done < tail)
                {
                    entry = buffer[done % buffer.Length];

                    entry.value.Serialize(entry.writer);

                    ++done;
                }
            }
        }
    }
}
