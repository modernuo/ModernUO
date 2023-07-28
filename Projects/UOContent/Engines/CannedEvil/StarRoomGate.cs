using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(1, false)]
public partial class StarRoomGate : Moongate
{
    private static TimeSpan GateDuration = TimeSpan.FromMinutes(2.0);

    [SerializableField(0)]
    private bool _decays;

    [DeltaDateTime]
    [SerializableField(1)]
    private DateTime _decayTime;

    private Timer _timer;

    [Constructible]
    public StarRoomGate(Point3D loc, Map map, bool decays) : this(decays)
    {
        MoveToWorld(loc, map);
        Effects.PlaySound(loc, map, 0x20E);
    }

    [Constructible]
    public StarRoomGate(bool decays = false) : base(new Point3D(5143, 1774, 0), Map.Felucca)
    {
        Dispellable = false;
        ItemID = 0x1FD4;

        if (decays)
        {
            _decays = true;
            _decayTime = Core.Now + GateDuration;

            _timer = Timer.DelayCall(GateDuration, Delete);
        }
    }

    public override int LabelNumber => 1049498; // dark moongate

    public override void OnAfterDelete()
    {
        _timer?.Stop();
        base.OnAfterDelete();
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _decays = reader.ReadBool();

        if (_decays)
        {
            _decayTime = reader.ReadDeltaTime();
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_decays)
        {
            _timer = Timer.DelayCall(_decayTime - Core.Now, Delete);
        }
    }
}
