using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.Items;
using Server.Misc;
using Server.Mobiles;

namespace Server.Spells.Fifth;

public class PoisonFieldSpell : MagerySpell, ITargetingSpell<IPoint3D>
{
    private static readonly SpellInfo _info = new(
        "Poison Field",
        "In Nox Grav",
        230,
        9052,
        false,
        Reagent.BlackPearl,
        Reagent.Nightshade,
        Reagent.SpidersSilk
    );

    public PoisonFieldSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Fifth;

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

            var itemID = eastToWest ? 0x3915 : 0x3922;
            var duration = Core.Expansion switch
            {
                Expansion.None  => TimeSpan.FromSeconds(20),
                < Expansion.LBR => TimeSpan.FromSeconds(15 + Caster.Skills.Magery.Value * 0.4),
                _               => TimeSpan.FromSeconds(3 + Caster.Skills.Magery.Value * 0.4)
            };

            for (var i = -2; i <= 2; ++i)
            {
                var targetLoc = new Point3D(eastToWest ? loc.X + i : loc.X, eastToWest ? loc.Y : loc.Y + i, loc.Z);

                new PoisonField(itemID, targetLoc, Caster, Caster.Map, duration, i);
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
public partial class PoisonField : Item
{
    [SerializableField(0)]
    private Mobile _caster;

    [DeltaDateTime]
    [SerializableField(1)]
    private DateTime _end;

    private Timer _timer;

    public PoisonField(int itemID, Point3D loc, Mobile caster, Map map, TimeSpan duration, int val) : base(itemID)
    {
        var canFit = SpellHelper.AdjustField(ref loc, map, 12, false);

        Visible = false;
        Movable = false;
        Light = LightType.Circle300;

        MoveToWorld(loc, map);

        _caster = caster;

        _end = Core.Now + duration;

        _timer = new InternalTimer(this, TimeSpan.FromSeconds(val.Abs() * 0.2), caster.InLOS(this), canFit);
        _timer.Start();
    }

    public override bool BlocksFit => true;

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _timer?.Stop();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        _timer = new InternalTimer(this, TimeSpan.Zero, true, true);
        _timer.Start();
    }

    public void ApplyPoisonTo(Mobile m)
    {
        if (_caster == null)
        {
            return;
        }

        int level;

        if (Core.AOS)
        {
            level = (_caster.Skills.Magery.Value + _caster.Skills.Poisoning.Value) switch
            {
                > 199.8 => 3,
                > 170.2 => 2,
                > 130.2 => 1,
                _       => 0
            };
        }
        else
        {
            level = 1;
        }

        if (m.ApplyPoison(_caster, Poison.GetPoison(level)) is ApplyPoisonResult.Poisoned or ApplyPoisonResult.HigherPoisonActive)
        {
            if (SpellHelper.CanRevealCaster(m))
            {
                _caster.RevealingAction();
            }
        }

        (m as BaseCreature)?.OnHarmfulSpell(_caster);
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (Visible && _caster != null && (!Core.AOS || m != _caster) &&
            SpellHelper.ValidIndirectTarget(_caster, m) && _caster.CanBeHarmful(m, false))
        {
            _caster.DoHarmful(m);

            ApplyPoisonTo(m);
            m.PlaySound(0x474);
        }

        return true;
    }

    private class InternalTimer : Timer
    {
        private readonly bool m_CanFit;
        private readonly bool m_InLOS;
        private readonly PoisonField m_Item;

        public InternalTimer(PoisonField item, TimeSpan delay, bool inLOS, bool canFit) : base(
            delay,
            TimeSpan.FromSeconds(1.5)
        )
        {
            m_Item = item;
            m_InLOS = inLOS;
            m_CanFit = canFit;
        }

        protected override void OnTick()
        {
            if (m_Item.Deleted)
            {
                return;
            }

            if (!m_Item.Visible)
            {
                if (m_InLOS && m_CanFit)
                {
                    m_Item.Visible = true;
                }
                else
                {
                    m_Item.Delete();
                }

                if (!m_Item.Deleted)
                {
                    m_Item.ProcessDelta();
                    Effects.SendLocationParticles(
                        EffectItem.Create(m_Item.Location, m_Item.Map, EffectItem.DefaultDuration),
                        0x376A,
                        9,
                        10,
                        5040
                    );
                }
            }
            else if (Core.Now > m_Item._end)
            {
                m_Item.Delete();
                Stop();
            }
            else
            {
                var map = m_Item.Map;
                var caster = m_Item._caster;

                if (map != null && caster != null)
                {
                    var eastToWest = m_Item.ItemID == 0x3915;
                    var bounds = new Rectangle2D(
                        m_Item.X - (eastToWest ? 0 : 1),
                        m_Item.Y - (eastToWest ? 1 : 0),
                        eastToWest ? 1 : 2,
                        eastToWest ? 2 : 1
                    );

                    using var queue = PooledRefQueue<Mobile>.Create();
                    foreach (var m in map.GetMobilesInBounds(bounds))
                    {
                        if (m.Z + 16 > m_Item.Z && m_Item.Z + 12 > m.Z && (!Core.AOS || m != caster) &&
                            SpellHelper.ValidIndirectTarget(caster, m) && caster.CanBeHarmful(m, false))
                        {
                            queue.Enqueue(m);
                        }
                    }

                    while (queue.Count > 0)
                    {
                        var m = queue.Dequeue();

                        caster.DoHarmful(m);

                        m_Item.ApplyPoisonTo(m);
                        m.PlaySound(0x474);
                    }
                }
            }
        }
    }
}
