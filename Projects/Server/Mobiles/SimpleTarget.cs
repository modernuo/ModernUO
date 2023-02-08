/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SimpleTarget.cs                                                 *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Targeting;

namespace Server.Mobiles;

public class SimpleTarget : Target
{
    private readonly TargetCallback m_Callback;

    public SimpleTarget(int range, TargetFlags flags, bool allowGround, TargetCallback callback)
        : base(range, allowGround, flags) =>
        m_Callback = callback;

    protected override void OnTarget(Mobile from, object targeted)
    {
        m_Callback?.Invoke(from, targeted);
    }
}

public class SimpleStateTarget<T> : Target
{
    private readonly TargetStateCallback<T> m_Callback;
    private readonly T m_State;

    public SimpleStateTarget(
        int range, TargetFlags flags, bool allowGround, TargetStateCallback<T> callback,
        T state
    )
        : base(range, allowGround, flags)
    {
        m_Callback = callback;
        m_State = state;
    }

    protected override void OnTarget(Mobile from, object targeted)
    {
        m_Callback?.Invoke(from, targeted, m_State);
    }
}
