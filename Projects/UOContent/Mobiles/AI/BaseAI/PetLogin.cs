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

    // PersistentOrder is runtime-only; ControlOrder and Home are saved. A saved standing order is
    // adopted as is; a pet saved mid-transient gets Follow near the master, else Stay, issued silently.
    public static void DeriveFollowerOrders(PlayerMobile master)
    {
        if (master?.AllFollowers == null)
        {
            return;
        }

        foreach (var follower in master.AllFollowers)
        {
            if (follower is not BaseCreature { Controlled: true, Deleted: false } bc
                || bc.ControlMaster != master
                || bc.AIObject is not { } ai
                || ai.PersistentOrder != OrderType.None)
            {
                continue;
            }

            var restored = bc.ControlOrder;

            if (restored is OrderType.Stay or OrderType.Follow or OrderType.Guard)
            {
                ai.RestorePersistentOrder(restored);
                continue;
            }

            var near = bc.Map == master.Map && bc.GetDistanceToSqrt(master) <= FollowRange;
            var derived = near ? OrderType.Follow : OrderType.Stay;

            // ControlTarget first: SetPersistentOrder records it as the Follow target, and a mid-Attack
            // save still holds the victim.
            bc.ControlTarget = near ? master : null;
            ai.SetPersistentOrder(derived);
            bc.SetControlOrder(derived, null, true);
        }
    }
}
