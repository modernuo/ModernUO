/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SimplePrompt.cs                                                 *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Prompts;

namespace Server.Mobiles;

public class SimplePrompt : Prompt
{
    private readonly PromptCallback m_Callback;
    private readonly bool m_CallbackHandlesCancel;
    private readonly PromptCallback m_CancelCallback;

    public SimplePrompt(PromptCallback callback, PromptCallback cancelCallback)
    {
        m_Callback = callback;
        m_CancelCallback = cancelCallback;
    }

    public SimplePrompt(PromptCallback callback, bool callbackHandlesCancel = false)
    {
        m_Callback = callback;
        m_CallbackHandlesCancel = callbackHandlesCancel;
    }

    public override void OnResponse(Mobile from, string text)
    {
        m_Callback?.Invoke(from, text);
    }

    public override void OnCancel(Mobile from)
    {
        if (m_CallbackHandlesCancel && m_Callback != null)
        {
            m_Callback(from, "");
        }
        else
        {
            m_CancelCallback?.Invoke(from, "");
        }
    }
}

public class SimpleStatePrompt<T> : Prompt
{
    private readonly PromptStateCallback<T> m_Callback;
    private readonly PromptStateCallback<T> m_CancelCallback;

    private readonly T m_State;

    public SimpleStatePrompt(PromptStateCallback<T> callback, PromptStateCallback<T> cancelCallback, T state)
    {
        m_Callback = callback;
        m_CancelCallback = cancelCallback;
        m_State = state;
    }

    public SimpleStatePrompt(PromptStateCallback<T> callback, bool callbackHandlesCancel, T state)
    {
        m_Callback = callback;
        m_State = state;
        m_CancelCallback = callbackHandlesCancel ? callback : null;
    }

    public SimpleStatePrompt(PromptStateCallback<T> callback, T state) : this(callback, false, state)
    {
    }

    public override void OnResponse(Mobile from, string text)
    {
        m_Callback?.Invoke(from, text, m_State);
    }

    public override void OnCancel(Mobile from)
    {
        m_CancelCallback?.Invoke(from, "", m_State);
    }
}
