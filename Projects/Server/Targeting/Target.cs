using System;
using Server.Logging;
using Server.Network;

namespace Server.Targeting;

public abstract class Target
{
    protected static readonly ILogger logger = LogFactory.GetLogger(typeof(Target));

    public Type TargetType { get; set; }

    private static int m_NextTargetID;

    public Timer m_TimeoutTimer;

    protected Target(int range, bool allowGround, TargetFlags flags)
    {
        TargetID = ++m_NextTargetID;
        Range = range;
        AllowGround = allowGround;
        Flags = flags;

        CheckLOS = true;
    }

    public long TimeoutTime { get; private set; }

    //property of every target if LOS of target is needed to be checked or not.
    public bool CheckLOS { get; set; }

    public bool DisallowMultis { get; set; }

    public bool AllowNonlocal { get; set; }

    public int TargetID { get; }

    public int Range { get; set; }

    public bool AllowGround { get; set; }

    public TargetFlags Flags { get; set; }

    public static void Cancel(Mobile m)
    {
        m.NetState.SendCancelTarget();
        m.Target?.Cancel(m, TargetCancelType.Canceled);
    }

    public void BeginTimeout(Mobile from, long delay)
    {
        TimeoutTime = Core.TickCount + delay;

        m_TimeoutTimer?.Stop();

        m_TimeoutTimer = new TimeoutTimer(this, from, TimeSpan.FromMilliseconds(delay));
        m_TimeoutTimer.Start();
    }

    public virtual void CancelTimeout()
    {
        m_TimeoutTimer?.Stop();
        m_TimeoutTimer = null;
    }

    public virtual void CancelSpellTargetTimeout()
    {

    }

    public void Timeout(Mobile m)
    {
        m.NetState.SendCancelTarget();
        CancelTimeout();
        m.ClearTarget();

        OnTargetCancel(m, TargetCancelType.Timeout);
        OnTargetFinish(m);
    }

    public virtual void SendTargetTo(NetState ns) => ns.SendTargetReq(this);

    public void Cancel(Mobile from, TargetCancelType type)
    {
        CancelTimeout();
        from.ClearTarget();

        OnTargetCancel(from, type);
        OnTargetFinish(from);
    }

    protected virtual bool CanTarget(Mobile from, LandTarget landTarget, ref Point3D loc, ref Map map)
    {
        if (!AllowGround)
        {
            // We should actually never get here. If we do, it's probably a misbehaving client/macro.
            return false;
        }

        loc = landTarget.Location;
        map = from.Map;
        return true;
    }

    protected virtual bool CanTarget(Mobile from, StaticTarget staticTarget, ref Point3D loc, ref Map map)
    {
        loc = staticTarget.Location;
        map = from.Map;
        return true;
    }

    protected virtual bool CanTarget(Mobile from, Item item, ref Point3D loc, ref Map map)
    {
        if (item.Deleted)
        {
            OnTargetDeleted(from, item);
            return false;
        }

        if (!item.CanTarget)
        {
            OnTargetUntargetable(from, item);
            return false;
        }

        if (!AllowNonlocal && item.RootParent is Mobile && item.RootParent != from &&
            from.AccessLevel == AccessLevel.Player)
        {
            OnNonlocalTarget(from, item);
            return false;
        }

        loc = item.GetWorldLocation();
        map = item.Map;
        return true;
    }

    protected virtual bool CanTarget(Mobile from, Mobile mobile, ref Point3D loc, ref Map map)
    {
        if (mobile.Deleted)
        {
            OnTargetDeleted(from, mobile);
            return false;
        }

        if (!mobile.CanTarget)
        {
            OnTargetUntargetable(from, mobile);
            return false;
        }

        loc = mobile.Location;
        map = mobile.Map;
        return true;
    }

    public void Invoke(Mobile from, object targeted)
    {
        CancelTimeout();
        from.ClearTarget();

        if (from.Deleted)
        {
            OnTargetCancel(from, TargetCancelType.Canceled);
            OnTargetFinish(from);
            return;
        }

        Point3D loc = default;
        Map map = null;
        Item item = null;
        Mobile mobile = null;
        var isValidTargetType = true;

        bool valid = targeted switch
        {
            LandTarget landTarget     => CanTarget(from, landTarget, ref loc, ref map),
            StaticTarget staticTarget => CanTarget(from, staticTarget, ref loc, ref map),
            Item i                    => CanTarget(from, item = i, ref loc, ref map),
            Mobile m                  => CanTarget(from, mobile = m, ref loc, ref map),
            _                         => isValidTargetType = false
        };

        if (!valid)
        {
            if (!isValidTargetType)
            {
                OnTargetUntargetable(from, targeted);
            }
        }
        else if (map == null || map != from.Map || Range >= 0 && !from.InRange(loc, Range))
        {
            OnTargetOutOfRange(from, targeted);
        }
        else if (!from.CanSee(targeted))
        {
            OnCantSeeTarget(from, targeted);
        }
        else if (CheckLOS && !from.InLOS(targeted))
        {
            OnTargetOutOfLOS(from, targeted);
        }
        else if (item?.InSecureTrade == true)
        {
            OnTargetInSecureTrade(from, targeted);
        }
        else if (item?.IsAccessibleTo(from) == false)
        {
            OnTargetNotAccessible(from, targeted);
        }
        else if (item?.CheckTarget(from, this, targeted) == false)
        {
            OnTargetUntargetable(from, targeted);
        }
        else if (mobile?.CheckTarget(from, this, targeted) == false)
        {
            OnTargetUntargetable(from, mobile);
        }
        else if (from.Region.OnTarget(from, this, targeted))
        {
            OnTarget(from, targeted);
        }

        OnTargetFinish(from);
    }

    protected virtual void OnTarget(Mobile from, object targeted)
    {
    }

    public virtual void ApplySpellOnTarget()
    {

    }
    protected virtual void OnTargetNotAccessible(Mobile from, object targeted)
    {
        from.SendLocalizedMessage(500447); // That is not accessible.
    }

    protected virtual void OnTargetInSecureTrade(Mobile from, object targeted)
    {
        from.SendLocalizedMessage(500447); // That is not accessible.
    }

    protected virtual void OnNonlocalTarget(Mobile from, object targeted)
    {
        from.SendLocalizedMessage(500447); // That is not accessible.
    }

    protected virtual void OnCantSeeTarget(Mobile from, object targeted)
    {
        from.SendLocalizedMessage(500237); // Target can not be seen.
    }

    protected virtual void OnTargetOutOfLOS(Mobile from, object targeted)
    {
        from.SendLocalizedMessage(500237); // Target can not be seen.
    }

    protected virtual void OnTargetOutOfRange(Mobile from, object targeted)
    {
        from.SendLocalizedMessage(500446); // That is too far away.
    }

    protected virtual void OnTargetDeleted(Mobile from, object targeted)
    {
    }

    protected virtual void OnTargetUntargetable(Mobile from, object targeted)
    {
        from.SendLocalizedMessage(500447); // That is not accessible.
    }

    protected virtual void OnTargetCancel(Mobile from, TargetCancelType cancelType)
    {
    }

    protected virtual void OnTargetFinish(Mobile from)
    {
    }

    private class TimeoutTimer : Timer
    {
        private readonly Mobile _mobile;
        private readonly Target _target;

        public TimeoutTimer(Target target, Mobile m, TimeSpan delay) : base(delay)
        {
            _target = target;
            _mobile = m;
        }

        protected override void OnTick()
        {
            if (_mobile.Target == _target)
            {
                _target.Timeout(_mobile);
            }
        }
    }
}
