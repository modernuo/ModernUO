using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Engines.Quests.Necro;

[SerializationGenerator(0, false)]
public partial class MaabusCoffin : BaseAddon
{
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [SerializableField(0, setter: "private")]
    private Maabus _maabus;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _spawnLocation;

    [Constructible]
    public MaabusCoffin()
    {
        AddComponent(new MaabusCoffinComponent(0x1C2B, 0x1C2B), -1, -1, 0);

        AddComponent(new MaabusCoffinComponent(0x1D16, 0x1C2C), 0, -1, 0);
        AddComponent(new MaabusCoffinComponent(0x1D17, 0x1C2D), 1, -1, 0);
        AddComponent(new MaabusCoffinComponent(0x1D51, 0x1C2E), 2, -1, 0);

        AddComponent(new MaabusCoffinComponent(0x1D4E, 0x1C2A), 0, 0, 0);
        AddComponent(new MaabusCoffinComponent(0x1D4D, 0x1C29), 1, 0, 0);
        AddComponent(new MaabusCoffinComponent(0x1D4C, 0x1C28), 2, 0, 0);
    }

    public void Awake(Mobile caller)
    {
        if (_maabus != null || _spawnLocation == Point3D.Zero)
        {
            return;
        }

        foreach (var c in Components)
        {
            (c as MaabusCoffinComponent)?.TurnToEmpty();
        }

        Maabus = new Maabus { Location = _spawnLocation, Map = Map };
        Maabus.Direction = Maabus.GetDirectionTo(caller);

        Timer.StartTimer(TimeSpan.FromSeconds(7.5), BeginSleep);
    }

    public void BeginSleep()
    {
        if (_maabus == null)
        {
            return;
        }

        Effects.PlaySound(_maabus.Location, _maabus.Map, 0x48E);

        Timer.StartTimer(TimeSpan.FromSeconds(2.5), Sleep);
    }

    public void Sleep()
    {
        if (_maabus == null)
        {
            return;
        }

        Effects.SendLocationParticles(
            EffectItem.Create(_maabus.Location, _maabus.Map, EffectItem.DefaultDuration),
            0x3728,
            10,
            10,
            0x7E7
        );
        Effects.PlaySound(_maabus.Location, _maabus.Map, 0x1FE);

        Maabus.Delete();
        Maabus = null;

        foreach (var c in Components)
        {
            (c as MaabusCoffinComponent)?.TurnToFull();
        }
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        Sleep();
    }
}

[SerializationGenerator(0, false)]
public partial class MaabusCoffinComponent : AddonComponent
{
    [SerializableField(0)]
    private int _fullItemID;

    [SerializableField(1)]
    private int _emptyItemID;

    public MaabusCoffinComponent(int itemID) : this(itemID, itemID)
    {
    }

    public MaabusCoffinComponent(int fullItemID, int emptyItemID) : base(fullItemID)
    {
        _fullItemID = fullItemID;
        _emptyItemID = emptyItemID;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public Point3D SpawnLocation
    {
        get => Addon is MaabusCoffin coffin ? coffin.SpawnLocation : Point3D.Zero;
        set
        {
            if (Addon is MaabusCoffin coffin)
            {
                coffin.SpawnLocation = value;
            }
        }
    }

    public void TurnToEmpty()
    {
        ItemID = _emptyItemID;
    }

    public void TurnToFull()
    {
        ItemID = _fullItemID;
    }
}
