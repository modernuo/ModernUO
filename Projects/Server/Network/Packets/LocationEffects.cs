using System;

namespace Server.Network
{
  public static partial class Packets
  {
    public static WriteFixedPacketMethod<IEntity, int, int, int, int, int, int, int> LocationParticleEffect(out int length)
    {
      length = ParticleEffectPacketLength;

      static void write(Memory<byte> mem, IEntity e, int itemID, int speed, int duration, int hue, int renderMode,
        int effect, int unknown)
      {
        WriteParticalEffect(mem, EffectType.FixedXYZ, e.Serial, Serial.Zero, itemID, e.Location, e.Location, speed, duration,
          true, false, hue, renderMode, effect, 1, 0, e.Serial, 255, unknown);
      }

      return write;
    }

    public static WriteFixedPacketMethod<IPoint3D, int, int, int, int, int> LocationHuedEffect(out int length)
    {
      length = HuedEffectPacketLength;

      static void write(Memory<byte> mem, IPoint3D p, int itemID, int speed, int duration, int hue, int renderMode)
      {
        WriteHuedEffect(mem, EffectType.FixedXYZ, Serial.Zero, Serial.Zero, itemID, p, p, speed, duration, true, false,
          hue, renderMode);
      }

      return write;
    }
  }
}
