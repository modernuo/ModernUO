/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
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

namespace Server
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
            ParticleSupportType == ParticleSupportType.Detect && state.IsUOTDClient;

        public static void PlaySound(IPoint3D p, Map map, int soundID)
        {
            if (soundID <= -1)
            {
                return;
            }

            if (map != null)
            {
                Span<byte> buffer = stackalloc byte[OutgoingEffectPackets.SoundPacketLength];
                OutgoingEffectPackets.CreateSoundEffect(ref buffer, soundID, p);

                var eable = map.GetClientsInRange(new Point3D(p));

                foreach (var state in eable)
                {
                    state.Mobile.ProcessDelta();
                    state.Send(ref buffer);
                }

                eable.Free();
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

            Span<byte> preEffect = stackalloc byte[OutgoingEffectPackets.ParticleEffectLength];
            OutgoingEffectPackets.CreateTargetParticleEffect(
                ref preEffect,
                e, 0, 10, 5, 0, 0, 5031, 3, 0
            );

            Span<byte> boltEffect = stackalloc byte[OutgoingEffectPackets.HuedEffectLength];
            OutgoingEffectPackets.CreateBoltEffect(ref boltEffect, e, hue);

            Span<byte> soundEffect = sound ? stackalloc byte[OutgoingEffectPackets.SoundPacketLength] : null;
            if (sound)
            {
                OutgoingEffectPackets.CreateSoundEffect(ref soundEffect, 0x29, e);
            }

            var eable = map.GetClientsInRange(e.Location);

            foreach (var state in eable)
            {
                if (state.Mobile.CanSee(e))
                {
                    if (SendParticlesTo(state))
                    {
                        state.Send(ref preEffect);
                    }

                    state.Send(ref boltEffect);

                    if (sound)
                    {
                        state.Send(ref soundEffect);
                    }
                }
            }

            eable.Free();
        }

        public static void SendLocationEffect(
            IPoint3D p, Map map, int itemID, int duration, int speed = 10, int hue = 0, int renderMode = 0
        )
        {
            SendPacket(p, map, new LocationEffect(p, itemID, speed, duration, hue, renderMode));
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

            Span<byte> particles = stackalloc byte[OutgoingEffectPackets.ParticleEffectLength];
            OutgoingEffectPackets.CreateLocationParticleEffect(
                ref particles,
                e, itemID, speed, duration, hue, renderMode, effect, unknown
            );

            Span<byte> regular = itemID != 0 ? stackalloc byte[OutgoingEffectPackets.HuedEffectLength] : null;
            if (itemID != 0)
            {
                OutgoingEffectPackets.CreateLocationHuedEffect(ref regular, e, itemID, speed, duration, hue, renderMode);
            }

            var eable = map.GetClientsInRange(e.Location);

            foreach (var state in eable)
            {
                state.Mobile.ProcessDelta();

                if (SendParticlesTo(state))
                {
                    state.Send(ref particles);
                }
                else if (itemID != 0)
                {
                    state.Send(ref regular);
                }
            }

            eable.Free();
        }

        public static void SendTargetEffect(IEntity target, int itemID, int speed, int duration, int hue = 0, int renderMode = 0)
        {
            if (target is Mobile mobile)
            {
                mobile.ProcessDelta();
            }

            SendPacket(target.Location, target.Map, new TargetEffect(target, itemID, speed, duration, hue, renderMode));
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
            if (target is Mobile mobile)
            {
                mobile.ProcessDelta();
            }

            var map = target.Map;

            if (map == null)
            {
                return;
            }

            Span<byte> particles = stackalloc byte[OutgoingEffectPackets.ParticleEffectLength];
            OutgoingEffectPackets.CreateTargetParticleEffect(
                ref particles,
                target, itemID, speed, duration, hue, renderMode, effect, (int)layer, unknown
            );

            Span<byte> regular = itemID != 0 ? stackalloc byte[OutgoingEffectPackets.HuedEffectLength] : null;
            if (itemID != 0)
            {
                OutgoingEffectPackets.CreateTargetHuedEffect(ref regular, target, itemID, speed, duration, hue, renderMode);
            }

            var eable = map.GetClientsInRange(target.Location);

            foreach (var state in eable)
            {
                state.Mobile.ProcessDelta();

                if (SendParticlesTo(state))
                {
                    state.Send(ref particles);
                }
                else if (itemID != 0)
                {
                    state.Send(ref regular);
                }
            }

            eable.Free();
        }

        public static void SendMovingEffect(
            IEntity from, IEntity to, int itemID, int speed, int duration,
            bool fixedDirection, bool explodes, int hue = 0, int renderMode = 0
        )
        {
            if (from is Mobile mobile)
            {
                mobile.ProcessDelta();
            }

            if (to is Mobile mobile1)
            {
                mobile1.ProcessDelta();
            }

            SendPacket(
                from.Location,
                from.Map,
                new MovingEffect(from, to, itemID, speed, duration, fixedDirection, explodes, hue, renderMode)
            );
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
            if (from is Mobile fromMob)
            {
                fromMob.ProcessDelta();
            }

            if (to is Mobile toMob)
            {
                toMob.ProcessDelta();
            }

            var map = from.Map;

            if (map == null)
            {
                return;
            }

            Span<byte> particles = stackalloc byte[OutgoingEffectPackets.ParticleEffectLength];
            OutgoingEffectPackets.CreateMovingParticleEffect(
                ref particles,
                from, to, itemID, speed, duration, fixedDirection, explodes, hue, renderMode, effect,
                explodeEffect, explodeSound, layer, unknown
            );

            Span<byte> regular = itemID != 0 ? stackalloc byte[OutgoingEffectPackets.HuedEffectLength] : null;
            if (itemID != 0)
            {
                OutgoingEffectPackets.CreateMovingHuedEffect(
                    ref regular,
                    from, to, itemID, speed, duration, fixedDirection, explodes, hue, renderMode
                );
            }

            var eable = map.GetClientsInRange(from.Location);

            foreach (var state in eable)
            {
                state.Mobile.ProcessDelta();

                if (SendParticlesTo(state))
                {
                    state.Send(ref particles);
                }
                else if (itemID > 1)
                {
                    state.Send(ref regular);
                }
            }

            eable.Free();
        }

        public static void SendPacket(IPoint3D origin, Map map, Packet p)
        {
            if (map == null)
            {
                return;
            }

            var eable = map.GetClientsInRange(new Point3D(origin));

            p.Acquire();

            foreach (var state in eable)
            {
                state.Mobile.ProcessDelta();
                state.Send(p);
            }

            p.Release();

            eable.Free();
        }
    }
}
