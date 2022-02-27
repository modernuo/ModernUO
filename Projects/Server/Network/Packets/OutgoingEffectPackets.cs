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

namespace Server.Network;

public static class OutgoingEffectPackets
{
    public const int SoundPacketLength = 12;
    public const int ParticleEffectLength = 49;
    public const int HuedEffectLength = 36;
    public const int BoltEffectLength = 36;

    public static void SendSoundEffect(this NetState ns, int soundID, IPoint3D target)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        Span<byte> buffer = stackalloc byte[SoundPacketLength].InitializePacket();
        CreateSoundEffect(buffer, soundID, target);

        ns.Send(buffer);
    }

    public static void CreateSoundEffect(Span<byte> buffer, int soundID, IPoint3D target)
    {
        if (buffer[0] != 0)
        {
            return;
        }

        var writer = new SpanWriter(buffer);
        writer.Write((byte)0x54); // Packet ID
        writer.Write((byte)1);    // flags
        writer.Write((short)soundID);
        writer.Write((short)0); // volume
        writer.Write((short)target.X);
        writer.Write((short)target.Y);
        writer.Write((short)target.Z);
    }

    public static void CreateParticleEffect(
        Span<byte> buffer,
        EffectType type, Serial from, Serial to, int itemID, IPoint3D fromPoint, IPoint3D toPoint,
        int speed, int duration, bool fixedDirection, bool explode, int hue, int renderMode, int effect,
        int explodeEffect, int explodeSound, Serial serial, int layer, int unknown
    )
    {
        if (buffer[0] != 0)
        {
            return;
        }

        var writer = new SpanWriter(buffer);
        writer.Write((byte)0xC7); // Packet ID
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
        Span<byte> buffer,
        IEntity e, int itemID, int speed, int duration, int hue, int renderMode, int effect, int layer, int unknown
    ) => CreateParticleEffect(
        buffer,
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
        Span<byte> buffer,
        IEntity e, int itemID, int speed, int duration, int hue, int renderMode, int effect, int unknown
    ) => CreateParticleEffect(
        buffer,
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
        Span<byte> buffer,
        IEntity from, IEntity to, int itemID, int speed, int duration, bool fixedDirection,
        bool explodes, int hue, int renderMode, int effect, int explodeEffect, int explodeSound, EffectLayer layer,
        int unknown
    ) => CreateParticleEffect(
        buffer,
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
        Span<byte> buffer,
        EffectType type, Serial from, Serial to, int itemID, Point3D fromPoint, Point3D toPoint,
        int speed, int duration, bool fixedDirection, bool explode, int hue, int renderMode
    )
    {
        if (buffer[0] != 0)
        {
            return;
        }

        var writer = new SpanWriter(buffer);
        writer.Write((byte)0xC0); // Packet ID
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
        Span<byte> buffer,
        IEntity e, int itemID, int speed, int duration, int hue, int renderMode
    ) => CreateHuedEffect(
        buffer,
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
        Span<byte> buffer,
        Point3D p, int itemID, int speed, int duration, int hue, int renderMode
    ) => CreateHuedEffect(
        buffer,
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
        Span<byte> buffer,
        IEntity from, IEntity to, int itemID, int speed, int duration, bool fixedDirection,
        bool explodes, int hue, int renderMode
    ) => CreateHuedEffect(
        buffer,
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
        Span<byte> buffer,
        int itemID, Point3D fromLocation, Point3D toLocation, int speed, int duration,
        bool fixedDirection, bool explodes, int hue, int renderMode
    ) => CreateHuedEffect(
        buffer,
        EffectType.Moving,
        Serial.Zero,
        Serial.Zero,
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

    public static void CreateMovingHuedEffect(
        Span<byte> buffer,
        Serial from, Serial to, int itemID, Point3D fromLocation, Point3D toLocation, int speed, int duration,
        bool fixedDirection, bool explodes, int hue, int renderMode
    ) => CreateHuedEffect(
        buffer,
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

    public static void CreateBoltEffect(Span<byte> buffer, IEntity target, int hue) => CreateHuedEffect(
        buffer,
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
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[28]);

        writer.Write((byte)0x70); // Packet ID
        writer.Write((byte)0x4);
        writer.Clear(8);
        writer.Write((ushort)type);
        writer.Clear(16);

        ns.Send(writer.Span);
    }
}
