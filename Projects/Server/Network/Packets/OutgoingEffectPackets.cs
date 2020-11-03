using System;
using System.Buffers;

namespace Server.Network
{
    public enum EffectType
    {
        Moving,
        Lightning,
        FixedXYZ,
        FixedFrom
    }

    public enum ScreenEffectType
    {
        FadeOut = 0x00,
        FadeIn = 0x01,
        LightFlash = 0x02,
        FadeInOut = 0x03,
        DarkFlash = 0x04
    }

    public static class OutgoingEffectPackets
    {
        public static int CreateParticleEffect(
            Span<byte> buffer,
            EffectType type, Serial from, Serial to, int itemID, IPoint3D fromPoint, IPoint3D toPoint,
            int speed, int duration, bool fixedDirection, bool explode, int hue, int renderMode, int effect,
            int explodeEffect, int explodeSound, Serial serial, int layer, int unknown
        )
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xC7);
            writer.Write((byte)type);
            writer.Write(from);
            writer.Write(to);
            writer.Write((short)itemID);
            writer.Write((short)fromPoint.X);
            writer.Write((short)fromPoint.Y);
            writer.Write((sbyte)fromPoint.Z);
            writer.Write((short)toPoint.X);
            writer.Write((short)toPoint.Y);
            writer.Write((sbyte)toPoint.Z);
            writer.Write((byte)speed);
            writer.Write((byte)duration);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write(fixedDirection);
            writer.Write(explode);
            writer.Write(hue);
            writer.Write(renderMode);
            writer.Write((short)effect);
            writer.Write((short)explodeEffect);
            writer.Write((short)explodeSound);
            writer.Write(serial);
            writer.Write((byte)layer);
            writer.Write((short)unknown);

            return writer.Position;
        }

        public static int CreateHuedEffect(
            Span<byte> buffer,
            EffectType type, Serial from, Serial to, int itemID, IPoint3D fromPoint, IPoint3D toPoint,
            int speed, int duration, bool fixedDirection, bool explode, int hue, int renderMode
        )
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xC0);
            writer.Write((byte)type);
            writer.Write(from);
            writer.Write(to);
            writer.Write((short)itemID);
            writer.Write((short)fromPoint.X);
            writer.Write((short)fromPoint.Y);
            writer.Write((sbyte)fromPoint.Z);
            writer.Write((short)toPoint.X);
            writer.Write((short)toPoint.Y);
            writer.Write((sbyte)toPoint.Z);
            writer.Write((byte)speed);
            writer.Write((byte)duration);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write(fixedDirection);
            writer.Write(explode);
            writer.Write(hue);
            writer.Write(renderMode);

            return writer.Position;
        }
    }
}
