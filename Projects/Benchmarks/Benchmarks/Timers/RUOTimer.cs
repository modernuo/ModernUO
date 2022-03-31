/***************************************************************************
 *                                 Timer.cs
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
using System.Collections.Generic;
using System.Threading;
using Server.Diagnostics;

namespace Server
{
    public enum TimerPriority
    {
        EveryTick,
        TenMS,
        TwentyFiveMS,
        FiftyMS,
        TwoFiftyMS,
        OneSecond,
        FiveSeconds,
        OneMinute
    }

    public class RUOTimer
    {
        private long m_Next;
        private long m_Delay;
        private long m_Interval;
        private bool m_Running;
        private int m_Index, m_Count;
        private TimerPriority m_Priority;
        private List<RUOTimer> m_List;
        private bool m_PrioritySet;

        private static string FormatDelegate( Delegate callback )
        {
            if ( callback == null )
            {
                return "null";
            }

            return String.Format( "{0}.{1}", callback.Method.DeclaringType.FullName, callback.Method.Name );
        }

        public TimerPriority Priority
        {
            get
            {
                return m_Priority;
            }
            set
            {
                if ( !m_PrioritySet )
                {
                    m_PrioritySet = true;
                }

                if ( m_Priority != value )
                {
                    m_Priority = value;

                    if ( m_Running )
                    {
                        TimerThread.PriorityChange( this, (int)m_Priority );
                    }
                }
            }
        }

        public DateTime Next
        {
            // Obnoxious
            get { return DateTime.UtcNow + TimeSpan.FromMilliseconds(m_Next-TimerThread.m_TickCount); }
        }

        public TimeSpan Delay
        {
            get { return TimeSpan.FromMilliseconds(m_Delay); }
            set { m_Delay = (long)value.TotalMilliseconds; }
        }

        public TimeSpan Interval
        {
            get { return TimeSpan.FromMilliseconds(m_Interval); }
            set { m_Interval = (long)value.TotalMilliseconds; }
        }

        public bool Running
        {
            get { return m_Running; }
            set {
                if ( value ) {
                    Start();
                } else {
                    Stop();
                }
            }
        }

        public TimerProfile GetProfile()
        {
            if ( !Core.Profiling ) {
                return null;
            }

            string name = ToString();

            if ( name == null ) {
                name = "null";
            }

            return TimerProfile.Acquire( name );
        }

        public class TimerThread
        {
            public static long m_TickCount; // Mimics core tick count for testing

            private static Dictionary<RUOTimer,TimerChangeEntry> m_Changed = new Dictionary<RUOTimer,TimerChangeEntry>();

            private static long[] m_NextPriorities = new long[8];
            private static long[] m_PriorityDelays = new long[8]
            {
                0,
                10,
                25,
                50,
                250,
                1000,
                5000,
                60000
            };

            private static List<RUOTimer>[] m_Timers = new List<RUOTimer>[8]
            {
                new List<RUOTimer>(),
                new List<RUOTimer>(),
                new List<RUOTimer>(),
                new List<RUOTimer>(),
                new List<RUOTimer>(),
                new List<RUOTimer>(),
                new List<RUOTimer>(),
                new List<RUOTimer>(),
            };

            private class TimerChangeEntry
            {
                public RUOTimer MRuoTimer;
                public int m_NewIndex;
                public bool m_IsAdd;

                private TimerChangeEntry( RUOTimer t, int newIndex, bool isAdd )
                {
                    MRuoTimer = t;
                    m_NewIndex = newIndex;
                    m_IsAdd = isAdd;
                }

                public void Free()
                {
                    lock (m_InstancePool) {
                        if (m_InstancePool.Count < 200) // Arbitrary
                        {
                            m_InstancePool.Enqueue( this );
                        }
                    }
                }

                private static Queue<TimerChangeEntry> m_InstancePool = new Queue<TimerChangeEntry>();

                public static TimerChangeEntry GetInstance( RUOTimer t, int newIndex, bool isAdd )
                {
                    TimerChangeEntry e = null;

                    lock (m_InstancePool) {
                        if ( m_InstancePool.Count > 0 ) {
                            e = m_InstancePool.Dequeue();
                        }
                    }

                    if (e != null) {
                        e.MRuoTimer = t;
                        e.m_NewIndex = newIndex;
                        e.m_IsAdd = isAdd;
                    } else {
                        e = new TimerChangeEntry( t, newIndex, isAdd );
                    }

                    return e;
                }
            }

            public TimerThread()
            {
            }

            public static void Change( RUOTimer t, int newIndex, bool isAdd )
            {
                lock (m_Changed)
                {
                    m_Changed[t] = TimerChangeEntry.GetInstance(t, newIndex, isAdd);
                }

                m_Signal.Set();
            }

            public static void AddTimer( RUOTimer t )
            {
                Change( t, (int)t.Priority, true );
            }

            public static void PriorityChange( RUOTimer t, int newPrio )
            {
                Change( t, newPrio, false );
            }

            public static void RemoveTimer( RUOTimer t )
            {
                Change( t, -1, false );
            }

            private static void ProcessChanged()
            {
                lock (m_Changed) {
                    long curTicks = m_TickCount;

                    foreach (TimerChangeEntry tce in m_Changed.Values) {
                        RUOTimer ruoTimer = tce.MRuoTimer;
                        int newIndex = tce.m_NewIndex;

                        if (ruoTimer.m_List != null)
                        {
                            ruoTimer.m_List.Remove(ruoTimer);
                        }

                        if (tce.m_IsAdd) {
                            ruoTimer.m_Next = curTicks + ruoTimer.m_Delay;
                            ruoTimer.m_Index = 0;
                        }

                        if (newIndex >= 0) {
                            ruoTimer.m_List = m_Timers[newIndex];
                            ruoTimer.m_List.Add(ruoTimer);
                        } else {
                            ruoTimer.m_List = null;
                        }

                        tce.Free();
                    }

                    m_Changed.Clear();
                }
            }

            public static void CleanupForTesting()
            {
                lock (m_Changed)
                {
                    m_Changed.Clear();
                }
            }

            private static AutoResetEvent m_Signal = new AutoResetEvent( false );
            public static void Set() { m_Signal.Set(); }

            public void TimerMain(CancellationToken cancellationToken)
            {
                long now;
                int i, j;
                bool loaded;

                while ( !cancellationToken.IsCancellationRequested )
                {
                    ProcessChanged();

                    loaded = false;

                    for ( i = 0; i < m_Timers.Length; i++)
                    {
                        now = m_TickCount;
                        if ( now < m_NextPriorities[i] )
                        {
                            break;
                        }

                        m_NextPriorities[i] = now + m_PriorityDelays[i];

                        for ( j = 0; j < m_Timers[i].Count; j++)
                        {
                            RUOTimer t = m_Timers[i][j];

                            if ( !t.m_Queued && now > t.m_Next )
                            {
                                t.m_Queued = true;

                                lock ( m_Queue )
                                {
                                    m_Queue.Enqueue( t );
                                }

                                loaded = true;

                                if ( t.m_Count != 0 && (++t.m_Index >= t.m_Count) )
                                {
                                    t.Stop();
                                }
                                else
                                {
                                    t.m_Next = now + t.m_Interval;
                                }
                            }
                        }
                    }

                    if ( loaded )
                    {
                        // Core.Set();
                    }

                    m_Signal.WaitOne(-1, false);
                }
            }
        }

        private static Queue<RUOTimer> m_Queue = new Queue<RUOTimer>();
        private static int m_BreakCount = 20000;

        public static int BreakCount{ get{ return m_BreakCount; } set{ m_BreakCount = value; } }

        private static int m_QueueCountAtSlice;

        private bool m_Queued;

        public static void Slice()
        {
            lock ( m_Queue )
            {
                m_QueueCountAtSlice = m_Queue.Count;

                int index = 0;

                while ( index < m_BreakCount && m_Queue.Count != 0 )
                {
                    RUOTimer t = m_Queue.Dequeue();
                    TimerProfile prof = t.GetProfile();

                    if ( prof != null ) {
                        prof.Start();
                    }

                    t.OnTick();
                    t.m_Queued = false;
                    ++index;

                    if ( prof != null ) {
                        prof.Finish();
                    }
                }
            }
        }

        public RUOTimer( TimeSpan delay ) : this( delay, TimeSpan.Zero, 1 )
        {
        }

        public RUOTimer( TimeSpan delay, TimeSpan interval ) : this( delay, interval, 0 )
        {
        }

        public virtual bool DefRegCreation
        {
            get{ return true; }
        }

        public void RegCreation()
        {
            TimerProfile prof = GetProfile();

            if ( prof != null ) {
                prof.Created++;
            }
        }

        public RUOTimer( TimeSpan delay, TimeSpan interval, int count )
        {
            m_Delay = (long)delay.TotalMilliseconds;
            m_Interval = (long)interval.TotalMilliseconds;
            m_Count = count;

            if ( !m_PrioritySet ) {
                if ( count == 1 ) {
                    m_Priority = ComputePriority( delay );
                } else {
                    m_Priority = ComputePriority( interval );
                }
                m_PrioritySet = true;
            }

            if ( DefRegCreation )
            {
                RegCreation();
            }
        }

        public override string ToString()
        {
            return GetType().FullName;
        }

        public static TimerPriority ComputePriority( TimeSpan ts )
        {
            if ( ts >= TimeSpan.FromMinutes( 1.0 ) )
            {
                return TimerPriority.FiveSeconds;
            }

            if ( ts >= TimeSpan.FromSeconds( 10.0 ) )
            {
                return TimerPriority.OneSecond;
            }

            if ( ts >= TimeSpan.FromSeconds( 5.0 ) )
            {
                return TimerPriority.TwoFiftyMS;
            }

            if ( ts >= TimeSpan.FromSeconds( 2.5 ) )
            {
                return TimerPriority.FiftyMS;
            }

            if ( ts >= TimeSpan.FromSeconds( 1.0 ) )
            {
                return TimerPriority.TwentyFiveMS;
            }

            if ( ts >= TimeSpan.FromSeconds( 0.5 ) )
            {
                return TimerPriority.TenMS;
            }

            return TimerPriority.EveryTick;
        }

        public void Start()
        {
            if ( !m_Running )
            {
                m_Running = true;
                TimerThread.AddTimer( this );

                TimerProfile prof = GetProfile();

                if ( prof != null ) {
                    prof.Started++;
                }
            }
        }

        public void Stop()
        {
            if ( m_Running )
            {
                m_Running = false;
                TimerThread.RemoveTimer( this );

                TimerProfile prof = GetProfile();

                if ( prof != null ) {
                    prof.Stopped++;
                }
            }
        }

        protected virtual void OnTick()
        {
        }
    }
}
