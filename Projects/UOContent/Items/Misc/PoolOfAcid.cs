using System;
using Server.Collections;
using Server.Mobiles;

namespace Server.Items;

[ManualDirtyChecking]
[TypeAlias("Server.Items.AcidSlime")]
public class PoolOfAcid : Item
{
    private readonly TimeSpan _duration;
    private readonly int _maxDamage;
    private readonly int _minDamage;
    private TimerExecutionToken _timerToken;
    private bool _drying;

    [Constructible]
    public PoolOfAcid() : this(TimeSpan.FromSeconds(10.0), 2, 5)
    {
    }

    [Constructible]
    public PoolOfAcid(TimeSpan duration, int minDamage, int maxDamage) : base(0x122A)
    {
        Hue = 0x3F;
        Movable = false;

        _minDamage = minDamage;
        _maxDamage = maxDamage;
        _duration = duration;

        Timer.StartTimer(TimeSpan.Zero, TimeSpan.FromSeconds(1), OnTick, out _timerToken);
    }

    public PoolOfAcid(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "a pool of acid";

    public override void OnDelete()
    {
        _timerToken.Cancel();
    }

    private void OnTick()
    {
        var now = Core.Now;
        var age = now - Created;

        if (age > _duration)
        {
            Delete();
            return;
        }

        if (!_drying && age > _duration - age)
        {
            _drying = true;
            ItemID = 0x122B;
        }

        using var queue = PooledRefQueue<Mobile>.Create();

        foreach (var m in GetMobilesInRange(0))
        {
            if (m.Alive && !m.IsDeadBondedPet && (m is not BaseCreature bc || bc.Controlled || bc.Summoned))
            {
                queue.Enqueue(m);
            }
        }

        while (queue.Count > 0)
        {
            Damage(queue.Dequeue());
        }
    }

    public override bool OnMoveOver(Mobile m)
    {
        Damage(m);
        return true;
    }

    public void Damage(Mobile m)
    {
        m.Damage(Utility.RandomMinMax(_minDamage, _maxDamage));
    }

    public override void Serialize(IGenericWriter writer)
    {
    }

    public override void Deserialize(IGenericReader reader)
    {
    }
}
