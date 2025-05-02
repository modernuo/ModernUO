using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.Items;
using Server.Misc;
using Server.Mobiles;

namespace Server.Spells.Fourth;

public class FireFieldSpell : MagerySpell, ITargetingSpell<IPoint3D>
{
    private static readonly SpellInfo _info = new(
        "Fire Field",
        "In Flam Grav",
        215,
        9041,
        false,
        Reagent.BlackPearl,
        Reagent.SpidersSilk,
        Reagent.SulfurousAsh
    );

    public FireFieldSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Fourth;

    public int TargetRange => Core.T2A ? 15 : 18;

    public void Target(IPoint3D p)
    {
        if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
        {
            SpellHelper.Turn(Caster, p);

            SpellHelper.GetSurfaceTop(ref p);

            var loc = new Point3D(p);

            var eastToWest = SpellHelper.GetEastToWest(Caster.Location, loc);

            Effects.PlaySound(loc, Caster.Map, 0x20C);

            var itemID = eastToWest ? 0x398C : 0x3996;

            var duration = Core.Expansion switch
            {
                Expansion.None  => TimeSpan.FromSeconds(20),
                < Expansion.LBR => TimeSpan.FromSeconds(Caster.Skills.Magery.Value),
                _               => TimeSpan.FromSeconds(4.0 + Caster.Skills.Magery.Value * 0.5)
            };

            for (var i = -2; i <= 2; ++i)
            {
                var targetLoc = new Point3D(eastToWest ? loc.X + i : loc.X, eastToWest ? loc.Y : loc.Y + i, loc.Z);

                new FireFieldItem(itemID, targetLoc, Caster, Caster.Map, duration, i);
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
public partial class FireFieldItem : Item
{
    [SerializableField(0)]
    private int _damage;

    [SerializableField(1)]
    private Mobile _caster;

    [DeltaDateTime]
    [SerializableField(2)]
    private DateTime _end;
    private Timer _timer;

    public FireFieldItem(
        int itemID, Point3D loc, Mobile caster, Map map, TimeSpan duration, int val,
        int damage = 2
    ) : base(itemID)
    {
        var canFit = SpellHelper.AdjustField(ref loc, map, 12, false);

        Visible = false;
        Movable = false;
        Light = LightType.Circle300;

        MoveToWorld(loc, map);

        _caster = caster;

        _damage = damage;

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

            var damage = _damage;

            if (!Core.AOS && m.CheckSkill(SkillName.MagicResist, 0.0, 30.0))
            {
                damage = 1;

                m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
            }

            AOS.Damage(m, _caster, damage, 0, 100, 0, 0, 0);
            m.PlaySound(0x208);

            (m as BaseCreature)?.OnHarmfulSpell(_caster);
        }

        return true;
    }

    private class InternalTimer : Timer
    {
        private readonly bool _canFit;
        private readonly bool _inLOS;
        private readonly FireFieldItem _item;

        public InternalTimer(FireFieldItem item, TimeSpan delay, bool inLOS, bool canFit) : base(delay, TimeSpan.FromSeconds(1.0))
        {
            _item = item;
            _inLOS = inLOS;
            _canFit = canFit;
        }

        protected override void OnTick()
        {
            if (_item.Deleted)
            {
                return;
            }

            if (!_item.Visible)
            {
                if (_inLOS && _canFit)
                {
                    _item.Visible = true;
                }
                else
                {
                    _item.Delete();
                }

                if (!_item.Deleted)
                {
                    _item.ProcessDelta();
                    Effects.SendLocationParticles(
                        EffectItem.Create(_item.Location, _item.Map, EffectItem.DefaultDuration),
                        0x376A,
                        9,
                        10,
                        5029
                    );
                }
            }
            else if (Core.Now > _item._end)
            {
                _item.Delete();
                Stop();
            }
            else
            {
                var map = _item.Map;
                var caster = _item._caster;

                if (map == null || caster == null)
                {
                    return;
                }

                using var queue = PooledRefQueue<Mobile>.Create();
                foreach (var m in _item.GetMobilesAt())
                {
                    if (m.Z + 16 > _item.Z && _item.Z + 12 > m.Z && (!Core.AOS || m != caster) &&
                        SpellHelper.ValidIndirectTarget(caster, m) && caster.CanBeHarmful(m, false))
                    {
                        queue.Enqueue(m);
                    }
                }

                while (queue.Count > 0)
                {
                    var m = queue.Dequeue();

                    if (SpellHelper.CanRevealCaster(m))
                    {
                        caster.RevealingAction();
                    }

                    caster.DoHarmful(m);

                    var damage = _item._damage;

                    if (!Core.AOS && m.CheckSkill(SkillName.MagicResist, 0.0, 30.0))
                    {
                        damage = 1;

                        m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
                    }

                    AOS.Damage(m, caster, damage, 0, 100, 0, 0, 0);
                    m.PlaySound(0x208);

                    (m as BaseCreature)?.OnHarmfulSpell(caster);
                }
            }
        }
    }
}
