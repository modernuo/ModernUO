using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Misc;
using Server.Mobiles;

namespace Server.Spells.Seventh;

public class EnergyFieldSpell : MagerySpell, ITargetingSpell<IPoint3D>
{
    private static readonly SpellInfo _info = new(
        "Energy Field",
        "In Sanct Grav",
        221,
        9022,
        false,
        Reagent.BlackPearl,
        Reagent.MandrakeRoot,
        Reagent.SpidersSilk,
        Reagent.SulfurousAsh
    );

    public EnergyFieldSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Seventh;

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

            TimeSpan duration = Core.AOS
                ? TimeSpan.FromSeconds((15 + Caster.Skills.Magery.Value * 2) / 7.0)
                : TimeSpan.FromSeconds(Caster.Skills.Magery.Value * 0.28 + 2.0);

            var itemID = eastToWest ? 0x3946 : 0x3956;

            for (var i = -2; i <= 2; ++i)
            {
                var targetLoc = new Point3D(eastToWest ? loc.X + i : loc.X, eastToWest ? loc.Y : loc.Y + i, loc.Z);
                var canFit = SpellHelper.AdjustField(ref targetLoc, Caster.Map, 12, false);

                if (!canFit)
                {
                    continue;
                }

                new EnergyField(targetLoc, Caster.Map, duration, itemID, Caster).ProcessDelta();

                Effects.SendLocationParticles(
                    EffectItem.Create(targetLoc, Caster.Map, EffectItem.DefaultDuration),
                    0x376A,
                    9,
                    10,
                    5051
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
[SerializationGenerator(1, false)]
public partial class EnergyField : Item
{
    [SerializableField(0)]
    private Mobile _caster;

    [DeltaDateTime]
    [SerializableField(1)]
    private DateTime _end;

    private TimerExecutionToken _timer;

    public EnergyField(Point3D loc, Map map, TimeSpan duration, int itemID, Mobile caster) : base(itemID)
    {
        Visible = false;
        Movable = false;
        Light = LightType.Circle300;

        MoveToWorld(loc, map);

        _caster = caster;

        if (caster.InLOS(this))
        {
            Visible = true;
        }
        else
        {
            Delete();
            return;
        }

        Timer.StartTimer(duration, Delete, out _timer);
    }

    public override bool BlocksFit => true;

    private void Deserialize(IGenericReader reader, int version)
    {
        // Do nothing, no caster
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        var duration = _end > DateTime.MinValue ? _end - Core.Now : TimeSpan.FromSeconds(5);
        Timer.StartTimer(duration, Delete, out _timer);
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (m is not PlayerMobile)
        {
            return base.OnMoveOver(m);
        }

        var noto = Notoriety.Compute(_caster, m);
        return noto != Notoriety.Enemy && noto != Notoriety.Ally && base.OnMoveOver(m);
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();
        _timer.Cancel();
    }
}
