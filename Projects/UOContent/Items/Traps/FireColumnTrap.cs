using System;
using ModernUO.Serialization;
using Server.Spells;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class FireColumnTrap : BaseTrap
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _warningFlame;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _minDamage;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _maxDamage;

    [Constructible]
    public FireColumnTrap() : base(0x1B71)
    {
        _minDamage = 10;
        _maxDamage = 40;

        _warningFlame = true;
    }

    public override bool PassivelyTriggered => true;
    public override TimeSpan PassiveTriggerDelay => TimeSpan.FromSeconds(2.0);
    public override int PassiveTriggerRange => 3;
    public override TimeSpan ResetDelay => TimeSpan.FromSeconds(0.5);

    public override void OnTrigger(Mobile from)
    {
        if (from.AccessLevel > AccessLevel.Player)
        {
            return;
        }

        if (WarningFlame)
        {
            DoEffect();
        }

        if (from.Alive && CheckRange(from.Location, 0))
        {
            SpellHelper.Damage(
                TimeSpan.FromSeconds(0.5),
                from,
                from,
                Utility.RandomMinMax(MinDamage, MaxDamage),
                0,
                100,
                0,
                0,
                0
            );

            if (!WarningFlame)
            {
                DoEffect();
            }
        }
    }

    private void DoEffect()
    {
        Effects.SendLocationParticles(
            EffectItem.Create(Location, Map, EffectItem.DefaultDuration),
            0x3709,
            10,
            30,
            5052
        );
        Effects.PlaySound(Location, Map, 0x225);
    }
}
