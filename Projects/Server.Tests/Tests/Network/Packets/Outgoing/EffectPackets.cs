namespace Server.Network;

public sealed class PlaySound : Packet
{
    public PlaySound(int soundID, IPoint3D target) : base(0x54, 12)
    {
        Stream.Write((byte)1); // flags
        Stream.Write((short)soundID);
        Stream.Write((short)0); // volume
        Stream.Write((short)target.X);
        Stream.Write((short)target.Y);
        Stream.Write((short)target.Z);
    }
}

public class ParticleEffect : Packet
{
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