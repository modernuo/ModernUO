using System;

namespace Server.Network
{
  public enum EffectType : byte
  {
    Moving,
    Lightning,
    FixedXYZ,
    FixedFrom,
    Screen
  }

  public enum ScreenEffectType : short
  {
    FadeOut,
    FadeIn,
    LightFlash,
    FadeInOut,
    DarkFlash
  }

  public static partial class Packets
  {
    internal static readonly int ParticleEffectPacketLength = 49;
    internal static readonly int HuedEffectPacketLength = 36;
    internal static readonly int OldEffectPacketLength = 28;

    public static void WriteParticalEffect(Memory<byte> mem, EffectType type, Serial from, Serial to,
      int itemID, IPoint3D fromPoint, IPoint3D toPoint, int speed, int duration, bool fixedDirection,
      bool explode, int hue, int renderMode, int effect, int explodeEffect, int explodeSound, Serial serial,
      int layer, int unknown)
    {
      SpanWriter w = new SpanWriter(mem.Span, ParticleEffectPacketLength);
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
      SpanWriter w = new SpanWriter(mem.Span, HuedEffectPacketLength);
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

    public static WriteFixedPacketMethod<ScreenEffectType> ScreenEffect(out int length)
    {
      length = HuedEffectPacketLength;

      static void write(Memory<byte> mem, ScreenEffectType screen)
      {
        SpanWriter w = new SpanWriter(mem.Span, HuedEffectPacketLength);
        w.Write((byte)0xC0); // Packet ID

        w.Write((byte)0x04);
        w.Position += 8;
        w.Write((short)screen);
      }

      return write;
    }

    public static WriteFixedPacketMethod<ScreenEffectType> ScreenOldEffect(out int length)
    {
      length = OldEffectPacketLength;

      static void write(Memory<byte> mem, ScreenEffectType screen)
      {
        SpanWriter w = new SpanWriter(mem.Span, OldEffectPacketLength);
        w.Write((byte)0xC0); // Packet ID

        w.Write((byte)0x04);
        w.Position += 8;
        w.Write((short)screen);
      }

      return write;
    }

    public static WriteFixedPacketMethod<IEntity> BoltEffect(out int length)
    {
      length = HuedEffectPacketLength;

      static void write(Memory<byte> mem, IEntity target)
      {
        SpanWriter w = new SpanWriter(mem.Span, HuedEffectPacketLength);
        w.Write((byte)0xC0); // Packet ID

        w.Write((byte)0x01);
        w.Write(target.Serial);
        w.Position += 6;
        w.Write((short)target.X);
        w.Write((short)target.Y);
        w.Write((sbyte)target.Z);
        w.Write((short)target.X);
        w.Write((short)target.Y);
        w.Write((sbyte)target.Z);
      }

      return write;
    }

    public static WriteFixedPacketMethod<IEntity> OldBoltEffect(out int length)
    {
      length = OldEffectPacketLength;

      static void write(Memory<byte> mem, IEntity target)
      {
        SpanWriter w = new SpanWriter(mem.Span, OldEffectPacketLength);
        w.Write((byte)0x70); // Packet ID

        w.Write((byte)0x01);
        w.Write(target.Serial);
        w.Position += 6;
        w.Write((short)target.X);
        w.Write((short)target.Y);
        w.Write((sbyte)target.Z);
        w.Write((short)target.X);
        w.Write((short)target.Y);
        w.Write((sbyte)target.Z);
      }

      return write;
    }

    public static void WriteEffect(Memory<byte> mem, EffectType type, Serial from, Serial to, int itemID,
      IPoint3D fromPoint, IPoint3D toPoint, int speed, int duration, bool fixedDirection, bool explode)
    {
      SpanWriter w = new SpanWriter(mem.Span, HuedEffectPacketLength);
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
    }

    public static void WriteOldEffect(Memory<byte> mem, EffectType type, Serial from, Serial to, int itemID,
      IPoint3D fromPoint, IPoint3D toPoint, int speed, int duration, bool fixedDirection, bool explode)
    {
      SpanWriter w = new SpanWriter(mem.Span, OldEffectPacketLength);
      w.Write((byte)0x70); // Packet ID

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
    }
  }
}
