using System;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class EffectPackets
{
    [Fact]
    public void TestSoundEffect()
    {
        const ushort soundID = 10;
        var p = new Point3D(1000, 10, 5);

        PacketAssert.Sends(new PlaySound(soundID, p), ns => ns.SendSoundEffect(soundID, p));
    }

    [Fact]
    public void TestParticleEffect()
    {
        var effectType = EffectType.Moving;
        var serial = (Serial)0x4000;
        var from = (Serial)0x1000;
        var to = (Serial)0x2000;
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
        var from = (Serial)0x1000;
        var to = (Serial)0x2000;
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
        PacketAssert.Sends(new ScreenEffect(screenType), ns => ns.SendScreenEffect(screenType));
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
