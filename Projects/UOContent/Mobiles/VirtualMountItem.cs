using System;
using ModernUO.Serialization;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class VirtualMountItem : Item, IMountItem, IMount
{
    public VirtualMountItem(Mobile mob) : base(0x3EA0)
    {
        Layer = Layer.Mount;
        _rider = mob;
    }

    public IMount Mount => this;

    [SerializableProperty(0)]
    public Mobile Rider
    {
        get => _rider;
        set { }
    }

    [AfterDeserialization(false)]
    private void AfterDeserialize()
    {
        if (_rider == null)
        {
            Delete();
        }
    }

    public int Steps { get; set; }

    public int StepsMax => 400;
    public int StepsGainedPerIdleTime => 1;
    public TimeSpan IdleTimePerStepsGain => TimeSpan.FromSeconds(10);

    public void OnRiderDamaged(int amount, Mobile from, bool willKill)
    {
    }

    public override DeathMoveResult OnParentDeath(Mobile parent)
    {
        Delete();
        return DeathMoveResult.RemainEquipped;
    }
}
