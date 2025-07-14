/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AITimer.cs                                                      *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program. If not, see <http://www.gnu.org/licenses/>.  *
 ************************************************************************/

using System;

namespace Server.Mobiles
{
     internal sealed class AITimer : Timer
     {
          private readonly BaseAI m_Owner;
          private int _detectHiddenMinDelay;
          private int _detectHiddenMaxDelay;

          public AITimer(BaseAI owner) : base(TimeSpan.FromMilliseconds(Utility.Random(3000)), 
               TimeSpan.FromMilliseconds(GetBaseInterval(owner)))
          {
               m_Owner = owner;
               m_Owner.m_NextDetectHidden = Core.TickCount;
          }

          private static double GetBaseInterval(BaseAI owner)
          {
               double interval;

               if (owner.m_Mobile.Controlled && owner.m_Mobile.ControlOrder == OrderType.Follow 
                    && owner.m_Mobile.Combatant != owner.m_Mobile.ControlMaster)
               {
                    interval = owner.m_Mobile.CurrentSpeed * 400;
               }
               else if (owner.m_Mobile.CurrentSpeed <= 0.4)
               {
                    interval = owner.m_Mobile.CurrentSpeed * 1000;
               }
               else
               {
                    interval = owner.m_Mobile.CurrentSpeed * 3000;
               }

               return Math.Max(interval, 200);
          }

          protected override void OnTick()
          {
               if (ShouldStop())
               {
                    Stop();
                    return;
               }

               var newInterval = TimeSpan.FromMilliseconds(GetBaseInterval(m_Owner));

               if (Interval != newInterval)
               {
                    Interval = newInterval;
               }

               m_Owner.m_Mobile.OnThink();

               if (ShouldStop())
               {
                    Stop();
                    return;
               }

               HandleBardEffects();

               if (m_Owner.m_Mobile.Controlled ? !m_Owner.Obey() : !m_Owner.Think())
               {
                    Stop();
                    return;
               }

               HandleDetectHidden();
          }

          private bool ShouldStop()
          {
               if (m_Owner.m_Mobile.Deleted)
               {
                    return true;
               }

               if (m_Owner.m_Mobile.Map == null || m_Owner.m_Mobile.Map == Map.Internal)
               {
                    m_Owner.Deactivate();
                    return true;
               }

               if (m_Owner.m_Mobile.PlayerRangeSensitive &&
                    !m_Owner.m_Mobile.Map.GetSector(m_Owner.m_Mobile.Location).Active)
               {
                    m_Owner.Deactivate();
                    return true;
               }

               return false;
          }

          private void HandleBardEffects()
          {
               if (m_Owner.m_Mobile.BardPacified)
               {
                    m_Owner.DoBardPacified();
               }
               else if (m_Owner.m_Mobile.BardProvoked)
               {
                    m_Owner.DoBardProvoked();
               }
          }

          private void CacheDetectHiddenDelays()
          {
               var delay = Math.Min(30000 / m_Owner.m_Mobile.Int, 120);
               _detectHiddenMinDelay = delay * 900;  // 26s to 108s
               _detectHiddenMaxDelay = delay * 1100; // 32s to 132s
          }

          private void HandleDetectHidden()
          {
               if (!m_Owner.CanDetectHidden || Core.TickCount - m_Owner.m_NextDetectHidden < 0)
               {
                    return;
               }

               m_Owner.DetectHidden();

               if (_detectHiddenMinDelay == 0 || _detectHiddenMaxDelay == 0)
               {
                    CacheDetectHiddenDelays();
               }

               m_Owner.m_NextDetectHidden = Core.TickCount +
                    Utility.RandomMinMax(_detectHiddenMinDelay, _detectHiddenMaxDelay);
          }
     }
}