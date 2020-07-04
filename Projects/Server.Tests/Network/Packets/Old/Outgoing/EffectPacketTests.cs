using System;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
  public class EffectPackets
  {
    [Fact]
    public void TestParticleEffect()
    {
      EffectType effectType = EffectType.Moving;
      Serial serial = 0x4000;
      Serial from = 0x1000;
      Serial to = 0x2000;
      var itemId = 0x100;
      Point3D fromPoint = new Point3D(1000, 100, -10);
      Point3D toPoint = new Point3D(1500, 500, 0);
      byte speed = 3;
      byte duration = 2;
      bool direction = false;
      bool explode = false;
      int hue = 0x1024;
      int renderMode = 1;
      ushort effect = 3;
      ushort explodeEffect = 0;
      ushort explodeSound = 0;
      byte layer = 9;
      ushort unknown = 0;

      Span<byte> data = new ParticleEffect(
        effectType, from, to, itemId,
        fromPoint, toPoint, speed, duration,
        direction, explode, hue, renderMode,
        effect, explodeEffect, explodeSound, serial,
        layer, unknown
      ).Compile();

      Span<byte> expectedData = stackalloc byte[49];

      int pos = 0;
      ((byte)0xC7).CopyTo(ref pos, expectedData); // Packet ID
      ((byte)effectType).CopyTo(ref pos, expectedData);
      from.CopyTo(ref pos, expectedData);
      to.CopyTo(ref pos, expectedData);
      ((ushort)itemId).CopyTo(ref pos, expectedData);
      ((ushort)fromPoint.X).CopyTo(ref pos, expectedData);
      ((ushort)fromPoint.Y).CopyTo(ref pos, expectedData);
      ((byte)fromPoint.Z).CopyTo(ref pos, expectedData);
      ((ushort)toPoint.X).CopyTo(ref pos, expectedData);
      ((ushort)toPoint.Y).CopyTo(ref pos, expectedData);
      ((byte)toPoint.Z).CopyTo(ref pos, expectedData);
      speed.CopyTo(ref pos, expectedData);
      duration.CopyTo(ref pos, expectedData);
      expectedData.Clear(ref pos, 2);
      direction.CopyTo(ref pos, expectedData);
      explode.CopyTo(ref pos, expectedData);
      hue.CopyTo(ref pos, expectedData);
      renderMode.CopyTo(ref pos, expectedData);
      effect.CopyTo(ref pos, expectedData);
      explodeEffect.CopyTo(ref pos, expectedData);
      explodeSound.CopyTo(ref pos, expectedData);
      serial.CopyTo(ref pos, expectedData);
      layer.CopyTo(ref pos, expectedData);
      unknown.CopyTo(ref pos, expectedData);

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestHuedEffect()
    {
      EffectType effectType = EffectType.Moving;
      Serial serial = 0x4000;
      Serial from = 0x1000;
      Serial to = 0x2000;
      var itemId = 0x100;
      Point3D fromPoint = new Point3D(1000, 100, -10);
      Point3D toPoint = new Point3D(1500, 500, 0);
      byte speed = 3;
      byte duration = 2;
      bool direction = false;
      bool explode = false;
      int hue = 0x1024;
      int renderMode = 1;

      Span<byte> data = new HuedEffect(
        effectType, from, to, itemId,
        fromPoint, toPoint, speed, duration,
        direction, explode, hue, renderMode
      ).Compile();

      Span<byte> expectedData = stackalloc byte[36];

      int pos = 0;
      ((byte)0xC0).CopyTo(ref pos, expectedData); // Packet ID
      ((byte)effectType).CopyTo(ref pos, expectedData);
      from.CopyTo(ref pos, expectedData);
      to.CopyTo(ref pos, expectedData);
      ((ushort)itemId).CopyTo(ref pos, expectedData);
      ((ushort)fromPoint.X).CopyTo(ref pos, expectedData);
      ((ushort)fromPoint.Y).CopyTo(ref pos, expectedData);
      ((byte)fromPoint.Z).CopyTo(ref pos, expectedData);
      ((ushort)toPoint.X).CopyTo(ref pos, expectedData);
      ((ushort)toPoint.Y).CopyTo(ref pos, expectedData);
      ((byte)toPoint.Z).CopyTo(ref pos, expectedData);
      speed.CopyTo(ref pos, expectedData);
      duration.CopyTo(ref pos, expectedData);
      expectedData.Clear(ref pos, 2);
      direction.CopyTo(ref pos, expectedData);
      explode.CopyTo(ref pos, expectedData);
      hue.CopyTo(ref pos, expectedData);
      renderMode.CopyTo(ref pos, expectedData);

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestScreenEffect()
    {
      var type = ScreenEffectType.FadeOut;
      Span<byte> data = new ScreenEffect(type).Compile();

      Span<byte> expectedData = stackalloc byte[28]
      {
        0x70, // Packet ID
        0x04,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, // Screen Effect Type
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00
      };

      ((ushort)type).CopyTo(expectedData.Slice(10, 2));

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestBoltEffect()
    {
      IEntity entity = new Entity(0x1000, new Point3D(1000, 100, -10), Map.Felucca);
      var hue = 0x1024;
      Span<byte> data = new BoltEffect(entity, hue).Compile();

      Span<byte> expectedData = stackalloc byte[36]
      {
        0xC0, // Packet ID
        0x01, // Effect
        0x00, 0x00, 0x00, 0x00, // From Serial
        0x00, 0x00, 0x00, 0x00, // To Serial
        0x00, 0x00, // Item ID
        0x00, 0x00, 0x00, 0x00, 0x00, // From Point
        0x00, 0x00, 0x00, 0x00, 0x00, // To Point
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, // Hue
        0x00, 0x00, 0x00, 0x00
      };

      int pos = 2;
      entity.Serial.CopyTo(ref pos, expectedData);
      pos += 6;
      ((ushort)entity.X).CopyTo(ref pos, expectedData);
      ((ushort)entity.Y).CopyTo(ref pos, expectedData);
      ((byte)entity.Z).CopyTo(ref pos, expectedData);
      ((ushort)entity.X).CopyTo(ref pos, expectedData);
      ((ushort)entity.Y).CopyTo(ref pos, expectedData);
      ((byte)entity.Z).CopyTo(ref pos, expectedData);
      pos += 6;
      hue.CopyTo(ref pos, expectedData);

      AssertThat.Equal(data, expectedData);
    }
  }
}
