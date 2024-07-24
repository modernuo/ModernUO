using System;
using Server.Ethics.Evil;
using Server.Ethics.Hero;

namespace Server.Ethics;

public class EthicsSystem : GenericEntityPersistence<EthicsEntity>
{
    private static EthicsSystem _persistence;

    public static void Configure()
    {
        _persistence = new EthicsSystem();
    }

    public EthicsSystem() : base("Ethics", 3, 0x1, 0x7FFFFFFF)
    {
    }

    public static Serial NewProfile => _persistence.NewEntity;

    public static void Add(EthicsEntity entity) => _persistence.AddEntity(entity);

    public static void Remove(EthicsEntity entity) => _persistence.RemoveEntity(entity);

    public static T Find<T>(Serial serial) where T : EthicsEntity => _persistence.FindEntity<T>(serial);
}

[ManualDirtyChecking]
[TypeAlias("Server.Ethics.EthicsPersistance")]
[Obsolete("Deprecated in favor of the static system. Only used for legacy deserialization")]
public class EthicsPersistence : Item
{
    public EthicsPersistence()
    {
        Delete();
    }

    public EthicsPersistence(Serial serial) : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);

        var heroEthic = new HeroEthic();
        heroEthic.Deserialize(reader);

        var evilEthic = new EvilEthic();
        evilEthic.Deserialize(reader);

        Timer.DelayCall(Delete);
    }
}
