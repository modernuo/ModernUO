/***************************************************************************
 *                              ItemListMenu.cs
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

namespace Server.Menus
{
  public class ItemListEntry
  {
    public ItemListEntry(string name, int itemID, int hue = 0)
    {
      Name = name;
      ItemID = itemID;
      Hue = hue;
    }

    public string Name{ get; }

    public int ItemID{ get; }

    public int Hue{ get; }
  }

  public class ItemListMenu : IMenu
  {
    private static Serial m_NextSerial;

    public ItemListMenu(string question, ItemListEntry[] entries)
    {
      Question = question;
      Entries = entries;

      do
      {
        Serial = m_NextSerial++;
        Serial &= 0x7FFFFFFF;
      } while (Serial == 0);

      Serial |= 0x80000000;
    }

    public string Question{ get; }

    public ItemListEntry[] Entries{ get; set; }

    public Serial Serial { get; private set; }

    int IMenu.EntryLength => Entries.Length;

    public virtual void OnCancel(NetState state)
    {
    }

    public virtual void OnResponse(NetState state, int index)
    {
    }

    public void SendTo(NetState state)
    {
      state.AddMenu(this);
      Packets.SendDisplayItemListMenu(state, this);
    }
  }
}
