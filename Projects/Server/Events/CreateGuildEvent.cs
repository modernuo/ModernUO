/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2020 - ModernUO Development Team                        *
 * Email: hi@modernuo.com                                                *
 * File: CreateGuildEvent.cs                                             *
 * Created: 2020/04/11 - Updated: 2020/04/11                             *
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
using Server.Guilds;

namespace Server
{
    public class CreateGuildEventArgs : EventArgs
    {
        public CreateGuildEventArgs(uint id) => Id = id;

        public uint Id { get; set; }

        public BaseGuild Guild { get; set; }
    }

    public static partial class EventSink
    {
        public static event Action<CreateGuildEventArgs> CreateGuild;
        public static void InvokeCreateGuild(CreateGuildEventArgs e) => CreateGuild?.Invoke(e);
    }
}
