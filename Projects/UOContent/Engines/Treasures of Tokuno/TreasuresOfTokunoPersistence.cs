using System;

namespace Server.Misc;

[ManualDirtyChecking]
[TypeAlias("Server.Misc.TreasuresOfTokunoPersistance")]
[Obsolete("Deprecated in favor of a configuration file. Only used for legacy deserialization")]
public class TreasuresOfTokunoPersistence : Item
{
    public TreasuresOfTokunoPersistence() : base(1) => Movable = false;

    public TreasuresOfTokunoPersistence(Serial serial) : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);

        var version = reader.ReadInt();

        TreasuresOfTokuno.RewardEra = (TreasuresOfTokunoEra)reader.ReadEncodedInt();
        TreasuresOfTokuno.DropEra = (TreasuresOfTokunoEra)reader.ReadEncodedInt();

        Timer.DelayCall(Delete);
    }
}
