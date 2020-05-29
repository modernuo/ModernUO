/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: AccountPackets.cs - Created: 2020/05/08 - Updated: 2020/05/08   *
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

using Server.Accounting;

namespace Server.Network
{
  public enum PMMessage : byte
  {
    CharNoExist = 1,
    CharExists = 2,
    CharInWorld = 5,
    LoginSyncError = 6,
    IdleWarning = 7
  }

  public enum DeleteResultType
  {
    PasswordInvalid,
    CharNotExist,
    CharBeingPlayed,
    CharTooYoung,
    CharQueued,
    BadRequest
  }

  public sealed class ChangeCharacter : Packet
  {
    public ChangeCharacter(IAccount a) : base(0x81)
    {
      EnsureCapacity(305);

      var count = 0;

      for (var i = 0; i < a.Length; ++i)
        if (a[i] != null)
          ++count;

      Stream.Write((byte)count);
      Stream.Write((byte)0);

      for (var i = 0; i < a.Length; ++i)
        if (a[i] != null)
        {
          var name = a[i].Name;

          if (name == null)
            name = "-null-";
          else if ((name = name.Trim()).Length == 0)
            name = "-empty-";

          Stream.WriteAsciiFixed(name, 30);
          Stream.Fill(30); // password
        }
        else
        {
          Stream.Fill(60);
        }
    }
  }

  /// <summary>
  ///   Asks the client for it's version
  /// </summary>
  public sealed class ClientVersionReq : Packet
  {
    public ClientVersionReq() : base(0xBD)
    {
      EnsureCapacity(3);
    }
  }

  public sealed class DeleteResult : Packet
  {
    public DeleteResult(DeleteResultType res) : base(0x85, 2)
    {
      Stream.Write((byte)res);
    }
  }

  public sealed class PopupMessage : Packet
  {
    public PopupMessage(PMMessage msg) : base(0x53, 2)
    {
      Stream.Write((byte)msg);
    }
  }
}
