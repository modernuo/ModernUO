/***************************************************************************
 *                              AggressorInfo.cs
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
using System.Diagnostics;
using System.IO;

namespace Server
{
    public class AggressorInfo
    {
        private static readonly Queue<AggressorInfo> m_Pool = new Queue<AggressorInfo>();
        private Mobile m_Attacker, m_Defender;
        private bool m_CanReportMurder;
        private bool m_CriminalAggression;
        private DateTime m_LastCombatTime;

        private bool m_Queued;
        private bool m_Reported;

        private AggressorInfo(Mobile attacker, Mobile defender, bool criminal)
        {
            m_Attacker = attacker;
            m_Defender = defender;

            m_CanReportMurder = criminal;
            m_CriminalAggression = criminal;

            Refresh();
        }

        public static TimeSpan ExpireDelay { get; set; } = TimeSpan.FromMinutes(2.0);

        public bool Expired
        {
            get
            {
                if (m_Queued)
                    DumpAccess();

                return m_Attacker.Deleted || m_Defender.Deleted || DateTime.UtcNow >= m_LastCombatTime + ExpireDelay;
            }
        }

        public bool CriminalAggression
        {
            get
            {
                if (m_Queued)
                    DumpAccess();

                return m_CriminalAggression;
            }
            set
            {
                if (m_Queued)
                    DumpAccess();

                m_CriminalAggression = value;
            }
        }

        public Mobile Attacker
        {
            get
            {
                if (m_Queued)
                    DumpAccess();

                return m_Attacker;
            }
        }

        public Mobile Defender
        {
            get
            {
                if (m_Queued)
                    DumpAccess();

                return m_Defender;
            }
        }

        public DateTime LastCombatTime
        {
            get
            {
                if (m_Queued)
                    DumpAccess();

                return m_LastCombatTime;
            }
        }

        public bool Reported
        {
            get
            {
                if (m_Queued)
                    DumpAccess();

                return m_Reported;
            }
            set
            {
                if (m_Queued)
                    DumpAccess();

                m_Reported = value;
            }
        }

        public bool CanReportMurder
        {
            get
            {
                if (m_Queued)
                    DumpAccess();

                return m_CanReportMurder;
            }
            set
            {
                if (m_Queued)
                    DumpAccess();

                m_CanReportMurder = value;
            }
        }

        public static AggressorInfo Create(Mobile attacker, Mobile defender, bool criminal)
        {
            AggressorInfo info;

            if (m_Pool.Count > 0)
            {
                info = m_Pool.Dequeue();

                info.m_Attacker = attacker;
                info.m_Defender = defender;

                info.m_CanReportMurder = criminal;
                info.m_CriminalAggression = criminal;

                info.m_Queued = false;

                info.Refresh();
            }
            else
            {
                info = new AggressorInfo(attacker, defender, criminal);
            }

            return info;
        }

        public void Free()
        {
            if (m_Queued)
                return;

            m_Queued = true;
            m_Pool.Enqueue(this);
        }

        public static void DumpAccess()
        {
            using var op = new StreamWriter("warnings.log", true);
            op.WriteLine("Warning: Access to queued AggressorInfo:");
            op.WriteLine(new StackTrace());
            op.WriteLine();
            op.WriteLine();
        }

        public void Refresh()
        {
            if (m_Queued)
                DumpAccess();

            m_LastCombatTime = DateTime.UtcNow;
            m_Reported = false;
        }
    }
}
