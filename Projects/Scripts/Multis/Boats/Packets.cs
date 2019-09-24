using System.Collections.Generic;
using Server.Buffers;
using Server.Multis;

namespace Server.Network
{
  public static class BoatPackets
  {
    public static void SendMoveBoatHS(NetState ns, Mobile beholder, BaseBoat boat, Direction d, int speed,
      IReadOnlyCollection<IEntity> ents, int xOffset,
      int yOffset)
    {
      if (ns == null)
        return;

      int length = 18 + ents.Count * 10;
      SpanWriter writer = new SpanWriter(stackalloc byte[length]);
      writer.Write((byte)0xF6); // Packet ID
      writer.Position += 2; // Dynamic Length

      writer.Write(boat.Serial);
      writer.Write((byte)speed);
      writer.Write((byte)d);
      writer.Write((byte)boat.Facing);
      writer.Write((short)(boat.X + xOffset));
      writer.Write((short)(boat.Y + yOffset));
      writer.Write((short)boat.Z);
      writer.Position += 2; // count

      ushort count = 0;
      foreach (IEntity ent in ents)
      {
        if (!beholder.CanSee(ent))
          continue;

        writer.Write(ent.Serial);
        writer.Write((short)(ent.X + xOffset));
        writer.Write((short)(ent.Y + yOffset));
        writer.Write((short)ent.Z);
        count++;
      }

      writer.Position = 16;
      writer.Write(count);
      writer.Position = 1;
      writer.Write((ushort)writer.WrittenCount);

      ns.Send(writer.Span);
    }

    public static void SendDisplayBoatHS(NetState ns, Mobile beholder, BaseBoat boat)
    {
      if (ns == null)
        return;

      List<IEntity> ents = boat.GetMovingEntities();
      if (boat.TillerMan != null) ents.Add(boat.TillerMan);
      if (boat.Hold != null) ents.Add(boat.Hold);
      if (boat.PPlank != null) ents.Add(boat.PPlank);
      if (boat.SPlank != null) ents.Add(boat.SPlank);
      ents.Add(boat);

      int length = 5 + ents.Count * 26;
      SpanWriter writer = new SpanWriter(stackalloc byte[length]);
      writer.Write((byte)0xF7); // Packet ID
      writer.Position += 4; // Dynamic Length & Count

      ushort count = 0;

      foreach (IEntity ent in ents)
      {
        if (!beholder.CanSee(ent))
          continue;

        writer.Write((byte)0xF3);
        writer.Write((short)0x1);

        if (ent is Mobile m)
        {
          writer.Write((byte)0x01);
          writer.Write(m.Serial);
          writer.Write((short)m.Body);
          writer.Position++;

          writer.Write((short)1);
          writer.Write((short)1);

          writer.Write((short)(m.X & 0x7FFF));
          writer.Write((short)(m.Y & 0x3FFF));
          writer.Write((sbyte)m.Z);

          writer.Write((byte)m.Direction);
          writer.Write((short)m.Hue);
          writer.Write((byte)m.GetPacketFlags());
        }
        else if (ent is Item item)
        {
          writer.Position++;
          writer.Write(item.Serial);
          writer.Write((ushort)(item.ItemID & 0xFFFF));
          writer.Position++;

          writer.Write((short)item.Amount);
          writer.Write((short)item.Amount);

          writer.Write((short)(item.X & 0x7FFF));
          writer.Write((short)(item.Y & 0x3FFF));
          writer.Write((sbyte)item.Z);

          writer.Write((byte)item.Light);
          writer.Write((short)item.Hue);
          writer.Write((byte)item.GetPacketFlags());
        }

        writer.Position += 2;
        count++;
      }

      writer.Position = 3;
      writer.Write(count);
      writer.Position = 1;
      writer.Write((ushort)writer.WrittenCount);

      ns.Send(writer.Span);
    }
  }
}
