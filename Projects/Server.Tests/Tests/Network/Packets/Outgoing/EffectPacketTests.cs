using System;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    public class EffectPackets
    {
        [Theory, InlineData(10, 1000, 10, 5)]
        public void TestSoundEffect(ushort soundID, int x, int y, int z)
        {
            var p = new Point3D(x, y, z);

            var expected = new PlaySound(soundID, p).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendSoundEffect(soundID, p);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestParticleEffect()
        {
            var effectType = EffectType.Moving;
            Serial serial = (Serial)0x4000;
            Serial from = (Serial)0x1000;
            Serial to = (Serial)0x2000;
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

            var expected = new ParticleEffect(
                effectType, from, to, itemId, fromPoint, toPoint, speed, duration, direction,
                explode, hue, renderMode, effect, explodeEffect, explodeSound, serial, layer,
                unknown
            ).Compile();

            Span<byte> actual = stackalloc byte[OutgoingEffectPackets.ParticleEffectLength];
            OutgoingEffectPackets.CreateParticleEffect(
                actual,
                effectType, from, to, itemId, fromPoint, toPoint, speed, duration, direction,
                explode, hue, renderMode, effect, explodeEffect, explodeSound, serial, layer,
                unknown
            );

            AssertThat.Equal(actual, expected);
        }

        [Fact]
        public void TestHuedEffect()
        {
            var effectType = EffectType.Moving;
            Serial from = (Serial)0x1000;
            Serial to = (Serial)0x2000;
            var itemId = 0x100;
            var fromPoint = new Point3D(1000, 100, -10);
            var toPoint = new Point3D(1500, 500, 0);
            byte speed = 3;
            byte duration = 2;
            var direction = false;
            var explode = false;
            var hue = 0x1024;
            var renderMode = 1;

            var expected = new HuedEffect(
                effectType, from, to, itemId, fromPoint, toPoint, speed,
                duration, direction, explode, hue, renderMode
            ).Compile();

            Span<byte> actual = stackalloc byte[OutgoingEffectPackets.HuedEffectLength];
            OutgoingEffectPackets.CreateHuedEffect(
                actual,
                effectType, from, to, itemId, fromPoint, toPoint, speed,
                duration, direction, explode, hue, renderMode
            );

            AssertThat.Equal(actual, expected);
        }

        [Theory, InlineData(ScreenEffectType.DarkFlash), InlineData(ScreenEffectType.FadeInOut)]
        public void TestScreenEffect(ScreenEffectType screenType)
        {
            var expected = new ScreenEffect(screenType).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendScreenEffect(screenType);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestBoltEffect()
        {
            IEntity entity = new Entity((Serial)0x1000, new Point3D(1000, 100, -10), Map.Felucca);
            var hue = 0x1024;
            var expected = new BoltEffect(entity, hue).Compile();

            Span<byte> actual = stackalloc byte[OutgoingEffectPackets.BoltEffectLength];
            OutgoingEffectPackets.CreateBoltEffect(actual, entity, hue);

            AssertThat.Equal(actual, expected);
        }
    }
}
