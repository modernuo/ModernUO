/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: NetworkState.cs                                                 *
 * Created: 2020/08/08 - Updated: 2020/08/09                             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Runtime.CompilerServices;
using System.Threading;

namespace Server.Network
{
  public class NetworkState
  {
    public static readonly NetworkState PauseState = new NetworkState(true);
    public static readonly NetworkState ResumeState = new NetworkState(false);

    public bool Paused { get; }

    internal NetworkState(bool paused) => Paused = paused;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Pause(ref NetworkState state) =>
      Interlocked.Exchange(ref state, PauseState)?.Paused != true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Resume(ref NetworkState state) =>
      Interlocked.Exchange(ref state, ResumeState)?.Paused == true;
  }
}
