using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Misc;
using Server.Mobiles;

namespace Server.Spells.Sixth;

public class ParalyzeFieldSpell : MagerySpell, ITargetingSpell<IPoint3D>
{
    private static readonly SpellInfo _info = new(
        "Paralyze Field",
        "In Ex Grav",
        230,
        9012,
        false,
        Reagent.BlackPearl,
        Reagent.Ginseng,
        Reagent.SpidersSilk
    );

    public ParalyzeFieldSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Sixth;

    public int TargetRange => Core.T2A ? 15 : 18;

    public void Target(IPoint3D p)
    {
        if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
        {
            SpellHelper.Turn(Caster, p);
            SpellHelper.GetSurfaceTop(ref p);

            var loc = new Point3D(p);
            var eastToWest = SpellHelper.GetEastToWest(Caster.Location, loc);

            Effects.PlaySound(loc, Caster.Map, 0x20B);

            var itemID = eastToWest ? 0x3967 : 0x3979;

            var duration = Core.Expansion switch
            {
                Expansion.None  => TimeSpan.FromSeconds(20),
                < Expansion.LBR => TimeSpan.FromSeconds(15.0 + Caster.Skills.Magery.Value / 3.0),
                _               => TimeSpan.FromSeconds(3.0 + Caster.Skills.Magery.Value / 3.0)
            };

            for (var i = -2; i <= 2; ++i)
            {
                var targetLoc = new Point3D(eastToWest ? loc.X + i : loc.X, eastToWest ? loc.Y : loc.Y + i, loc.Z);

                if (!SpellHelper.AdjustField(ref targetLoc, Caster.Map, 12, false))
                {
                    continue;
                }

                new ParalyzeField(Caster, itemID, targetLoc, Caster.Map, duration).ProcessDelta();

                Effects.SendLocationParticles(
                    EffectItem.Create(targetLoc, Caster.Map, EffectItem.DefaultDuration),
                    0x376A,
                    9,
                    10,
                    5048
                );
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
public partial class ParalyzeField : Item
{
    [SerializableField(0)]
    private Mobile _caster;

    [DeltaDateTime]
    [SerializableField(1)]
    private DateTime _end;

    private TimerExecutionToken _timer;

    public ParalyzeField(Mobile caster, int itemID, Point3D loc, Map map, TimeSpan duration) : base(itemID)
    {
        Visible = false;
        Movable = false;
        Light = LightType.Circle300;

        MoveToWorld(loc, map);

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

        _caster = caster;

        Timer.StartTimer(duration, Delete, out _timer);
        _end = Core.Now + duration;
    }

    public override bool BlocksFit => true;

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

    public override bool OnMoveOver(Mobile m)
    {
        if (Visible && _caster != null && (!Core.AOS || m != _caster) &&
            SpellHelper.ValidIndirectTarget(_caster, m) && _caster.CanBeHarmful(m, false))
        {
            if (SpellHelper.CanRevealCaster(m))
            {
                _caster.RevealingAction();
            }

            _caster.DoHarmful(m);

            double duration;

            if (Core.AOS)
            {
                duration = Math.Max(
                    2.0 + ((int)(_caster.Skills.EvalInt.Value / 10) - (int)(m.Skills.MagicResist.Value / 10)),
                    0.0
                );

                if (!m.Player)
                {
                    duration *= 3.0;
                }
            }
            else
            {
                duration = 7.0 + _caster.Skills.Magery.Value / 5;
            }

            m.Paralyze(TimeSpan.FromSeconds(duration));

            m.PlaySound(0x204);
            m.FixedEffect(0x376A, 10, 16);

            (m as BaseCreature)?.OnHarmfulSpell(_caster);
        }

        return true;
    }
}
