using System;
using ModernUO.Serialization;
using Server.Misc;
using Server.Mobiles;

namespace Server.Spells.Third;

public class WallOfStoneSpell : MagerySpell, ITargetingSpell<IPoint3D>
{
    private static readonly SpellInfo _info = new(
        "Wall of Stone",
        "In Sanct Ylem",
        227,
        9011,
        false,
        Reagent.Bloodmoss,
        Reagent.Garlic
    );

    public WallOfStoneSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Third;

    public int TargetRange => Core.T2A ? 15 : 18;

    public void Target(IPoint3D p)
    {
        if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
        {
            SpellHelper.Turn(Caster, p);

            SpellHelper.GetSurfaceTop(ref p);

            var loc = new Point3D(p);

            var eastToWest = SpellHelper.GetEastToWest(Caster.Location, loc);

            Effects.PlaySound(loc, Caster.Map, 0x1F6);

            for (var i = -1; i <= 1; ++i)
            {
                var targetLoc = new Point3D(eastToWest ? p.X + i : p.X, eastToWest ? p.Y : p.Y + i, p.Z);
                var canFit = SpellHelper.AdjustField(ref targetLoc, Caster.Map, 22, true);

                if (!canFit)
                {
                    continue;
                }

                var item = new WallOfStone(targetLoc, Caster.Map, Caster);

                Effects.SendLocationParticles(item, 0x376A, 9, 10, 5025);
            }
        }
    }

    public override void OnCast()
    {
        Caster.Target = new SpellTarget<IPoint3D>(this, allowGround: true);
    }
}

[DispellableField]
[SerializationGenerator(0, false)]
public partial class WallOfStone : Item
{
    [SerializableField(0)]
    private Mobile _caster;

    [DeltaDateTime]
    [SerializableField(1)]
    private DateTime _end;

    private TimerExecutionToken _timer;

    public WallOfStone(Point3D loc, Map map, Mobile caster) : base(0x82)
    {
        Visible = false;
        Movable = false;

        MoveToWorld(loc, map);

        _caster = caster;

        if (caster.InLOS(this))
        {
            Visible = true;
        }
        else
        {
            Delete();
        }

        if (Deleted)
        {
            return;
        }

        var duration = TimeSpan.FromSeconds(10.0);
        Timer.StartTimer(duration, Delete, out _timer);
        _end = Core.Now + duration;
    }
    public override bool BlocksFit => true;

    public override bool OnMoveOver(Mobile m)
    {
        if (m is PlayerMobile)
        {
            var noto = Notoriety.Compute(_caster, m);
            if (noto is Notoriety.Enemy or Notoriety.Ally)
            {
                return false;
            }
        }

        return base.OnMoveOver(m);
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _timer.Cancel();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Timer.StartTimer(_end - Core.Now, Delete, out _timer);
    }
}
