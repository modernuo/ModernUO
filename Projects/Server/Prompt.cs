/***************************************************************************
 *                                 Prompt.cs
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

namespace Server.Prompts
{
    public abstract class Prompt
    {
        private static int m_Serials;

        protected Prompt()
        {
            do
            {
                Serial = ++m_Serials;
            } while (Serial == 0);
        }

        public int Serial { get; }

        public virtual void OnCancel(Mobile from)
        {
        }

        public virtual void OnResponse(Mobile from, string text)
        {
        }
    }
}
