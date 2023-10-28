using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class Acid : Item
{
    private TimeSpan _duration;
    private int _maxDamage;
    private int _minDamage;
    private TimerExecutionToken _timerToken;
    private bool _drying;

    [Constructible]
    public Acid() : this(TimeSpan.FromSeconds(10.0), 2, 5)
    {
    }

    [Constructible]
    public Acid(TimeSpan duration, int minDamage, int maxDamage) : this(duration, TimeSpan.FromSeconds(1), minDamage, maxDamage)
    {
    }

    [Constructible]
    public Acid(TimeSpan duration, TimeSpan tickRate, int minDamage, int maxDamage) : base(0x122A)
    {
        Hue = 0x3F;
        Movable = false;

        _minDamage = minDamage;
        _maxDamage = maxDamage;
        _duration = duration;

        Timer.StartTimer(TimeSpan.Zero, tickRate, OnTick, out _timerToken);
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

        // Dries half way through duration
        if (!_drying && age > _duration - age)
        {
            _drying = true;
            ItemID = 0x122B;
        }

        using var queue = PooledRefQueue<Mobile>.Create();

        foreach (var m in GetMobilesAt())
        {
            if (m.AccessLevel == AccessLevel.Player &&
                m.Alive && !m.IsDeadBondedPet && (m is not BaseCreature bc || bc.Controlled || bc.Summoned))
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

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        Delete();
    }
}
