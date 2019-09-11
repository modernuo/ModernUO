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

namespace Server.Persistence
{
  public sealed class ParallelSaveStrategy : SaveStrategy
  {
    private Queue<Item> _decayQueue;

    private Consumer[] consumers;
    private int cycle;

    private bool finished;
    private SequentialFileWriter guildData, guildIndex;

    private SequentialFileWriter itemData, itemIndex;

    private SequentialFileWriter mobileData, mobileIndex;

    private int processorCount;

    public ParallelSaveStrategy(int processorCount)
    {
      this.processorCount = processorCount;

      _decayQueue = new Queue<Item>();
    }

    public override string Name => "Parallel";

    private int GetThreadCount() => processorCount - 1;

    public override void Save(bool permitBackgroundWrite)
    {
      OpenFiles();

      consumers = new Consumer[GetThreadCount()];

      for (int i = 0; i < consumers.Length; ++i) consumers[i] = new Consumer(this, 256);

      IEnumerable<ISerializable> collection = new Producer();

      foreach (ISerializable value in collection)
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
        Item item = _decayQueue.Dequeue();

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
      BinaryFileWriter bfw = new BinaryFileWriter(path, false);

      bfw.Write(types.Count);

      foreach (Type type in types) bfw.Write(type.FullName);

      bfw.Flush();

      bfw.Close();
    }

    private void OpenFiles()
    {
      itemData = new SequentialFileWriter(World.ItemDataPath);
      itemIndex = new SequentialFileWriter(World.ItemIndexPath);

      mobileData = new SequentialFileWriter(World.MobileDataPath);
      mobileIndex = new SequentialFileWriter(World.MobileIndexPath);

      guildData = new SequentialFileWriter(World.GuildDataPath);
      guildIndex = new SequentialFileWriter(World.GuildIndexPath);

      WriteCount(itemIndex, World.Items.Count);
      WriteCount(mobileIndex, World.Mobiles.Count);
      WriteCount(guildIndex, BaseGuild.List.Count);
    }

    private void WriteCount(SequentialFileWriter indexFile, int count)
    {
      byte[] buffer = new byte[4];

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
      ISerializable value = entry.value;
      BinaryMemoryWriter writer = entry.writer;

      if (value is Item item)
        Save(item, writer);
      else if (value is Mobile mob)
        Save(mob, writer);
      else if (value is BaseGuild guild)
        Save(guild, writer);
    }

    private void Save(Item item, BinaryMemoryWriter writer)
    {
      writer.CommitTo(itemData, itemIndex, item.m_TypeRef, item.Serial);

      if (item.Decays && item.Parent == null && item.Map != Map.Internal &&
          DateTime.UtcNow > item.LastMoved + item.DecayTime) _decayQueue.Enqueue(item);
    }

    private void Save(Mobile mob, BinaryMemoryWriter writer)
    {
      writer.CommitTo(mobileData, mobileIndex, mob.m_TypeRef, mob.Serial);
    }

    private void Save(BaseGuild guild, BinaryMemoryWriter writer)
    {
      writer.CommitTo(guildData, guildIndex, 0, guild.Id);
    }

    private bool Enqueue(ISerializable value)
    {
      for (int i = 0; i < consumers.Length; ++i)
      {
        Consumer consumer = consumers[cycle++ % consumers.Length];

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
      bool committed = false;

      for (int i = 0; i < consumers.Length; ++i)
      {
        Consumer consumer = consumers[i];

        while (consumer.head < consumer.done)
        {
          OnSerialized(consumer.buffer[consumer.head % consumer.buffer.Length]);
          consumer.head++;

          committed = true;
        }
      }

      return committed;
    }

    private sealed class Producer : IEnumerable<ISerializable>
    {
      private IEnumerable<BaseGuild> guilds;
      private IEnumerable<Item> items;
      private IEnumerable<Mobile> mobiles;

      public Producer()
      {
        items = World.Items.Values;
        mobiles = World.Mobiles.Values;
        guilds = BaseGuild.List.Values;
      }

      public IEnumerator<ISerializable> GetEnumerator()
      {
        foreach (Item item in items) yield return item;

        foreach (Mobile mob in mobiles) yield return mob;

        foreach (BaseGuild guild in guilds) yield return guild;
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
      public ConsumableEntry[] buffer;

      public ManualResetEvent completionEvent;
      public int head, done, tail;
      private ParallelSaveStrategy owner;

      private Thread thread;

      public Consumer(ParallelSaveStrategy owner, int bufferSize)
      {
        this.owner = owner;

        buffer = new ConsumableEntry[bufferSize];

        for (int i = 0; i < buffer.Length; ++i) buffer[i].writer = new BinaryMemoryWriter();

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