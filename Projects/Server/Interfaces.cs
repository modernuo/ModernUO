using System;

namespace Server;

public interface IPoint2D
{
    int X { get; }
    int Y { get; }
}

public interface IPoint3D : IPoint2D
{
    int Z { get; }
}

public interface ICarvable
{
    void Carve(Mobile from, Item item);
}

public interface IWeapon
{
    int MaxRange { get; }
    void OnBeforeSwing(Mobile attacker, Mobile defender);
    TimeSpan OnSwing(Mobile attacker, Mobile defender, double damageBonus = 1.0);
    void GetStatusDamage(Mobile from, out int min, out int max);
}

public interface IHued
{
    int HuedItemID { get; }
}

public interface ISpell
{
    bool IsCasting { get; }
    void OnCasterHurt();
    void OnCasterKilled();
    void OnConnectionChanged();
    bool OnCasterMoving(Direction d);
    bool OnCasterEquipping(Item item);
    bool OnCasterUsingObject(IEntity entity);
    bool OnCastInTown(Region r);
    void FinishSequence();
}

public interface IParty
{
    void OnStamChanged(Mobile m);
    void OnManaChanged(Mobile m);
    void OnStatsQuery(Mobile beholder, Mobile beheld);
}

// TODO: Add SpawnMap and change Spawner.Map to use it
public interface ISpawner : IEntity
{
    Guid Guid { get; }
    bool UnlinkOnTaming { get; }
    Point3D HomeLocation { get; }
    int HomeRange { get; }
    Region Region { get; }
    bool ReturnOnDeactivate { get; }

    void Remove(ISpawnable spawn);
    Point3D GetSpawnPosition(ISpawnable spawned, Map map);
    void Respawn();
}

public interface ISpawnable : IEntity
{
    ISpawner Spawner { get; set; }
    void OnBeforeSpawn(Point3D location, Map map);
    void OnAfterSpawn();
}
