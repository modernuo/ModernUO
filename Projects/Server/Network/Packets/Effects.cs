using Server.Buffers;

namespace Server.Network.Packets
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
    private static readonly int ParticleEffectPacketLength = 49;
    private static readonly int HuedEffectPacketLength = 36;
    private static readonly int OldEffectPacketLength = 28;

    public static void SendParticalEffect(NetState ns, EffectType type, Serial from, Serial to,
      int itemId, IPoint3D fromPoint, IPoint3D toPoint, int speed, int duration, bool fixedDirection,
      bool explode, int hue, int renderMode, int effect, int explodeEffect, int explodeSound, Serial serial,
      int layer, int unknown)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[ParticleEffectPacketLength]);
      w.Write((byte)0xC7); // Packet ID

      w.Write((byte)type);
      w.Write(from);
      w.Write(to);
      w.Write((short)itemId);
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

      ns.Send(w.RawSpan);
    }

    public static void SendHuedEffect(NetState ns, EffectType type, Serial from, Serial to, int itemId,
      IPoint3D fromPoint, IPoint3D toPoint, int speed, int duration, bool fixedDirection, bool explode, int hue,
      int renderMode)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[HuedEffectPacketLength]);
      w.Write((byte)0xC0); // Packet ID

      w.Write((byte)type);
      w.Write(from);
      w.Write(to);
      w.Write((short)itemId);
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

      ns.Send(w.RawSpan);
    }

    public static void SendScreenEffect(NetState ns, ScreenEffectType screen)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[HuedEffectPacketLength]);
      w.Write((byte)0xC0); // Packet ID

      w.Write((byte)0x04);
      w.Position += 8;
      w.Write((short)screen);

      ns.Send(w.RawSpan);
    }

    public static void SendScreenOldEffect(NetState ns, ScreenEffectType screen)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[OldEffectPacketLength]);
      w.Write((byte)0x70); // Packet ID

      w.Write((byte)0x04);
      w.Position += 8;
      w.Write((short)screen);

      ns.Send(w.RawSpan);
    }

    public static void SendBoltEffect(NetState ns, IEntity target)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[HuedEffectPacketLength]);
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

      ns.Send(w.RawSpan);
    }

    public static void SendOldBoltEffect(NetState ns, IEntity target)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[OldEffectPacketLength]);
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

      ns.Send(w.RawSpan);
    }

    public static void SendEffect(NetState ns, EffectType type, Serial from, Serial to, int itemId,
      IPoint3D fromPoint, IPoint3D toPoint, int speed, int duration, bool fixedDirection, bool explode)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[HuedEffectPacketLength]);
      w.Write((byte)0xC0); // Packet ID

      w.Write((byte)type);
      w.Write(from);
      w.Write(to);
      w.Write((short)itemId);
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

      ns.Send(w.RawSpan);
    }

    public static void SendOldEffect(NetState ns, EffectType type, Serial from, Serial to, int itemId,
      IPoint3D fromPoint, IPoint3D toPoint, int speed, int duration, bool fixedDirection, bool explode)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[OldEffectPacketLength]);
      w.Write((byte)0x70); // Packet ID

      w.Write((byte)type);
      w.Write(from);
      w.Write(to);
      w.Write((short)itemId);
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

      ns.Send(w.RawSpan);
    }

    public static void SendLocationParticleEffect(NetState ns, IEntity e, int itemId, int speed, int duration, int hue, int renderMode,
        int effect, int unknown)
    {
      SendParticalEffect(ns, EffectType.FixedXYZ, e.Serial, Serial.Zero, itemId, e.Location, e.Location, speed, duration,
        true, false, hue, renderMode, effect, 1, 0, e.Serial, 255, unknown);
    }

    public static void SendLocationHuedEffect(NetState ns, IPoint3D p, int itemId, int speed, int duration, int hue, int renderMode)
    {
      SendHuedEffect(ns, EffectType.FixedXYZ, Serial.Zero, Serial.Zero, itemId, p, p, speed, duration, true, false,
        hue, renderMode);
    }

    public static void SendMovingParticleEffect(NetState ns, IEntity from, IEntity to, int itemId, int speed, int duration, bool fixedDirection,
        bool explodes, int hue, int renderMode, int effect, int explodeEffect, int explodeSound, EffectLayer layer,
        int unknown)
    {
      SendParticalEffect(ns, EffectType.Moving, from.Serial, to.Serial, itemId, from.Location, to.Location, speed,
        duration, fixedDirection, explodes, hue, renderMode, effect, explodeEffect, explodeSound, Serial.Zero,
        (int)layer, unknown);
    }

    public static void SendMovingHuedEffect(NetState ns, IEntity from, IEntity to, int itemId, int speed, int duration, bool fixedDirection,
        bool explodes, int hue, int renderMode)
    {
      SendHuedEffect(ns, EffectType.Moving, from.Serial, to.Serial, itemId, from.Location,
        to.Location, speed, duration, fixedDirection, explodes, hue, renderMode);
    }

    public static void SendTargetParticleEffect(NetState ns, IEntity e, int itemId, int speed, int duration, int hue, int renderMode,
        int effect, int layer, int unknown)
    {
      SendParticalEffect(ns, EffectType.FixedFrom, e.Serial, Serial.Zero, itemId, e.Location, e.Location,
        speed, duration, true, false, hue, renderMode, effect, 1, 0, e.Serial, layer, unknown);
    }

    public static void SendTargetHuedEffect(NetState ns, IEntity e, int itemId, int speed, int duration, int hue, int renderMode)
    {
      SendHuedEffect(ns, EffectType.FixedFrom, e.Serial, Serial.Zero, itemId, e.Location, e.Location, speed, duration,
        true, false, hue, renderMode);
    }
  }
}
