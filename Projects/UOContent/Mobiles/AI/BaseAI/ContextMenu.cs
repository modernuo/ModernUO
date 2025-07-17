/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ContextMenu.cs                                                  *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program. If not, see <http://www.gnu.org/licenses/>.  *
 ************************************************************************/

using Server.Collections;
using Server.ContextMenus;

namespace Server.Mobiles;

public partial class BaseAI
{
    public virtual void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        if (!from.Alive || !m_Mobile.Controlled || !from.InRange(m_Mobile, 16))
        {
            return;
        }

        if (from == m_Mobile.ControlMaster)
        {
            AddControlMasterEntries(ref list);
        }
        else if (m_Mobile.IsPetFriend(from))
        {
            AddPetFriendEntries(ref list);
        }
    }

    private void AddControlMasterEntries(ref PooledRefList<ContextMenuEntry> list)
    {
        var isDeadPet = m_Mobile.IsDeadPet;

        list.Add(new InternalEntry(3006111, 14, OrderType.Attack, !isDeadPet)); // Command: Kill
        list.Add(new InternalEntry(3006108, 14, OrderType.Follow, true));       // Command: Follow
        list.Add(new InternalEntry(3006107, 14, OrderType.Guard, !isDeadPet));  // Command: Guard
        list.Add(new InternalEntry(3006112, 14, OrderType.Stop, true));         // Command: Stop
        list.Add(new InternalEntry(3006114, 14, OrderType.Stay, true));         // Command: Stay

        if (m_Mobile.CanDrop)
        {
            list.Add(new InternalEntry(3006109, 14, OrderType.Drop, !isDeadPet)); // Command: Drop
        }

        list.Add(new InternalEntry(3006098, 14, OrderType.Rename, true)); // Rename

        if (!m_Mobile.Summoned && m_Mobile is not GrizzledMare)
        {
            list.Add(new InternalEntry(3006110, 14, OrderType.Friend, true));         // Add Friend
            list.Add(new InternalEntry(3006099, 14, OrderType.Unfriend, true));       // Remove Friend
            list.Add(new InternalEntry(3006113, 14, OrderType.Transfer, !isDeadPet)); // Transfer
        }

        list.Add(new InternalEntry(3006118, 14, OrderType.Release, true)); // Release
    }

    private void AddPetFriendEntries(ref PooledRefList<ContextMenuEntry> list)
    {
        var isDeadPet = m_Mobile.IsDeadPet;

        list.Add(new InternalEntry(3006108, 14, OrderType.Follow, true));     // Command: Follow
        list.Add(new InternalEntry(3006112, 14, OrderType.Stop, !isDeadPet)); // Command: Stop
        list.Add(new InternalEntry(3006114, 14, OrderType.Stay, true));       // Command: Stay
    }
}