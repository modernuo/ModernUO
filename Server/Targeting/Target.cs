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

		private static bool m_TargetIDValidation = true;

		public static bool TargetIDValidation
		{
			get => m_TargetIDValidation;
			set => m_TargetIDValidation = value;
		}

		private int m_TargetID;
		private int m_Range;
		private bool m_AllowGround;
		private bool m_CheckLOS;
		private bool m_AllowNonlocal;
		private bool m_DisallowMultis;
		private TargetFlags m_Flags;
		private DateTime m_TimeoutTime;

		public DateTime TimeoutTime => m_TimeoutTime;

		protected Target( int range, bool allowGround, TargetFlags flags )
		{
			m_TargetID = ++m_NextTargetID;
			m_Range = range;
			m_AllowGround = allowGround;
			m_Flags = flags;

			m_CheckLOS = true;
		}

		public static void Cancel( Mobile m )
		{
			NetState ns = m.NetState;

			ns?.Send( CancelTarget.Instance );

			Target targ = m.Target;

			targ?.OnTargetCancel( m, TargetCancelType.Canceled );
		}

		private Timer m_TimeoutTimer;

		public void BeginTimeout( Mobile from, TimeSpan delay )
		{
			m_TimeoutTime = DateTime.UtcNow + delay;

			m_TimeoutTimer?.Stop();

			m_TimeoutTimer = new TimeoutTimer( this, from, delay );
			m_TimeoutTimer.Start();
		}

		public void CancelTimeout()
		{
			m_TimeoutTimer?.Stop();

			m_TimeoutTimer = null;
		}

		public void Timeout( Mobile from )
		{
			CancelTimeout();
			from.ClearTarget();

			Cancel( from );

			OnTargetCancel( from, TargetCancelType.Timeout );
			OnTargetFinish( from );
		}

		private class TimeoutTimer : Timer
		{
			private Target m_Target;
			private Mobile m_Mobile;

			private static TimeSpan ThirtySeconds = TimeSpan.FromSeconds( 30.0 );
			private static TimeSpan TenSeconds = TimeSpan.FromSeconds( 10.0 );
			private static TimeSpan OneSecond = TimeSpan.FromSeconds( 1.0 );

			public TimeoutTimer( Target target, Mobile m, TimeSpan delay ) : base( delay )
			{
				m_Target = target;
				m_Mobile = m;

				if ( delay >= ThirtySeconds )
					Priority = TimerPriority.FiveSeconds;
				else if ( delay >= TenSeconds )
					Priority = TimerPriority.OneSecond;
				else if ( delay >= OneSecond )
					Priority = TimerPriority.TwoFiftyMS;
				else
					Priority = TimerPriority.TwentyFiveMS;
			}

			protected override void OnTick()
			{
				if ( m_Mobile.Target == m_Target )
					m_Target.Timeout( m_Mobile );
			}
		}

		public bool CheckLOS
		{
			get => m_CheckLOS;
			set => m_CheckLOS = value;
		}

		public bool DisallowMultis
		{
			get => m_DisallowMultis;
			set => m_DisallowMultis = value;
		}

		public bool AllowNonlocal
		{
			get => m_AllowNonlocal;
			set => m_AllowNonlocal = value;
		}

		public int TargetID => m_TargetID;

		public virtual Packet GetPacketFor( NetState ns )
		{
			return new TargetReq( this );
		}

		public void Cancel( Mobile from, TargetCancelType type )
		{
			CancelTimeout();
			from.ClearTarget();

			OnTargetCancel( from, type );
			OnTargetFinish( from );
		}

		public void Invoke( Mobile from, object targeted )
		{
			CancelTimeout();
			from.ClearTarget();

			if ( from.Deleted )
			{
				OnTargetCancel( from, TargetCancelType.Canceled );
				OnTargetFinish( from );
				return;
			}

			Point3D loc;
			Map map;

			Item item = targeted as Item;
			Mobile mobile = targeted as Mobile;

			if ( targeted is LandTarget target )
			{
				loc = target.Location;
				map = from.Map;
			}
			else if ( targeted is StaticTarget staticTarget )
			{
				loc = staticTarget.Location;
				map = from.Map;
			}
			else if ( mobile != null )
			{
				if ( mobile.Deleted )
				{
					OnTargetDeleted( from, mobile );
					OnTargetFinish( from );
					return;
				}

				if ( !mobile.CanTarget )
				{
					OnTargetUntargetable( from, mobile );
					OnTargetFinish( from );
					return;
				}

				loc = mobile.Location;
				map = mobile.Map;
			}
			else if ( item != null )
			{
				if ( item.Deleted )
				{
					OnTargetDeleted( from, item );
					OnTargetFinish( from );
					return;
				}

				if ( !item.CanTarget )
				{
					OnTargetUntargetable( from, item );
					OnTargetFinish( from );
					return;
				}

				object root = item.RootParent;

				if ( !m_AllowNonlocal && root is Mobile && root != from && from.AccessLevel == AccessLevel.Player )
				{
					OnNonlocalTarget( from, item );
					OnTargetFinish( from );
					return;
				}

				loc = item.GetWorldLocation();
				map = item.Map;
			}
			else
			{
				OnTargetCancel( from, TargetCancelType.Canceled );
				OnTargetFinish( from );
				return;
			}

			if ( map == null || map != from.Map || ( m_Range != -1 && !from.InRange( loc, m_Range ) ) )
			{
				OnTargetOutOfRange( from, targeted );
			}
			else
			{
				if ( !from.CanSee( targeted ) )
					OnCantSeeTarget( from, targeted );
				else if ( m_CheckLOS && !from.InLOS( targeted ) )
					OnTargetOutOfLOS( from, targeted );
				else if ( item?.InSecureTrade == true )
					OnTargetInSecureTrade( from, targeted );
				else if ( item?.IsAccessibleTo( from ) == true )
					OnTargetNotAccessible( from, targeted );
				else if ( item?.CheckTarget( from, this, targeted ) == true )
					OnTargetUntargetable( from, targeted );
				else if ( mobile?.CheckTarget( from, this, mobile ) != true )
					OnTargetUntargetable( from, mobile );
				else if ( from.Region.OnTarget( from, this, targeted ) )
					OnTarget( from, targeted );
			}

			OnTargetFinish( from );
		}

		protected virtual void OnTarget( Mobile from, object targeted )
		{
		}

		protected virtual void OnTargetNotAccessible( Mobile from, object targeted )
		{
			from.SendLocalizedMessage( 500447 ); // That is not accessible.
		}

		protected virtual void OnTargetInSecureTrade( Mobile from, object targeted )
		{
			from.SendLocalizedMessage( 500447 ); // That is not accessible.
		}

		protected virtual void OnNonlocalTarget( Mobile from, object targeted )
		{
			from.SendLocalizedMessage( 500447 ); // That is not accessible.
		}

		protected virtual void OnCantSeeTarget( Mobile from, object targeted )
		{
			from.SendLocalizedMessage( 500237 ); // Target can not be seen.
		}

		protected virtual void OnTargetOutOfLOS( Mobile from, object targeted )
		{
			from.SendLocalizedMessage( 500237 ); // Target can not be seen.
		}

		protected virtual void OnTargetOutOfRange( Mobile from, object targeted )
		{
			from.SendLocalizedMessage( 500446 ); // That is too far away.
		}

		protected virtual void OnTargetDeleted( Mobile from, object targeted )
		{
		}

		protected virtual void OnTargetUntargetable( Mobile from, object targeted )
		{
			from.SendLocalizedMessage( 500447 ); // That is not accessible.
		}

		protected virtual void OnTargetCancel( Mobile from, TargetCancelType cancelType )
		{
		}

		protected virtual void OnTargetFinish( Mobile from )
		{
		}

		public int Range
		{
			get => m_Range;
			set => m_Range = value;
		}

		public bool AllowGround
		{
			get => m_AllowGround;
			set => m_AllowGround = value;
		}

		public TargetFlags Flags
		{
			get => m_Flags;
			set => m_Flags = value;
		}
	}
}
