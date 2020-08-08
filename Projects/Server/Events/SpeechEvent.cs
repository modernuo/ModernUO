/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2020 - ModernUO Development Team                        *
 * Email: hi@modernuo.com                                                *
 * File: SpeechEvent.cs - Created: 2020/04/11 - Updated: 2020/04/12      *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using Server.Network;

namespace Server
{
  public class SpeechEventArgs : EventArgs
  {
    public SpeechEventArgs(Mobile mobile, string speech, MessageType type, int hue, int[] keywords)
    {
      Mobile = mobile;
      Speech = speech;
      Type = type;
      Hue = hue;
      Keywords = keywords;
    }

    public Mobile Mobile { get; }

    public string Speech { get; set; }

    public MessageType Type { get; }

    public int Hue { get; }

    public int[] Keywords { get; }

    public bool Handled { get; set; }

    public bool Blocked { get; set; }

    public bool HasKeyword(int keyword)
    {
      for (var i = 0; i < Keywords.Length; ++i)
        if (Keywords[i] == keyword)
          return true;

      return false;
    }
  }

  public static partial class EventSink
  {
    public static event Action<SpeechEventArgs> Speech;
    public static void InvokeSpeech(SpeechEventArgs e) => Speech?.Invoke(e);
  }
}
