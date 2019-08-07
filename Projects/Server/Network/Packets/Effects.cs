using System;

namespace Server.Network
{
  public static partial class Packets
  {
    public static void WriteParticaleEffect(Memory<byte> mem, EffectType type, Serial from, Serial to,
      int itemID, IPoint3D fromPoint, IPoint3D toPoint, int speed, int duration, bool fixedDirection,
      bool explode, int hue, int renderMode, int effect, int explodeEffect, int explodeSound, Serial serial,
      int layer, int unknown)
    {
      SpanWriter w = new SpanWriter(mem.Span, 49);
      w.Write((byte)0xC7); // Packet ID

      w.Write((byte)type);
      w.Write(from);
      w.Write(to);
      w.Write((short)itemID);
      w.Write((short)fromPoint.X);
      w.Write((short)fromPoint.Y);
      w.Write((sbyte)fromPoint.Z);
      w.Write((short)toPoint.X);
      w.Write((short)toPoint.Y);
      w.Write((sbyte)toPoint.Z);
      w.Write((byte)speed);
      w.Write((byte)duration);
      w.Position += 2;
      // w.Write((byte)0);
      // w.Write((byte)0);
      w.Write(fixedDirection);
      w.Write(explode);
      w.Write(hue);
      w.Write(renderMode);
      w.Write((short)effect);
      w.Write((short)explodeEffect);
      w.Write((short)explodeSound);
      w.Write(serial);
      w.Write((byte)layer);
      w.Write((short)unknown);
    }

    public static void WriteHuedEffect(Memory<byte> mem, EffectType type, Serial from, Serial to, int itemID,
      IPoint3D fromPoint, IPoint3D toPoint, int speed, int duration, bool fixedDirection, bool explode, int hue,
      int renderMode)
    {
      SpanWriter w = new SpanWriter(mem.Span, 36);
      w.Write((byte)0xC0); // Packet ID

      w.Write((byte)type);
      w.Write(from);
      w.Write(to);
      w.Write((short)itemID);
      w.Write((short)fromPoint.X);
      w.Write((short)fromPoint.Y);
      w.Write((sbyte)fromPoint.Z);
      w.Write((short)toPoint.X);
      w.Write((short)toPoint.Y);
      w.Write((sbyte)toPoint.Z);
      w.Write((byte)speed);
      w.Write((byte)duration);
      w.Position += 2;
      // w.Write((byte)0);
      // w.Write((byte)0);
      w.Write(fixedDirection);
      w.Write(explode);
      w.Write(hue);
      w.Write(renderMode);
    }

    public static WriteFixedPacketMethod<IEntity, int, int, int, int, int, int, int, int> TargetParticleEffect(out int length)
    {
      length = 49;

      static void write(Memory<byte> mem, IEntity e, int itemID, int speed, int duration, int hue, int renderMode,
        int effect, int layer, int unknown)
      {
        WriteParticaleEffect(mem, EffectType.FixedFrom, e.Serial, Serial.Zero, itemID, e.Location, e.Location,
          speed, duration, true, false, hue, renderMode, effect, 1, 0, e.Serial, layer, unknown);
      }

      return write;
    }

    public static WriteFixedPacketMethod<IEntity, int, int, int, int, int> TargetEffect(out int length)
    {
      length = 36;

      static void write(Memory<byte> mem, IEntity e, int itemID, int speed, int duration, int hue, int renderMode)
      {
        WriteHuedEffect(mem, EffectType.FixedFrom, e.Serial, Serial.Zero, itemID, e.Location, e.Location, speed, duration,
          true, false, hue, renderMode);
      }

      return write;
    }
  }
}
