using System;

namespace Server.Network
{
  public static partial class Packets
  {
    public static WriteFixedPacketMethod<IEntity, IEntity, int, int, int, bool, bool, int, int, int, int, int, EffectLayer, int> MovingParticleEffect(out int length)
    {
      length = ParticleEffectPacketLength;

      static void write(Memory<byte> mem, IEntity from, IEntity to, int itemID, int speed, int duration, bool fixedDirection,
        bool explodes, int hue, int renderMode, int effect, int explodeEffect, int explodeSound, EffectLayer layer,
        int unknown)
      {
        WriteParticalEffect(mem, EffectType.Moving, from.Serial, to.Serial, itemID, from.Location, to.Location, speed,
          duration, fixedDirection, explodes, hue, renderMode, effect, explodeEffect, explodeSound, Serial.Zero,
          (int)layer, unknown);
      }

      return write;
    }

    public static WriteFixedPacketMethod<IEntity, IEntity, int, int, int, bool, bool, int, int> MovingHuedEffect(out int length)
    {
      length = HuedEffectPacketLength;

      static void write(Memory<byte> mem, IEntity from, IEntity to, int itemID, int speed, int duration, bool fixedDirection,
        bool explodes, int hue, int renderMode)
      {
        WriteHuedEffect(mem, EffectType.Moving, from.Serial, to.Serial, itemID, from.Location,
          to.Location, speed, duration, fixedDirection, explodes, hue, renderMode);
      }

      return write;
    }
  }
}
