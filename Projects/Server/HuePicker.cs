/***************************************************************************
 *                               HuePicker.cs
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

using Server.Network;

namespace Server.HuePickers
{
    public class HuePicker
    {
        private static int m_NextSerial = 1;

        public HuePicker(int itemID)
        {
            do
            {
                Serial = m_NextSerial++;
            } while (Serial == 0);

            ItemID = itemID;
        }

        public int Serial { get; }

        public int ItemID { get; }

        public virtual void OnResponse(int hue)
        {
        }

        public void SendTo(NetState state)
        {
            state.Send(new DisplayHuePicker(this));
            state.AddHuePicker(this);
        }
    }
}
