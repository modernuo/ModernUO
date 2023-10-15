using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.Spells;
using Server.Spells.Fourth;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Firebomb : Item
{
    private Mobile m_LitBy;
    private Point3D _thrownFromLocation;
    private int _ticks;
    private TimerExecutionToken _timerToken;
    private List<Mobile> _users;

    [Constructible]
    public Firebomb(int itemID = 0x99B) : base(itemID)
    {
        // Name = "a firebomb";
        Weight = 2.0;
        Hue = 1260;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        if (Core.AOS && (from.Paralyzed || from.Frozen || from.Spell?.IsCasting == true))
        {
            // to prevent exploiting for pvp
            from.SendLocalizedMessage(1075857); // You cannot use that while paralyzed.
            return;
        }

        if (_timerToken.Running)
        {
            from.SendLocalizedMessage(1060581); // You've already lit it!  Better throw it now!
        }
        else
        {
            Timer.StartTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), OnFirebombTimerTick, out _timerToken);
            m_LitBy = from;
            from.SendLocalizedMessage(1060582); // You light the firebomb.  Throw it now!
        }

        _users ??= new List<Mobile>();

        if (!_users.Contains(from))
        {
            _users.Add(from);
        }

        from.Target = new ThrowTarget(this);
    }

    private void OnFirebombTimerTick()
    {
        if (Deleted)
        {
            _timerToken.Cancel();
            return;
        }

        if (Map == Map.Internal && HeldBy == null)
        {
            return;
        }

        switch (_ticks)
        {
            case 0:
            case 1:
            case 2:
                {
                    ++_ticks;

                    if (HeldBy != null)
                    {
                        HeldBy.PublicOverheadMessage(MessageType.Regular, 957, false, _ticks.ToString());
                    }
                    else if (RootParent == null)
                    {
                        PublicOverheadMessage(MessageType.Regular, 957, false, _ticks.ToString());
                    }
                    else if (RootParent is Mobile mobile)
                    {
                        mobile.PublicOverheadMessage(MessageType.Regular, 957, false, _ticks.ToString());
                    }

                    break;
                }
            default:
                {
                    HeldBy?.DropHolding();

                    if (_users != null)
                    {
                        foreach (var m in _users)
                        {
                            if (m.Target is ThrowTarget targ && targ.Bomb == this)
                            {
                                Target.Cancel(m);
                            }
                        }

                        _users.Clear();
                        _users = null;
                    }

                    if (RootParent is Mobile parent)
                    {
                        parent.SendLocalizedMessage(1060583); // The firebomb explodes in your hand!
                        AOS.Damage(parent, Utility.Random(3) + 4, 0, 100, 0, 0, 0);
                    }
                    else if (RootParent == null)
                    {
                        var eable = Map.GetMobilesInRange(Location, 1);
                        using var targets = PooledRefQueue<Mobile>.Create();
                        foreach (var m in eable)
                        {
                            if (m_LitBy == null || SpellHelper.ValidIndirectTarget(m_LitBy, m) &&
                                m_LitBy.CanBeHarmful(m, false))
                            {
                                targets.Enqueue(m);
                            }
                        }

                        while (targets.Count > 0)
                        {
                            var victim = targets.Dequeue();
                            m_LitBy?.DoHarmful(victim);
                            AOS.Damage(victim, m_LitBy, Utility.Random(3) + 4, 0, 100, 0, 0, 0);
                        }

                        var loc = _thrownFromLocation;
                        var eastToWest = SpellHelper.GetEastToWest(loc, Location);
                        Effects.PlaySound(loc, Map, 0x20C);
                        var itemID = eastToWest ? 0x398C : 0x3996;

                        for (var i = -2; i <= 2; ++i)
                        {
                            var targetLoc = new Point3D(eastToWest ? loc.X + i : loc.X, eastToWest ? loc.Y : loc.Y + i, loc.Z);
                            new FireFieldSpell.FireFieldItem(itemID, targetLoc, m_LitBy, Map, TimeSpan.FromSeconds(9), i);
                        }
                    }

                    _timerToken.Cancel();
                    Delete();
                    break;
                }
        }
    }

    private void OnFirebombTarget(Mobile from, object obj)
    {
        if (Deleted || Map == Map.Internal || !IsChildOf(from.Backpack))
        {
            return;
        }

        if (obj is not IPoint3D p)
        {
            return;
        }

        SpellHelper.GetSurfaceTop(ref p);
        _thrownFromLocation = new Point3D(p);
        var map = Map;

        from.RevealingAction();

        var to = p as IEntity ?? new Entity(Serial.Zero, _thrownFromLocation, map);

        Effects.SendMovingEffect(from, to, ItemID, 7, 0, false, false, Hue);

        Timer.StartTimer(TimeSpan.FromSeconds(1.0),
            () =>
            {
                if (Deleted)
                {
                    return;
                }

                MoveToWorld(_thrownFromLocation, map);
            }
        );
        Internalize();
    }

    private class ThrowTarget : Target
    {
        public ThrowTarget(Firebomb bomb) : base(12, true, TargetFlags.None) => Bomb = bomb;

        public Firebomb Bomb { get; }

        protected override void OnTarget(Mobile from, object targeted) => Bomb.OnFirebombTarget(from, targeted);
    }
}
