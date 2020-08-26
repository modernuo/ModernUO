/***************************************************************************
 *                                Target.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using Server.Network;

namespace Server.Targeting
{
    public abstract class Target
    {
        private static int m_NextTargetID;

        private Timer m_TimeoutTimer;

        protected Target(int range, bool allowGround, TargetFlags flags)
        {
            TargetID = ++m_NextTargetID;
            Range = range;
            AllowGround = allowGround;
            Flags = flags;

            CheckLOS = true;
        }

        public DateTime TimeoutTime { get; private set; }

        public bool CheckLOS { get; set; }

        public bool DisallowMultis { get; set; }

        public bool AllowNonlocal { get; set; }

        public int TargetID { get; }

        public int Range { get; set; }

        public bool AllowGround { get; set; }

        public TargetFlags Flags { get; set; }

        public static void Cancel(Mobile m)
        {
            m.NetState?.Send(CancelTarget.Instance);
            m.Target?.OnTargetCancel(m, TargetCancelType.Canceled);
        }

        public void BeginTimeout(Mobile from, TimeSpan delay)
        {
            TimeoutTime = DateTime.UtcNow + delay;

            m_TimeoutTimer?.Stop();

            m_TimeoutTimer = new TimeoutTimer(this, from, delay);
            m_TimeoutTimer.Start();
        }

        public void CancelTimeout()
        {
            m_TimeoutTimer?.Stop();
            m_TimeoutTimer = null;
        }

        public void Timeout(Mobile from)
        {
            CancelTimeout();
            from.ClearTarget();

            Cancel(from);

            OnTargetCancel(from, TargetCancelType.Timeout);
            OnTargetFinish(from);
        }

        public virtual Packet GetPacketFor(NetState ns) => new TargetReq(this);

        public void Cancel(Mobile from, TargetCancelType type)
        {
            CancelTimeout();
            from.ClearTarget();

            OnTargetCancel(from, type);
            OnTargetFinish(from);
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

            Point3D loc;
            Map map;

            var item = targeted as Item;
            var mobile = targeted as Mobile;

            if (targeted is LandTarget target)
            {
                loc = target.Location;
                map = from.Map;
            }
            else if (targeted is StaticTarget staticTarget)
            {
                loc = staticTarget.Location;
                map = from.Map;
            }
            else if (mobile != null)
            {
                if (mobile.Deleted)
                {
                    OnTargetDeleted(from, mobile);
                    OnTargetFinish(from);
                    return;
                }

                if (!mobile.CanTarget)
                {
                    OnTargetUntargetable(from, mobile);
                    OnTargetFinish(from);
                    return;
                }

                loc = mobile.Location;
                map = mobile.Map;
            }
            else if (item != null)
            {
                if (item.Deleted)
                {
                    OnTargetDeleted(from, item);
                    OnTargetFinish(from);
                    return;
                }

                if (!item.CanTarget)
                {
                    OnTargetUntargetable(from, item);
                    OnTargetFinish(from);
                    return;
                }

                if (!AllowNonlocal && item.RootParent is Mobile && item.RootParent != from &&
                    from.AccessLevel == AccessLevel.Player)
                {
                    OnNonlocalTarget(from, item);
                    OnTargetFinish(from);
                    return;
                }

                loc = item.GetWorldLocation();
                map = item.Map;
            }
            else
            {
                OnTargetCancel(from, TargetCancelType.Canceled);
                OnTargetFinish(from);
                return;
            }

            if (map == null || map != from.Map || Range != -1 && !from.InRange(loc, Range))
            {
                OnTargetOutOfRange(from, targeted);
            }
            else
            {
                if (!from.CanSee(targeted))
                    OnCantSeeTarget(from, targeted);
                else if (CheckLOS && !from.InLOS(targeted))
                    OnTargetOutOfLOS(from, targeted);
                else if (item?.InSecureTrade == true)
                    OnTargetInSecureTrade(from, targeted);
                else if (item?.IsAccessibleTo(from) == false)
                    OnTargetNotAccessible(from, targeted);
                else if (item?.CheckTarget(from, this, targeted) == false)
                    OnTargetUntargetable(from, targeted);
                else if (mobile?.CheckTarget(from, this, mobile) == false)
                    OnTargetUntargetable(from, mobile);
                else if (from.Region.OnTarget(from, this, targeted))
                    OnTarget(from, targeted);
            }

            OnTargetFinish(from);
        }

        protected virtual void OnTarget(Mobile from, object targeted)
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
            private static readonly TimeSpan ThirtySeconds = TimeSpan.FromSeconds(30.0);
            private static readonly TimeSpan TenSeconds = TimeSpan.FromSeconds(10.0);
            private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1.0);
            private readonly Mobile m_Mobile;
            private readonly Target m_Target;

            public TimeoutTimer(Target target, Mobile m, TimeSpan delay) : base(delay)
            {
                m_Target = target;
                m_Mobile = m;

                if (delay >= ThirtySeconds)
                    Priority = TimerPriority.FiveSeconds;
                else if (delay >= TenSeconds)
                    Priority = TimerPriority.OneSecond;
                else if (delay >= OneSecond)
                    Priority = TimerPriority.TwoFiftyMS;
                else
                    Priority = TimerPriority.TwentyFiveMS;
            }

            protected override void OnTick()
            {
                if (m_Mobile.Target == m_Target)
                    m_Target.Timeout(m_Mobile);
            }
        }
    }
}
