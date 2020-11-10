/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingEffectPackets.cs                                        *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Buffers;

namespace Server.Network
{
    public static class OutgoingEffectPackets
    {
        public const int SoundPacketLength = 12;
        public const int ParticleEffectLength = 49;
        public const int HuedEffectLength = 36;

        public static void CreateSoundEffect(ref Span<byte> buffer, int soundID, IPoint3D target)
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xC7);
            writer.Write((byte)1); // flags
            writer.Write((short)soundID);
            writer.Write((short)0); // volume
            writer.Write((short)target.X);
            writer.Write((short)target.Y);
            writer.Write((short)target.Z);
        }

        public static void SendSoundEffect(this NetState ns, int soundID, IPoint3D target)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0xC7);
            writer.Write((byte)1); // flags
            writer.Write((short)soundID);
            writer.Write((short)0); // volume
            writer.Write((short)target.X);
            writer.Write((short)target.Y);
            writer.Write((short)target.Z);

            ns.Send(ref buffer, writer.Position);
        }

        public static void CreateParticleEffect(
            ref Span<byte> buffer,
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
        }

        public static void CreateTargetParticleEffect(
            ref Span<byte> buffer,
            IEntity e, int itemID, int speed, int duration, int hue, int renderMode, int effect, int layer, int unknown
        ) => CreateParticleEffect(
            ref buffer,
            EffectType.FixedFrom,
            e.Serial,
            Serial.Zero,
            itemID,
            e.Location,
            e.Location,
            speed,
            duration,
            true,
            false,
            hue,
            renderMode,
            effect,
            1,
            0,
            e.Serial,
            layer,
            unknown
        );

        public static void CreateLocationParticleEffect(
            ref Span<byte> buffer,
            IEntity e, int itemID, int speed, int duration, int hue, int renderMode, int effect, int unknown
        ) => CreateParticleEffect(
            ref buffer,
            EffectType.FixedXYZ,
            e.Serial,
            Serial.Zero,
            itemID,
            e.Location,
            e.Location,
            speed,
            duration,
            true,
            false,
            hue,
            renderMode,
            effect,
            1,
            0,
            e.Serial,
            255,
            unknown
        );

        public static void CreateMovingParticleEffect(
            ref Span<byte> buffer,
            IEntity from, IEntity to, int itemID, int speed, int duration, bool fixedDirection,
            bool explodes, int hue, int renderMode, int effect, int explodeEffect, int explodeSound, EffectLayer layer,
            int unknown
        ) => CreateParticleEffect(
            ref buffer,
            EffectType.Moving,
            from.Serial,
            to.Serial,
            itemID,
            from.Location,
            to.Location,
            speed,
            duration,
            fixedDirection,
            explodes,
            hue,
            renderMode,
            effect,
            explodeEffect,
            explodeSound,
            Serial.Zero,
            (int)layer,
            unknown
        );

        public static void CreateHuedEffect(
            ref Span<byte> buffer,
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
        }

        public static void CreateTargetHuedEffect(
            ref Span<byte> buffer,
            IEntity e, int itemID, int speed, int duration, int hue, int renderMode
        ) => CreateHuedEffect(
            ref buffer,
            EffectType.FixedFrom,
            e.Serial,
            Serial.Zero,
            itemID,
            e.Location,
            e.Location,
            speed,
            duration,
            true,
            false,
            hue,
            renderMode
        );

        public static void CreateLocationHuedEffect(
            ref Span<byte> buffer,
            IPoint3D p, int itemID, int speed, int duration, int hue, int renderMode
        ) => CreateHuedEffect(
            ref buffer,
            EffectType.FixedXYZ,
            Serial.Zero,
            Serial.Zero,
            itemID,
            p,
            p,
            speed,
            duration,
            true,
            false,
            hue,
            renderMode
        );

        public static void CreateMovingHuedEffect(
            ref Span<byte> buffer,
            IEntity from, IEntity to, int itemID, int speed, int duration, bool fixedDirection,
            bool explodes, int hue, int renderMode
        ) => CreateHuedEffect(
            ref  buffer,
            EffectType.Moving,
            from.Serial,
            to.Serial,
            itemID,
            from.Location,
            to.Location,
            speed,
            duration,
            fixedDirection,
            explodes,
            hue,
            renderMode
        );

        public static void CreateMovingHuedEffect(
            ref Span<byte> buffer,
            Serial from, Serial to, int itemID, IPoint3D fromLocation, IPoint3D toLocation, int speed, int duration,
            bool fixedDirection, bool explodes, int hue, int renderMode
        ) => CreateHuedEffect(
            ref  buffer,
            EffectType.Moving,
            from,
            to,
            itemID,
            fromLocation,
            toLocation,
            speed,
            duration,
            fixedDirection,
            explodes,
            hue,
            renderMode
        );

        public static void CreateBoltEffect(ref Span<byte> buffer, IEntity target, int hue) => CreateHuedEffect(
            ref buffer,
            EffectType.Lightning,
            target.Serial,
            Serial.Zero,
            0,
            target.Location,
            target.Location,
            0,
            0,
            false,
            false,
            hue,
            0
        );

        public static void SendScreenEffect(this NetState ns, ScreenEffectType type)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);

            writer.Write((byte)0x70); // Packet ID
            writer.Clear(8);
            writer.Write((ushort)type);
            writer.Clear(16);

            ns.Send(ref buffer, writer.Position);
        }
    }
}
