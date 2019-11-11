using System;
using System.Collections.Generic;
using Server.Buffers;
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

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public abstract class BaseBulletinBoard : Item
  {
    // Threads will be removed six hours after the last post was made
    private static TimeSpan ThreadDeletionTime = TimeSpan.FromHours(6.0);

    // A player may only create a thread once every two minutes
    private static TimeSpan ThreadCreateTime = TimeSpan.FromMinutes(2.0);

    // A player may only reply once every thirty seconds
    private static TimeSpan ThreadReplyTime = TimeSpan.FromSeconds(30.0);

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
      int totalSeconds = (int)ts.TotalSeconds;
      int seconds = totalSeconds % 60;
      int minutes = totalSeconds / 60;

      if (minutes != 0 && seconds != 0)
        return $"{minutes} minute{(minutes == 1 ? "" : "s")} and {seconds} second{(seconds == 1 ? "" : "s")}";

      if (minutes != 0)
        return $"{minutes} minute{(minutes == 1 ? "" : "s")}";

      return $"{seconds} second{(seconds == 1 ? "" : "s")}";
    }

    public virtual void Cleanup()
    {
      List<Item> items = Items;

      for (int i = items.Count - 1; i >= 0; --i)
      {
        if (i >= items.Count)
          continue;

        if (!(items[i] is BulletinMessage msg))
          continue;

        if (msg.Thread == null && CheckTime(msg.LastPostTime, ThreadDeletionTime))
        {
          msg.Delete();
          RecurseDelete(msg); // A root-level thread has expired
        }
      }
    }

    private void RecurseDelete(BulletinMessage msg)
    {
      List<Item> found = new List<Item>();
      List<Item> items = Items;

      for (int i = items.Count - 1; i >= 0; --i)
      {
        if (i >= items.Count)
          continue;

        if (!(items[i] is BulletinMessage check))
          continue;

        if (check.Thread == msg)
        {
          check.Delete();
          found.Add(check);
        }
      }

      for (int i = 0; i < found.Count; ++i)
        RecurseDelete((BulletinMessage)found[i]);
    }

    public virtual bool GetLastPostTime(Mobile poster, bool onlyCheckRoot, ref DateTime lastPostTime)
    {
      List<Item> items = Items;
      bool wasSet = false;

      for (int i = 0; i < items.Count; ++i)
      {
        if (!(items[i] is BulletinMessage msg) || msg.Poster != poster)
          continue;

        if (onlyCheckRoot && msg.Thread != null)
          continue;

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

        NetState state = from.NetState;

        BBPackets.SendBBDisplayBoard(state, this);
        Packets.SendContainerContent(state, from, this);
      }
      else
      {
        from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
      }
    }

    public virtual bool CheckRange(Mobile from)
    {
      if (from.AccessLevel >= AccessLevel.GameMaster)
        return true;

      return from.Map == Map && from.InRange(GetWorldLocation(), 2);
    }

    public void PostMessage(Mobile from, BulletinMessage thread, string subject, string[] lines)
    {
      if (thread != null)
        thread.LastPostTime = DateTime.UtcNow;

      AddItem(new BulletinMessage(from, thread, subject, lines));
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version

      writer.Write(BoardName);
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

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
      PacketHandlers.Register(0x71, 0, true, BBClientRequest);
    }

    public static void BBClientRequest(NetState state, PacketReader pvSrc)
    {
      Mobile from = state.Mobile;

      int packetID = pvSrc.ReadByte();

      if (!(World.FindItem(pvSrc.ReadUInt32()) is BaseBulletinBoard board) || !board.CheckRange(from))
        return;

      switch (packetID)
      {
        case 3:
          BBRequestContent(from, board, pvSrc);
          break;
        case 4:
          BBRequestHeader(from, board, pvSrc);
          break;
        case 5:
          BBPostMessage(from, board, pvSrc);
          break;
        case 6:
          BBRemoveMessage(from, board, pvSrc);
          break;
      }
    }

    public static void BBRequestContent(Mobile from, BaseBulletinBoard board, PacketReader pvSrc)
    {
      if (World.FindItem(pvSrc.ReadUInt32()) is BulletinMessage msg && msg.Parent == board)
        BBPackets.SendBBMessageContent(from.NetState, board, msg);
    }

    public static void BBRequestHeader(Mobile from, BaseBulletinBoard board, PacketReader pvSrc)
    {
      if (World.FindItem(pvSrc.ReadUInt32()) is BulletinMessage msg && msg.Parent == board)
        BBPackets.SendBBMessageHeader(from.NetState, board, msg);
    }

    public static void BBPostMessage(Mobile from, BaseBulletinBoard board, PacketReader pvSrc)
    {
      BulletinMessage thread = World.FindItem(pvSrc.ReadUInt32()) as BulletinMessage;

      if (thread != null && thread.Parent != board)
        thread = null;

      int breakout = 0;

      while (thread?.Thread != null && breakout++ < 10)
        thread = thread.Thread;

      DateTime lastPostTime = DateTime.MinValue;

      if (board.GetLastPostTime(from, thread == null, ref lastPostTime))
        if (!CheckTime(lastPostTime, thread == null ? ThreadCreateTime : ThreadReplyTime))
        {
          if (thread == null)
            from.SendMessage("You must wait {0} before creating a new thread.", FormatTS(ThreadCreateTime));
          else
            from.SendMessage("You must wait {0} before replying to another thread.", FormatTS(ThreadReplyTime));

          return;
        }

      string subject = pvSrc.ReadStringSafe(pvSrc.ReadByte());

      if (subject.Length == 0)
        return;

      string[] lines = new string[pvSrc.ReadByte()];

      if (lines.Length == 0)
        return;

      for (int i = 0; i < lines.Length; ++i)
        lines[i] = pvSrc.ReadStringSafe(pvSrc.ReadByte());

      board.PostMessage(from, thread, subject, lines);
    }

    public static void BBRemoveMessage(Mobile from, BaseBulletinBoard board, PacketReader pvSrc)
    {
      if (!(World.FindItem(pvSrc.ReadUInt32()) is BulletinMessage msg) || msg.Parent != board)
        return;

      if (from.AccessLevel < AccessLevel.GameMaster && msg.Poster != from)
        return;

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

      List<BulletinEquip> list = new List<BulletinEquip>();

      for (int i = 0; i < poster.Items.Count; ++i)
      {
        Item item = poster.Items[i];

        if (item.Layer >= Layer.OneHanded && item.Layer <= Layer.Mount)
          list.Add(new BulletinEquip(item.ItemID, item.Hue));
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

    public override void Serialize(GenericWriter writer)
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

      for (int i = 0; i < PostedEquip.Length; ++i)
      {
        writer.Write(PostedEquip[i].itemID);
        writer.Write(PostedEquip[i].hue);
      }

      writer.Write(Lines.Length);

      for (int i = 0; i < Lines.Length; ++i)
        writer.Write(Lines[i]);
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      switch (version)
      {
        case 1:
        case 0:
        {
          Poster = reader.ReadMobile();
          Subject = reader.ReadString();
          Time = reader.ReadDateTime();
          LastPostTime = reader.ReadDateTime();
          bool hasThread = reader.ReadBool();
          Thread = reader.ReadItem() as BulletinMessage;
          PostedName = reader.ReadString();
          PostedBody = reader.ReadInt();
          PostedHue = reader.ReadInt();

          PostedEquip = new BulletinEquip[reader.ReadInt()];

          for (int i = 0; i < PostedEquip.Length; ++i)
          {
            PostedEquip[i].itemID = reader.ReadInt();
            PostedEquip[i].hue = reader.ReadInt();
          }

          Lines = new string[reader.ReadInt()];

          for (int i = 0; i < Lines.Length; ++i)
            Lines[i] = reader.ReadString();

          if (hasThread && Thread == null)
            Delete();

          if (version == 0)
            ValidationQueue<BulletinMessage>.Add(this);

          break;
        }
      }
    }

    public void Validate()
    {
      if ((Parent as BulletinBoard)?.Items.Contains(this) == false)
        Delete();
    }
  }

  public static class BBPackets
  {
    public static void SendBBDisplayBoard(NetState ns, BaseBulletinBoard board)
    {
      SpanWriter writer = new SpanWriter(stackalloc byte[38]);
      writer.Write((byte)0x71); // Packet ID
      writer.Write((ushort)38); // Dynamic Length

      writer.Position++; // writer.Write((byte)0); // Command
      writer.Write(board.Serial);
      writer.WriteAsciiNull(board.BoardName ?? "", 29);

      ns.Send(writer.Span);
    }

    public static void SendBBMessageHeader(NetState ns, BaseBulletinBoard board, BulletinMessage msg)
    {
      string poster = msg.PostedName ?? "";
      string subject = msg.Subject ?? "";
      string time = msg.GetTimeAsString() ?? "";

      byte posterLength = Math.Min((byte)poster.Length, (byte)254);
      byte subjectLength = Math.Min((byte)subject.Length, (byte)254);
      byte timeLength = Math.Min((byte)time.Length, (byte)254);

      int length = 22 + posterLength + subjectLength + timeLength;
      SpanWriter writer = new SpanWriter(stackalloc byte[length]);
      writer.Write((byte)0x71); // Packet ID
      writer.Write((ushort)length); // Dynamic Length

      writer.Write((byte)0x01); // Command
      writer.Write(board.Serial); // Bulletin board serial
      writer.Write(msg.Serial); // Message serial
      writer.Write(msg.Thread?.Serial ?? 0);

      writer.Write(posterLength);
      writer.WriteAsciiNull(poster, posterLength + 1);
      writer.Write(subjectLength);
      writer.WriteAsciiNull(subject, subjectLength + 1);
      writer.Write(timeLength);
      writer.WriteAsciiNull(time, timeLength + 1);

      ns.Send(writer.Span);
    }

    public static void SendBBMessageContent(NetState ns, BaseBulletinBoard board, BulletinMessage msg)
    {
      string poster = msg.PostedName ?? "";
      string subject = msg.Subject ?? "";
      string time = msg.GetTimeAsString() ?? "";

      byte posterLength = Math.Min((byte)poster.Length, (byte)254);
      byte subjectLength = Math.Min((byte)subject.Length, (byte)254);
      byte timeLength = Math.Min((byte)time.Length, (byte)254);

      int equipLength = Math.Min(msg.PostedEquip.Length, 255);
      int msgLength = Math.Min(msg.Lines.Length, 255);

    public string SafeString(string v) => v ?? string.Empty;
  }

  public class BBMessageContent : Packet
  {
    public BBMessageContent(BaseBulletinBoard board, BulletinMessage msg) : base(0x71)
    {
      string poster = SafeString(msg.PostedName);
      string subject = SafeString(msg.Subject);
      string time = SafeString(msg.GetTimeAsString());

      EnsureCapacity(22 + poster.Length + subject.Length + time.Length);

      writer.Write((byte)0x02); // Command
      writer.Write(board.Serial); // Bulletin board serial
      writer.Write(msg.Serial); // Message serial

      writer.Write(posterLength);
      writer.WriteAsciiNull(poster, posterLength + 1);
      writer.Write(subjectLength);
      writer.WriteAsciiNull(subject, subjectLength + 1);
      writer.Write(timeLength);
      writer.WriteAsciiNull(time, timeLength + 1);

      writer.Write((short)msg.PostedBody);
      writer.Write((short)msg.PostedHue);

      writer.Write((byte)equipLength);

      for (int i = 0; i < equipLength; ++i)
      {
        BulletinEquip eq = msg.PostedEquip[i];

        writer.Write((short)eq.itemID);
        writer.Write((short)eq.hue);
      }

      writer.Write((byte)msgLength);

      for (int i = 0; i < msgLength; ++i)
      {
        string line = msg.Lines[i];
        byte len = Math.Min((byte)line.Length, (byte)254);
        writer.WriteAsciiNull(line, len);
      }

      writer.Position = 1;
      writer.Write((ushort)writer.WrittenCount);

      ns.Send(writer.Span);
    }
  }
}
