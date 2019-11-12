/***************************************************************************
 *                             Serialization.cs
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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using Server.Guilds;

namespace Server
{
  public sealed class AsyncWriter : IGenericWriter
  {
    private int BufferSize;
    private BinaryWriter m_Bin;
    private bool m_Closed;
    private FileStream m_File;

    private long m_LastPos, m_CurPos;

    private MemoryStream m_Mem;
    private Thread m_WorkerThread;

    private Queue<MemoryStream> m_WriteQueue;
    private bool PrefixStrings;

    public AsyncWriter(string filename, bool prefix)
      : this(filename, 1048576, prefix) //1 mb buffer
    {
    }

    public AsyncWriter(string filename, int buffSize, bool prefix)
    {
      PrefixStrings = prefix;
      m_Closed = false;
      m_WriteQueue = new Queue<MemoryStream>();
      BufferSize = buffSize;

      m_File = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
      m_Mem = new MemoryStream(BufferSize + 1024);
      m_Bin = new BinaryWriter(m_Mem, Utility.UTF8WithEncoding);
    }

    public static int ThreadCount{ get; private set; }

    public MemoryStream MemStream
    {
      get => m_Mem;
      set
      {
        if (m_Mem.Length > 0)
          Enqueue(m_Mem);

        m_Mem = value;
        m_Bin = new BinaryWriter(m_Mem, Utility.UTF8WithEncoding);
        m_LastPos = 0;
        m_CurPos = m_Mem.Length;
        m_Mem.Seek(0, SeekOrigin.End);
      }
    }

    public long Position => m_CurPos;

    private void Enqueue(MemoryStream mem)
    {
      lock (m_WriteQueue)
      {
        m_WriteQueue.Enqueue(mem);
      }

      if (m_WorkerThread.IsAlive != true)
      {
        m_WorkerThread = new Thread(new WorkerThread(this).Worker) { Priority = ThreadPriority.BelowNormal };
        m_WorkerThread.Start();
      }
    }

    private void OnWrite()
    {
      long curlen = m_Mem.Length;
      m_CurPos += curlen - m_LastPos;
      m_LastPos = curlen;
      if (curlen >= BufferSize)
      {
        Enqueue(m_Mem);
        m_Mem = new MemoryStream(BufferSize + 1024);
        m_Bin = new BinaryWriter(m_Mem, Utility.UTF8WithEncoding);
        m_LastPos = 0;
      }
    }

    public void Close()
    {
      Enqueue(m_Mem);
      m_Closed = true;
    }

    public void Write(IPAddress value)
    {
      m_Bin.Write(Utility.GetLongAddressValue(value));
      OnWrite();
    }

    public void Write(string value)
    {
      if (PrefixStrings)
      {
        if (value == null)
        {
          m_Bin.Write((byte)0);
        }
        else
        {
          m_Bin.Write((byte)1);
          m_Bin.Write(value);
        }
      }
      else
      {
        m_Bin.Write(value);
      }

      OnWrite();
    }

    public void WriteDeltaTime(DateTime value)
    {
      long ticks = value.Ticks;
      long now = DateTime.UtcNow.Ticks;

      TimeSpan d;

      try
      {
        d = new TimeSpan(ticks - now);
      }
      catch
      {
        d = TimeSpan.MaxValue;
      }

      Write(d);
    }

    public void Write(DateTime value)
    {
      m_Bin.Write(value.Ticks);
      OnWrite();
    }

    public void Write(DateTimeOffset value)
    {
      m_Bin.Write(value.Ticks);
      m_Bin.Write(value.Offset.Ticks);
      OnWrite();
    }

    public void Write(TimeSpan value)
    {
      m_Bin.Write(value.Ticks);
      OnWrite();
    }

    public void Write(decimal value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public void Write(long value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public void Write(ulong value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public void WriteEncodedInt(int value)
    {
      uint v = (uint)value;

      while (v >= 0x80)
      {
        m_Bin.Write((byte)(v | 0x80));
        v >>= 7;
      }

      m_Bin.Write((byte)v);
      OnWrite();
    }

    public void Write(int value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public void Write(uint value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public void Write(short value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public void Write(ushort value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public void Write(double value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public void Write(float value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public void Write(char value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public void Write(byte value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public void Write(byte[] value)
    {
      Write(value, value.Length);
    }

    public void Write(byte[] value, int length)
    {
      m_Bin.Write(value, 0, length);
      OnWrite();
    }
    public void Write(sbyte value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public void Write(bool value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public void Write(Point3D value)
    {
      Write(value.m_X);
      Write(value.m_Y);
      Write(value.m_Z);
    }

    public void Write(Point2D value)
    {
      Write(value.m_X);
      Write(value.m_Y);
    }

    public void Write(Rectangle2D value)
    {
      Write(value.Start);
      Write(value.End);
    }

    public void Write(Rectangle3D value)
    {
      Write(value.Start);
      Write(value.End);
    }

    public void Write(Map value)
    {
      if (value != null)
        Write((byte)value.MapIndex);
      else
        Write((byte)0xFF);
    }

    public void Write(Race value)
    {
      if (value != null)
        Write((byte)value.RaceIndex);
      else
        Write((byte)0xFF);
    }

    public void WriteEntity(IEntity value)
    {
      if (value?.Deleted != false)
        Write(Serial.MinusOne);
      else
        Write(value.Serial);
    }

    public void Write(Item value)
    {
      if (value?.Deleted != false)
        Write(Serial.MinusOne);
      else
        Write(value.Serial);
    }

    public void Write(Mobile value)
    {
      if (value?.Deleted != false)
        Write(Serial.MinusOne);
      else
        Write(value.Serial);
    }

    public void Write(BaseGuild value)
    {
      if (value == null)
        Write(0);
      else
        Write(value.Id);
    }

    public void WriteItem<T>(T value) where T : Item
    {
      Write(value);
    }

    public void WriteMobile<T>(T value) where T : Mobile
    {
      Write(value);
    }

    public void WriteGuild<T>(T value) where T : BaseGuild
    {
      Write(value);
    }

    public void Write(List<Item> list)
    {
      Write(list, false);
    }

    public void Write(List<Item> list, bool tidy)
    {
      if (tidy)
        for (int i = 0; i < list.Count;)
          if (list[i].Deleted)
            list.RemoveAt(i);
          else
            ++i;

      Write(list.Count);

      for (int i = 0; i < list.Count; ++i)
        Write(list[i]);
    }

    public void WriteItemList<T>(List<T> list) where T : Item
    {
      WriteItemList(list, false);
    }

    public void WriteItemList<T>(List<T> list, bool tidy) where T : Item
    {
      if (tidy)
        for (int i = 0; i < list.Count;)
          if (list[i].Deleted)
            list.RemoveAt(i);
          else
            ++i;

      Write(list.Count);

      for (int i = 0; i < list.Count; ++i)
        Write(list[i]);
    }

    public void Write(HashSet<Item> set)
    {
      Write(set, false);
    }

    public void Write(HashSet<Item> set, bool tidy)
    {
      if (tidy) set.RemoveWhere(item => item.Deleted);

      Write(set.Count);

      foreach (Item item in set) Write(item);
    }

    public void WriteItemSet<T>(HashSet<T> set) where T : Item
    {
      WriteItemSet(set, false);
    }

    public void WriteItemSet<T>(HashSet<T> set, bool tidy) where T : Item
    {
      if (tidy) set.RemoveWhere(item => item.Deleted);

      Write(set.Count);

      foreach (T item in set) Write(item);
    }

    public void Write(List<Mobile> list)
    {
      Write(list, false);
    }

    public void Write(List<Mobile> list, bool tidy)
    {
      if (tidy)
        for (int i = 0; i < list.Count;)
          if (list[i].Deleted)
            list.RemoveAt(i);
          else
            ++i;

      Write(list.Count);

      for (int i = 0; i < list.Count; ++i)
        Write(list[i]);
    }

    public void WriteMobileList<T>(List<T> list) where T : Mobile
    {
      WriteMobileList(list, false);
    }

    public void WriteMobileList<T>(List<T> list, bool tidy) where T : Mobile
    {
      if (tidy)
        for (int i = 0; i < list.Count;)
          if (list[i].Deleted)
            list.RemoveAt(i);
          else
            ++i;

      Write(list.Count);

      for (int i = 0; i < list.Count; ++i)
        Write(list[i]);
    }

    public void Write(HashSet<Mobile> set)
    {
      Write(set, false);
    }

    public void Write(HashSet<Mobile> set, bool tidy)
    {
      if (tidy) set.RemoveWhere(mobile => mobile.Deleted);

      Write(set.Count);

      foreach (Mobile mob in set) Write(mob);
    }

    public void WriteMobileSet<T>(HashSet<T> set) where T : Mobile
    {
      WriteMobileSet(set, false);
    }

    public void WriteMobileSet<T>(HashSet<T> set, bool tidy) where T : Mobile
    {
      if (tidy) set.RemoveWhere(mob => mob.Deleted);

      Write(set.Count);

      foreach (T mob in set) Write(mob);
    }

    public void Write(List<BaseGuild> list)
    {
      Write(list, false);
    }

    public void Write(List<BaseGuild> list, bool tidy)
    {
      if (tidy)
        for (int i = 0; i < list.Count;)
          if (list[i].Disbanded)
            list.RemoveAt(i);
          else
            ++i;

      Write(list.Count);

      for (int i = 0; i < list.Count; ++i)
        Write(list[i]);
    }

    public void WriteGuildList<T>(List<T> list) where T : BaseGuild
    {
      WriteGuildList(list, false);
    }

    public void WriteGuildList<T>(List<T> list, bool tidy) where T : BaseGuild
    {
      if (tidy)
        for (int i = 0; i < list.Count;)
          if (list[i].Disbanded)
            list.RemoveAt(i);
          else
            ++i;

      Write(list.Count);

      for (int i = 0; i < list.Count; ++i)
        Write(list[i]);
    }

    public void Write(HashSet<BaseGuild> set)
    {
      Write(set, false);
    }

    public void Write(HashSet<BaseGuild> set, bool tidy)
    {
      if (tidy) set.RemoveWhere(guild => guild.Disbanded);

      Write(set.Count);

      foreach (BaseGuild guild in set) Write(guild);
    }

    public void WriteGuildSet<T>(HashSet<T> set) where T : BaseGuild
    {
      WriteGuildSet(set, false);
    }

    public void WriteGuildSet<T>(HashSet<T> set, bool tidy) where T : BaseGuild
    {
      if (tidy) set.RemoveWhere(guild => guild.Disbanded);

      Write(set.Count);

      foreach (T guild in set) Write(guild);
    }

    private class WorkerThread
    {
      private AsyncWriter m_Owner;

      public WorkerThread(AsyncWriter owner) => m_Owner = owner;

      public void Worker()
      {
        ThreadCount++;

        int lastCount;

        do
        {
          MemoryStream mem = null;

          lock (m_Owner.m_WriteQueue)
          {
            if ((lastCount = m_Owner.m_WriteQueue.Count) > 0)
              mem = m_Owner.m_WriteQueue.Dequeue();
          }

          if (mem?.Length > 0)
            mem.WriteTo(m_Owner.m_File);
        } while (lastCount > 1);

        if (m_Owner.m_Closed)
          m_Owner.m_File.Close();

        ThreadCount--;

        if (ThreadCount <= 0)
          World.NotifyDiskWriteComplete();
      }
    }
  }
}
