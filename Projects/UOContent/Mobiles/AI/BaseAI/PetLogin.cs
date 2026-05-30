/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PetLogin.cs                                                     *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program. If not, see <http://www.gnu.org/licenses/>.  *
 ************************************************************************/

using ModernUO.CodeGeneratedEvents;

namespace Server.Mobiles;

public static class PetLoginHandler
{
    // Within this many tiles of the master we assume the pet was following; otherwise staying.
    private const int FollowRange = 12;

    [OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
    public static void OnLogin(PlayerMobile pm) => DeriveFollowerOrders(pm);

    // The persistent command is runtime-only and reset to None on load. When the master logs
    // in we give each controlled pet that still has no standing command a sane one, inferred
    // from proximity: near master -> Follow, otherwise Stay.
    public static void DeriveFollowerOrders(PlayerMobile master)
    {
        if (master?.AllFollowers == null)
        {
            return;
        }

        foreach (var follower in master.AllFollowers)
        {
            if (follower is BaseCreature { Controlled: true, Deleted: false } bc
                && bc.ControlMaster == master
                && bc.AIObject is { } ai
                && ai.PersistentOrder == OrderType.None)
            {
                var near = bc.Map == master.Map && bc.GetDistanceToSqrt(master) <= FollowRange;
                ai.SetPersistentOrder(near ? OrderType.Follow : OrderType.Stay);
            }
        }
    }
}
