using System;
using Server.Collections;
using Server.Targeting;

namespace Server.Spells.Mysticism;

public class MassSleepSpell : MysticSpell, ITargetingSpell<IPoint3D>
{
    private const int Radius = 3;

    private static readonly SpellInfo _info = new(
        "Mass Sleep",
        "Vas Zu",
        230,
        9022,
        Reagent.Ginseng,
        Reagent.Nightshade,
        Reagent.SpidersSilk
    );

    public MassSleepSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Fifth;

    public void Target(IPoint3D point)
    {
        var location = (point as Item)?.GetWorldLocation() ?? new Point3D(point);

        if (!SpellHelper.CheckTown(location, Caster) || !CheckSequence())
        {
            return;
        }

        SpellHelper.Turn(Caster, point);

        var map = Caster.Map;

        if (map == null)
        {
            return;
        }

        using var targets = PooledRefQueue<Mobile>.Create();

        foreach (var target in map.GetMobilesInRange(location, Radius))
        {
            // Sleep is explicitly allowed to affect hidden players. The location target still
            // passes the normal target cursor, map, range, town, and LOS validation first.
            if (target == Caster || !SpellHelper.ValidIndirectTarget(Caster, target) || !Caster.CanBeHarmful(target, false) ||
                !Caster.InLOS(target))
            {
                continue;
            }

            targets.Enqueue(target);
        }

        while (targets.Count > 0)
        {
            var target = targets.Dequeue();
            var duration = SleepSpell.GetDuration(Caster, target);

            if (duration <= TimeSpan.Zero)
            {
                continue;
            }

            Caster.DoHarmful(target);
            SleepSpell.Apply(Caster, target, duration);
        }
    }

    public override void OnCast()
    {
        Caster.Target = new SpellTarget<IPoint3D>(this, allowGround: true);
    }
}
