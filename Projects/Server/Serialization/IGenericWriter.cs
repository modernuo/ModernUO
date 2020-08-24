/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: IGenericWriter.cs                                               *
 * Created: 2019/12/30 - Updated: 2020/01/18                             *
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
using System.Collections.Generic;
using System.Net;
using Server.Guilds;

namespace Server
{
    public interface IGenericWriter
    {
        long Position { get; }

        void Close();

        void Write(string value);
        void Write(DateTime value);
        void Write(DateTimeOffset value);
        void Write(TimeSpan value);
        void Write(decimal value);
        void Write(long value);
        void Write(ulong value);
        void Write(int value);
        void Write(uint value);
        void Write(short value);
        void Write(ushort value);
        void Write(double value);
        void Write(float value);
        void Write(char value);
        void Write(byte value);
        void Write(byte[] value);
        void Write(byte[] value, int length);
        void Write(sbyte value);
        void Write(bool value);
        void WriteEncodedInt(int value);
        void Write(IPAddress value);

        void WriteDeltaTime(DateTime value);

        void Write(Point3D value);
        void Write(Point2D value);
        void Write(Rectangle2D value);
        void Write(Rectangle3D value);
        void Write(Map value);

        void WriteEntity(IEntity value);
        void Write(Item value);
        void Write(Mobile value);
        void Write(BaseGuild value);

        void WriteItem<T>(T value) where T : Item;
        void WriteMobile<T>(T value) where T : Mobile;
        void WriteGuild<T>(T value) where T : BaseGuild;

        void Write(Race value);

        void Write(List<Item> list);
        void Write(List<Item> list, bool tidy);

        void WriteItemList<T>(List<T> list) where T : Item;
        void WriteItemList<T>(List<T> list, bool tidy) where T : Item;

        void Write(HashSet<Item> list);
        void Write(HashSet<Item> list, bool tidy);

        void WriteItemSet<T>(HashSet<T> set) where T : Item;
        void WriteItemSet<T>(HashSet<T> set, bool tidy) where T : Item;

        void Write(List<Mobile> list);
        void Write(List<Mobile> list, bool tidy);

        void WriteMobileList<T>(List<T> list) where T : Mobile;
        void WriteMobileList<T>(List<T> list, bool tidy) where T : Mobile;

        void Write(HashSet<Mobile> list);
        void Write(HashSet<Mobile> list, bool tidy);

        void WriteMobileSet<T>(HashSet<T> set) where T : Mobile;
        void WriteMobileSet<T>(HashSet<T> set, bool tidy) where T : Mobile;

        void Write(List<BaseGuild> list);
        void Write(List<BaseGuild> list, bool tidy);

        void WriteGuildList<T>(List<T> list) where T : BaseGuild;
        void WriteGuildList<T>(List<T> list, bool tidy) where T : BaseGuild;

        void Write(HashSet<BaseGuild> list);
        void Write(HashSet<BaseGuild> list, bool tidy);

        void WriteGuildSet<T>(HashSet<T> set) where T : BaseGuild;
        void WriteGuildSet<T>(HashSet<T> set, bool tidy) where T : BaseGuild;
    }
}
