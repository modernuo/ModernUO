/***************************************************************************
 *                              QuestionMenu.cs
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
using Server.Network.Packets;

namespace Server.Menus
{
  public class QuestionMenu : IMenu
  {
    private static Serial m_NextSerial;

    public QuestionMenu(string question, string[] answers)
    {
      Question = question;
      Answers = answers;

      do
      {
        Serial = ++m_NextSerial;
        Serial &= 0x7FFFFFFF;
      } while (Serial == 0);
    }

    public string Question{ get; set; }

    public string[] Answers{ get; }

    public Serial Serial { get; private set; }

    int IMenu.EntryLength => Answers.Length;

    public virtual void OnCancel(NetState state)
    {
    }

    public virtual void OnResponse(NetState state, int index)
    {
    }

    public void SendTo(NetState state)
    {
      state.AddMenu(this);
      Packets.SendDisplayQuestionMenu(state, this);
    }
  }
}
