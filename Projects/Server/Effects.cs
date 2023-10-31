/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Effects.cs                                                      *
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
using Server.Network;

namespace Server;

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

public enum EffectLayer
{
    Head = 0,
    RightHand = 1,
    LeftHand = 2,
    Waist = 3,
    LeftFoot = 4,
    RightFoot = 5,
    CenterFeet = 7
}

public enum ParticleSupportType
{
    Full,
    Detect,
    None
}

public static class Effects
{
    public static ParticleSupportType ParticleSupportType { get; set; } = ParticleSupportType.Detect;

    public static bool SendParticlesTo(NetState state) =>
        ParticleSupportType == ParticleSupportType.Full ||
        ParticleSupportType == ParticleSupportType.Detect && (state.IsUOTDClient || state.IsEnhancedClient);

    public static void PlaySound(IEntity e, int soundID) => PlaySound(e.Location, e.Map, soundID);

    public static void PlaySound(Point3D p, Map map, int soundID)
    {
        if (soundID <= -1)
        {
            return;
        }

        if (map != null)
        {
            Span<byte> buffer = stackalloc byte[OutgoingEffectPackets.SoundPacketLength].InitializePacket();

            foreach (var state in map.GetClientsInRange(p))
            {
                state.Mobile.ProcessDelta();
                OutgoingEffectPackets.CreateSoundEffect(buffer, soundID, p);
                state.Send(buffer);
            }
        }
    }

    public static void SendBoltEffect(IEntity e, bool sound = true, int hue = 0)
    {
        var map = e.Map;

        if (map == null)
        {
            return;
        }

        e.ProcessDelta();

        Span<byte> preEffect = stackalloc byte[OutgoingEffectPackets.ParticleEffectLength].InitializePacket();
        Span<byte> boltEffect = stackalloc byte[OutgoingEffectPackets.BoltEffectLength].InitializePacket();
        Span<byte> soundEffect = sound ? stackalloc byte[OutgoingEffectPackets.SoundPacketLength].InitializePacket() : null;

        foreach (var state in map.GetClientsInRange(e.Location))
        {
            if (state.Mobile.CanSee(e))
            {
                if (SendParticlesTo(state))
                {
                    OutgoingEffectPackets.CreateTargetParticleEffect(
                        preEffect,
                        e, 0, 10, 5, 0, 0, 5031, 3, 0
                    );
                    state.Send(preEffect);
                }

                OutgoingEffectPackets.CreateBoltEffect(boltEffect, e, hue);
                state.Send(boltEffect);

                if (sound)
                {
                    OutgoingEffectPackets.CreateSoundEffect(soundEffect, 0x29, e);
                    state.Send(soundEffect);
                }
            }
        }
    }

    public static void SendLocationEffect(
        IEntity e, int itemID, int duration, int speed = 10, int hue = 0, int renderMode = 0
    ) => SendLocationEffect(e.Location, e.Map, itemID, duration, speed, hue, renderMode);

    public static void SendLocationEffect(
        Point3D p, Map map, int itemID, int duration, int speed = 10, int hue = 0, int renderMode = 0
    )
    {
        Span<byte> effect = stackalloc byte[OutgoingEffectPackets.HuedEffectLength].InitializePacket();
        OutgoingEffectPackets.CreateLocationHuedEffect(
            effect,
            p, itemID, speed, duration, hue, renderMode
        );

        SendPacket(p, map, effect);
    }

    public static void SendLocationParticles(IEntity e, int itemID, int speed, int duration, int effect)
    {
        SendLocationParticles(e, itemID, speed, duration, 0, 0, effect, 0);
    }

    public static void SendLocationParticles(IEntity e, int itemID, int speed, int duration, int effect, int unknown)
    {
        SendLocationParticles(e, itemID, speed, duration, 0, 0, effect, unknown);
    }

    public static void SendLocationParticles(
        IEntity e, int itemID, int speed, int duration, int hue, int renderMode, int effect, int unknown
    )
    {
        var map = e.Map;

        if (map == null)
        {
            return;
        }

        Span<byte> particles = stackalloc byte[OutgoingEffectPackets.ParticleEffectLength].InitializePacket();

        Span<byte> regular = itemID != 0 ? stackalloc byte[OutgoingEffectPackets.HuedEffectLength].InitializePacket() : null;

        foreach (var state in map.GetClientsInRange(e.Location))
        {
            state.Mobile.ProcessDelta();

            if (SendParticlesTo(state))
            {
                OutgoingEffectPackets.CreateLocationParticleEffect(
                    particles,
                    e, itemID, speed, duration, hue, renderMode, effect, unknown
                );
                state.Send(particles);
            }
            else if (itemID != 0)
            {
                OutgoingEffectPackets.CreateLocationHuedEffect(
                    regular,
                    e.Location, itemID, speed, duration, hue, renderMode
                );
                state.Send(regular);
            }
        }
    }

    public static void SendTargetEffect(IEntity target, int itemID, int speed, int duration, int hue = 0, int renderMode = 0)
    {
        (target as Mobile)?.ProcessDelta();

        Span<byte> effect = stackalloc byte[OutgoingEffectPackets.HuedEffectLength].InitializePacket();
        OutgoingEffectPackets.CreateTargetHuedEffect(
            effect,
            target, itemID, speed, duration, hue, renderMode
        );

        SendPacket(target.Location, target.Map, effect);
    }

    public static void SendTargetParticles(
        IEntity target, int itemID, int speed, int duration, int effect,
        EffectLayer layer
    )
    {
        SendTargetParticles(target, itemID, speed, duration, 0, 0, effect, layer);
    }

    public static void SendTargetParticles(
        IEntity target, int itemID, int speed, int duration, int hue, int renderMode,
        int effect, EffectLayer layer, int unknown = 0
    )
    {
        (target as Mobile)?.ProcessDelta();

        var map = target.Map;

        if (map == null)
        {
            return;
        }

        Span<byte> particles = stackalloc byte[OutgoingEffectPackets.ParticleEffectLength].InitializePacket();
        Span<byte> regular = itemID != 0 ? stackalloc byte[OutgoingEffectPackets.HuedEffectLength].InitializePacket() : null;

        foreach (var state in map.GetClientsInRange(target.Location))
        {
            state.Mobile.ProcessDelta();

            if (SendParticlesTo(state))
            {
                OutgoingEffectPackets.CreateTargetParticleEffect(
                    particles,
                    target, itemID, speed, duration, hue, renderMode, effect, (int)layer, unknown
                );
                state.Send(particles);
            }
            else if (itemID != 0)
            {
                OutgoingEffectPackets.CreateTargetHuedEffect(regular, target, itemID, speed, duration, hue, renderMode);
                state.Send(regular);
            }
        }
    }

    public static void SendMovingEffect(
        Map map, int itemID, Point3D from, Point3D to, int speed, int duration,
        bool fixedDirection = false, bool explodes = false, int hue = 0, int renderMode = 0
    ) => SendMovingEffect(
        Serial.Zero,
        Serial.Zero,
        from,
        map,
        itemID,
        from,
        to,
        speed,
        duration,
        fixedDirection,
        explodes,
        hue,
        renderMode
    );

    public static void SendMovingEffect(
        Point3D origin, Map map, int itemID, Point3D from, Point3D to, int speed, int duration,
        bool fixedDirection = false, bool explodes = false, int hue = 0, int renderMode = 0
    ) => SendMovingEffect(
        Serial.Zero,
        Serial.Zero,
        origin,
        map,
        itemID,
        from,
        to,
        speed,
        duration,
        fixedDirection,
        explodes,
        hue,
        renderMode
    );

    public static void SendMovingEffect(
        IEntity from, Point3D to, int itemID,
        int speed, int duration, bool fixedDirection = false, bool explodes = false, int hue = 0, int renderMode = 0
    )
    {
        (from as Mobile)?.ProcessDelta();

        SendMovingEffect(
            from.Serial,
            Serial.Zero,
            from.Location,
            from.Map,
            itemID,
            from.Location,
            to,
            speed,
            duration,
            fixedDirection,
            explodes,
            hue,
            renderMode
        );
    }

    public static void SendMovingEffect(
        IEntity from, IEntity to, int itemID,
        int speed, int duration, bool fixedDirection = false, bool explodes = false, int hue = 0, int renderMode = 0
    )
    {
        (from as Mobile)?.ProcessDelta();
        (to as Mobile)?.ProcessDelta();

        SendMovingEffect(
            from,
            to,
            from.Location,
            from.Map,
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
    }

    public static void SendMovingEffect(
        IEntity from, IEntity to, Point3D origin, Map map, int itemID, Point3D fromLocation, Point3D toLocation,
        int speed, int duration, bool fixedDirection = false, bool explodes = false, int hue = 0, int renderMode = 0
    )
    {
        (from as Mobile)?.ProcessDelta();
        (to as Mobile)?.ProcessDelta();

        SendMovingEffect(
            from.Serial,
            to.Serial,
            origin,
            map,
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
    }

    public static void SendMovingEffect(
        Serial from, Serial to, Point3D origin, Map map, int itemID, Point3D fromLocation, Point3D toLocation,
        int speed, int duration, bool fixedDirection = false, bool explodes = false, int hue = 0, int renderMode = 0
    )
    {
        Span<byte> effect = stackalloc byte[OutgoingEffectPackets.HuedEffectLength].InitializePacket();
        OutgoingEffectPackets.CreateMovingHuedEffect(
            effect,
            from, to, itemID, fromLocation, toLocation, speed, duration, fixedDirection,
            explodes, hue, renderMode
        );

        SendPacket(origin, map, effect);
    }

    public static void SendMovingParticles(
        IEntity from, IEntity to, int itemID, int speed, int duration,
        bool fixedDirection, bool explodes, int effect, int explodeEffect, int explodeSound, int unknown = 0
    )
    {
        SendMovingParticles(
            from,
            to,
            itemID,
            speed,
            duration,
            fixedDirection,
            explodes,
            0,
            0,
            effect,
            explodeEffect,
            explodeSound,
            unknown
        );
    }

    public static void SendMovingParticles(
        IEntity from, IEntity to, int itemID, int speed, int duration,
        bool fixedDirection, bool explodes, int hue, int renderMode, int effect, int explodeEffect, int explodeSound,
        int unknown
    )
    {
        SendMovingParticles(
            from,
            to,
            itemID,
            speed,
            duration,
            fixedDirection,
            explodes,
            hue,
            renderMode,
            effect,
            explodeEffect,
            explodeSound,
            (EffectLayer)255,
            unknown
        );
    }

    public static void SendMovingParticles(
        IEntity from, IEntity to, int itemID, int speed, int duration,
        bool fixedDirection, bool explodes, int hue, int renderMode, int effect, int explodeEffect, int explodeSound,
        EffectLayer layer, int unknown
    )
    {
        (from as Mobile)?.ProcessDelta();
        (to as Mobile)?.ProcessDelta();

        var map = from.Map;

        if (map == null)
        {
            return;
        }

        Span<byte> particles = stackalloc byte[OutgoingEffectPackets.ParticleEffectLength].InitializePacket();
        Span<byte> regular = itemID != 0 ? stackalloc byte[OutgoingEffectPackets.HuedEffectLength].InitializePacket() : null;

        foreach (var state in map.GetClientsInRange(from.Location))
        {
            state.Mobile.ProcessDelta();

            if (SendParticlesTo(state))
            {
                OutgoingEffectPackets.CreateMovingParticleEffect(
                    particles,
                    from, to, itemID, speed, duration, fixedDirection, explodes, hue, renderMode, effect,
                    explodeEffect, explodeSound, layer, unknown
                );
                state.Send(particles);
            }
            else if (itemID > 1)
            {
                OutgoingEffectPackets.CreateMovingHuedEffect(
                    regular,
                    from, to, itemID, speed, duration, fixedDirection, explodes, hue, renderMode
                );
                state.Send(regular);
            }
        }
    }

    public static void SendPacket(Point3D origin, Map map, Span<byte> effectBuffer)
    {
        if (map == null)
        {
            return;
        }

        foreach (var state in map.GetClientsInRange(origin))
        {
            state.Mobile.ProcessDelta();
            state.Send(effectBuffer);
        }
    }
}
