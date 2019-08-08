using System;

namespace Server.Network
{
  public static partial class Packets
  {
    public static WriteFixedPacketMethod<IEntity, int, int, int, int, int, int, int, int> TargetParticleEffect(out int length)
    {
      length = ParticleEffectPacketLength;

      static void write(Memory<byte> mem, IEntity e, int itemID, int speed, int duration, int hue, int renderMode,
        int effect, int layer, int unknown)
      {
        WriteParticalEffect(mem, EffectType.FixedFrom, e.Serial, Serial.Zero, itemID, e.Location, e.Location,
          speed, duration, true, false, hue, renderMode, effect, 1, 0, e.Serial, layer, unknown);
      }

      return write;
    }

    public static WriteFixedPacketMethod<IEntity, int, int, int, int, int> TargetHuedEffect(out int length)
    {
      length = HuedEffectPacketLength;

      static void write(Memory<byte> mem, IEntity e, int itemID, int speed, int duration, int hue, int renderMode)
      {
        WriteHuedEffect(mem, EffectType.FixedFrom, e.Serial, Serial.Zero, itemID, e.Location, e.Location, speed, duration,
          true, false, hue, renderMode);
      }

      return write;
    }
  }
}
