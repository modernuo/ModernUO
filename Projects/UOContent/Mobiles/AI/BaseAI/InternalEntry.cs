/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: InternalEntry.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program. If not, see <http://www.gnu.org/licenses/>.  *
 ************************************************************************/

using Server.ContextMenus;
using Server.Gumps;

namespace Server.Mobiles;

internal sealed class InternalEntry : ContextMenuEntry
{
    private readonly OrderType _order;

    public InternalEntry(int number, int range, OrderType order, bool enabled) : base(number, range)
    {
        _order = order;
        Enabled = enabled;
    }

    public override void OnClick(Mobile from, IEntity target)
    {
        if (!IsValidClick(from, target, out var bc) ||
            IsInvalidOrderForDeadPet(bc) ||
            !IsOwnerOrFriend(from, bc, out var isFriend) ||
            IsInvalidOrderForFriend(isFriend))
        {
            return;
        }

        HandleOrder(from, bc);
    }

    private static bool IsValidClick(Mobile from, IEntity target, out BaseCreature bc)
    {
        bc = target as BaseCreature;
        return from.CheckAlive() && bc != null && !bc.Deleted && bc.Controlled;
    }

    private bool IsInvalidOrderForDeadPet(BaseCreature bc) => bc.IsDeadPet && _order is OrderType.Guard or OrderType.Attack or OrderType.Transfer or OrderType.Drop;

    private static bool IsOwnerOrFriend(Mobile from, BaseCreature bc, out bool isFriend)
    {
        var isOwner = from == bc.ControlMaster;
        isFriend = !isOwner && bc.IsPetFriend(from);
        return isOwner || isFriend;
    }

    private bool IsInvalidOrderForFriend(bool isFriend) => isFriend && _order is not (OrderType.Follow or OrderType.Stay or OrderType.Stop);

    private void HandleOrder(Mobile from, BaseCreature bc)
    {
        switch (_order)
        {
            case OrderType.Follow:
            case OrderType.Attack:
            case OrderType.Transfer:
            case OrderType.Friend:
            case OrderType.Unfriend:
                {
                    HandleTargetOrder(from, bc);
                    break;
                }
            case OrderType.Release:
                {
                    HandleReleaseOrder(from, bc);
                    break;
                }
            default:
                {
                    HandleDefaultOrder(from, bc);
                    break;
                }
        }
    }

    private void HandleTargetOrder(Mobile from, BaseCreature bc)
    {
        if (_order is OrderType.Transfer or OrderType.Friend && from.HasTrade)
        {
            from.SendLocalizedMessage(_order == OrderType.Transfer ? 1010507 : 1070947);
            // 1010507: You cannot transfer a pet with a trade pending
            // 1070947: You cannot friend a pet with a trade pending
            return;
        }

        bc.AIObject.BeginPickTarget(from, _order);
    }

    private void HandleReleaseOrder(Mobile from, BaseCreature bc)
    {
        if (bc.Summoned)
        {
            HandleDefaultOrder(from, bc);
            return;
        }

        from.SendGump(new ConfirmReleaseGump(from, bc));
    }

    private void HandleDefaultOrder(Mobile from, BaseCreature bc)
    {
        if (bc.CheckControlChance(from))
        {
            bc.ControlTarget = null;
            bc.ControlOrder = _order;
        }
    }
}
