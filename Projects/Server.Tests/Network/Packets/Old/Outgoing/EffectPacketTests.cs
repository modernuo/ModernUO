using System;
using System.Buffers;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
    public class EffectPackets
    {
        [Fact]
        public void TestParticleEffect()
        {
            var effectType = EffectType.Moving;
            Serial serial = 0x4000;
            Serial from = 0x1000;
            Serial to = 0x2000;
            var itemId = 0x100;
            var fromPoint = new Point3D(1000, 100, -10);
            var toPoint = new Point3D(1500, 500, 0);
            byte speed = 3;
            byte duration = 2;
            var direction = false;
            var explode = false;
            var hue = 0x1024;
            var renderMode = 1;
            ushort effect = 3;
            ushort explodeEffect = 0;
            ushort explodeSound = 0;
            byte layer = 9;
            ushort unknown = 0;

            var data = new ParticleEffect(
                effectType,
                from,
                to,
                itemId,
                fromPoint,
                toPoint,
                speed,
                duration,
                direction,
                explode,
                hue,
                renderMode,
                effect,
                explodeEffect,
                explodeSound,
                serial,
                layer,
                unknown
            ).Compile();

            Span<byte> expectedData = stackalloc byte[49];

            var pos = 0;
            expectedData.Write(ref pos, (byte)0xC7); // Packet ID
            expectedData.Write(ref pos, (byte)effectType);
            expectedData.Write(ref pos, from);
            expectedData.Write(ref pos, to);
            expectedData.Write(ref pos, (ushort)itemId);
            expectedData.Write(ref pos, fromPoint);
            expectedData.Write(ref pos, toPoint);
            expectedData.Write(ref pos, speed);
            expectedData.Write(ref pos, duration);
#if NO_LOCAL_INIT
            expectedData.Write(ref pos, (ushort)0);
#else
            pos += 2;
#endif
            expectedData.Write(ref pos, direction);
            expectedData.Write(ref pos, explode);
            expectedData.Write(ref pos, hue);
            expectedData.Write(ref pos, renderMode);
            expectedData.Write(ref pos, effect);
            expectedData.Write(ref pos, explodeEffect);
            expectedData.Write(ref pos, explodeSound);
            expectedData.Write(ref pos, serial);
            expectedData.Write(ref pos, layer);
            expectedData.Write(ref pos, unknown);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestHuedEffect()
        {
            var effectType = EffectType.Moving;
            Serial from = 0x1000;
            Serial to = 0x2000;
            var itemId = 0x100;
            var fromPoint = new Point3D(1000, 100, -10);
            var toPoint = new Point3D(1500, 500, 0);
            byte speed = 3;
            byte duration = 2;
            var direction = false;
            var explode = false;
            var hue = 0x1024;
            var renderMode = 1;

            var data = new HuedEffect(
                effectType,
                from,
                to,
                itemId,
                fromPoint,
                toPoint,
                speed,
                duration,
                direction,
                explode,
                hue,
                renderMode
            ).Compile();

            Span<byte> expectedData = stackalloc byte[36];

            var pos = 0;
            expectedData.Write(ref pos, (byte)0xC0); // Packet ID
            expectedData.Write(ref pos, (byte)effectType);
            expectedData.Write(ref pos, from);
            expectedData.Write(ref pos, to);
            expectedData.Write(ref pos, (ushort)itemId);
            expectedData.Write(ref pos, fromPoint);
            expectedData.Write(ref pos, toPoint);
            expectedData.Write(ref pos, speed);
            expectedData.Write(ref pos, duration);
#if NO_LOCAL_INIT
            expectedData.Write(ref pos, (ushort)0);
#else
            pos += 2;
#endif
            expectedData.Write(ref pos, direction);
            expectedData.Write(ref pos, explode);
            expectedData.Write(ref pos, hue);
            expectedData.Write(ref pos, renderMode);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestScreenEffect()
        {
            var type = ScreenEffectType.FadeOut;
            var data = new ScreenEffect(type).Compile();

            Span<byte> expectedData = stackalloc byte[28];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x70); // Packet ID
            expectedData.Write(ref pos, (byte)0x04); // Effect
#if NO_LOCAL_INIT
            expectedData.Write(ref pos, 0);
            expectedData.Write(ref pos, 0);
#else
            pos += 8;
#endif

            expectedData.Write(ref pos, (ushort)type); // Screen Effect Type

#if NO_LOCAL_INIT
            expectedData.Write(ref pos, 0);
            expectedData.Write(ref pos, 0);
            expectedData.Write(ref pos, 0);
            expectedData.Write(ref pos, 0);
#endif

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestBoltEffect()
        {
            IEntity entity = new Entity(0x1000, new Point3D(1000, 100, -10), Map.Felucca);
            var hue = 0x1024;
            var data = new BoltEffect(entity, hue).Compile();

            Span<byte> expectedData = stackalloc byte[36];
            var pos = 0;
            expectedData.Write(ref pos, (byte)0xC0); // Packet ID
            expectedData.Write(ref pos, (byte)0x01); // Effect


            expectedData.Write(ref pos, entity.Serial);
#if NO_LOCAL_INIT
            expectedData.Write(ref pos, 0);
            expectedData.Write(ref pos, (ushort)0);
#else
            pos += 6;
#endif
            expectedData.Write(ref pos, entity.Location);
            expectedData.Write(ref pos, entity.Location);
#if NO_LOCAL_INIT
            expectedData.Write(ref pos, 0);
            expectedData.Write(ref pos, (ushort)0);
#else
            pos += 6;
#endif
            expectedData.Write(ref pos, hue);

            AssertThat.Equal(data, expectedData);
        }
    }
}
