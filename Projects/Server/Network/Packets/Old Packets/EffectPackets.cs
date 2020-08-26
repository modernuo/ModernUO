/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: EffectPackets.cs - Created: 2020/05/26 - Updated: 2020/05/26    *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Network
{
    public enum EffectType
    {
        Moving,
        Lightning,
        FixedXYZ,
        FixedFrom
    }

    public class ParticleEffect : Packet
    {
        public ParticleEffect(
            EffectType type, Serial from, Serial to, int itemID, Point3D fromPoint, Point3D toPoint,
            int speed, int duration, bool fixedDirection, bool explode, int hue, int renderMode, int effect,
            int explodeEffect, int explodeSound, Serial serial, int layer, int unknown
        ) : base(0xC7, 49)
        {
            Stream.Write((byte)type);
            Stream.Write(from);
            Stream.Write(to);
            Stream.Write((short)itemID);
            Stream.Write((short)fromPoint.m_X);
            Stream.Write((short)fromPoint.m_Y);
            Stream.Write((sbyte)fromPoint.m_Z);
            Stream.Write((short)toPoint.m_X);
            Stream.Write((short)toPoint.m_Y);
            Stream.Write((sbyte)toPoint.m_Z);
            Stream.Write((byte)speed);
            Stream.Write((byte)duration);
            Stream.Write((byte)0);
            Stream.Write((byte)0);
            Stream.Write(fixedDirection);
            Stream.Write(explode);
            Stream.Write(hue);
            Stream.Write(renderMode);
            Stream.Write((short)effect);
            Stream.Write((short)explodeEffect);
            Stream.Write((short)explodeSound);
            Stream.Write(serial);
            Stream.Write((byte)layer);
            Stream.Write((short)unknown);
        }

        public ParticleEffect(
            EffectType type, Serial from, Serial to, int itemID, IPoint3D fromPoint, IPoint3D toPoint,
            int speed, int duration, bool fixedDirection, bool explode, int hue, int renderMode, int effect,
            int explodeEffect, int explodeSound, Serial serial, int layer, int unknown
        ) : base(0xC7, 49)
        {
            Stream.Write((byte)type);
            Stream.Write(from);
            Stream.Write(to);
            Stream.Write((short)itemID);
            Stream.Write((short)fromPoint.X);
            Stream.Write((short)fromPoint.Y);
            Stream.Write((sbyte)fromPoint.Z);
            Stream.Write((short)toPoint.X);
            Stream.Write((short)toPoint.Y);
            Stream.Write((sbyte)toPoint.Z);
            Stream.Write((byte)speed);
            Stream.Write((byte)duration);
            Stream.Write((byte)0);
            Stream.Write((byte)0);
            Stream.Write(fixedDirection);
            Stream.Write(explode);
            Stream.Write(hue);
            Stream.Write(renderMode);
            Stream.Write((short)effect);
            Stream.Write((short)explodeEffect);
            Stream.Write((short)explodeSound);
            Stream.Write(serial);
            Stream.Write((byte)layer);
            Stream.Write((short)unknown);
        }
    }

    public class HuedEffect : Packet
    {
        public HuedEffect(
            EffectType type, Serial from, Serial to, int itemID, Point3D fromPoint, Point3D toPoint, int speed,
            int duration, bool fixedDirection, bool explode, int hue, int renderMode
        ) : base(0xC0, 36)
        {
            Stream.Write((byte)type);
            Stream.Write(from);
            Stream.Write(to);
            Stream.Write((short)itemID);
            Stream.Write((short)fromPoint.m_X);
            Stream.Write((short)fromPoint.m_Y);
            Stream.Write((sbyte)fromPoint.m_Z);
            Stream.Write((short)toPoint.m_X);
            Stream.Write((short)toPoint.m_Y);
            Stream.Write((sbyte)toPoint.m_Z);
            Stream.Write((byte)speed);
            Stream.Write((byte)duration);
            Stream.Write((byte)0);
            Stream.Write((byte)0);
            Stream.Write(fixedDirection);
            Stream.Write(explode);
            Stream.Write(hue);
            Stream.Write(renderMode);
        }

        public HuedEffect(
            EffectType type, Serial from, Serial to, int itemID, IPoint3D fromPoint, IPoint3D toPoint,
            int speed, int duration, bool fixedDirection, bool explode, int hue, int renderMode
        ) : base(0xC0, 36)
        {
            Stream.Write((byte)type);
            Stream.Write(from);
            Stream.Write(to);
            Stream.Write((short)itemID);
            Stream.Write((short)fromPoint.X);
            Stream.Write((short)fromPoint.Y);
            Stream.Write((sbyte)fromPoint.Z);
            Stream.Write((short)toPoint.X);
            Stream.Write((short)toPoint.Y);
            Stream.Write((sbyte)toPoint.Z);
            Stream.Write((byte)speed);
            Stream.Write((byte)duration);
            Stream.Write((byte)0);
            Stream.Write((byte)0);
            Stream.Write(fixedDirection);
            Stream.Write(explode);
            Stream.Write(hue);
            Stream.Write(renderMode);
        }
    }

    public sealed class TargetParticleEffect : ParticleEffect
    {
        public TargetParticleEffect(
            IEntity e, int itemID, int speed, int duration, int hue, int renderMode, int effect,
            int layer, int unknown
        ) : base(
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
        )
        {
        }
    }

    public sealed class TargetEffect : HuedEffect
    {
        public TargetEffect(IEntity e, int itemID, int speed, int duration, int hue, int renderMode) : base(
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
        )
        {
        }
    }

    public sealed class LocationParticleEffect : ParticleEffect
    {
        public LocationParticleEffect(
            IEntity e, int itemID, int speed, int duration, int hue, int renderMode, int effect,
            int unknown
        ) : base(
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
        )
        {
        }
    }

    public sealed class LocationEffect : HuedEffect
    {
        public LocationEffect(IPoint3D p, int itemID, int speed, int duration, int hue, int renderMode) : base(
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
        )
        {
        }
    }

    public sealed class MovingParticleEffect : ParticleEffect
    {
        public MovingParticleEffect(
            IEntity from, IEntity to, int itemID, int speed, int duration, bool fixedDirection,
            bool explodes, int hue, int renderMode, int effect, int explodeEffect, int explodeSound, EffectLayer layer,
            int unknown
        ) : base(
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
        )
        {
        }
    }

    public sealed class MovingEffect : HuedEffect
    {
        public MovingEffect(
            IEntity from, IEntity to, int itemID, int speed, int duration, bool fixedDirection,
            bool explodes, int hue, int renderMode
        ) : base(
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
        )
        {
        }
    }

    public enum ScreenEffectType
    {
        FadeOut = 0x00,
        FadeIn = 0x01,
        LightFlash = 0x02,
        FadeInOut = 0x03,
        DarkFlash = 0x04
    }

    public class ScreenEffect : Packet
    {
        public ScreenEffect(ScreenEffectType type)
            : base(0x70, 28)
        {
            Stream.Write((byte)0x04);
            Stream.Fill(8);
            Stream.Write((short)type);
            Stream.Fill(16);
        }
    }

    public sealed class ScreenFadeOut : ScreenEffect
    {
        public static readonly Packet Instance = SetStatic(new ScreenFadeOut());

        public ScreenFadeOut()
            : base(ScreenEffectType.FadeOut)
        {
        }
    }

    public sealed class ScreenFadeIn : ScreenEffect
    {
        public static readonly Packet Instance = SetStatic(new ScreenFadeIn());

        public ScreenFadeIn()
            : base(ScreenEffectType.FadeIn)
        {
        }
    }

    public sealed class ScreenFadeInOut : ScreenEffect
    {
        public static readonly Packet Instance = SetStatic(new ScreenFadeInOut());

        public ScreenFadeInOut()
            : base(ScreenEffectType.FadeInOut)
        {
        }
    }

    public sealed class ScreenLightFlash : ScreenEffect
    {
        public static readonly Packet Instance = SetStatic(new ScreenLightFlash());

        public ScreenLightFlash()
            : base(ScreenEffectType.LightFlash)
        {
        }
    }

    public sealed class ScreenDarkFlash : ScreenEffect
    {
        public static readonly Packet Instance = SetStatic(new ScreenDarkFlash());

        public ScreenDarkFlash()
            : base(ScreenEffectType.DarkFlash)
        {
        }
    }

    public sealed class BoltEffect : Packet
    {
        public BoltEffect(IEntity target, int hue) : base(0xC0, 36)
        {
            Stream.Write((byte)0x01); // type
            Stream.Write(target.Serial);
            Stream.Write(Serial.Zero);
            Stream.Write((short)0); // itemID
            Stream.Write((short)target.X);
            Stream.Write((short)target.Y);
            Stream.Write((sbyte)target.Z);
            Stream.Write((short)target.X);
            Stream.Write((short)target.Y);
            Stream.Write((sbyte)target.Z);
            Stream.Write((byte)0);  // speed
            Stream.Write((byte)0);  // duration
            Stream.Write((short)0); // unk
            Stream.Write(false);    // fixed direction
            Stream.Write(false);    // explode
            Stream.Write(hue);
            Stream.Write(0); // render mode
        }
    }
}
