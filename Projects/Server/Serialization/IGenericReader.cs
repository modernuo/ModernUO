/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: IGenericReader.cs                                               *
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
    public interface IGenericReader
    {
        string ReadString();
        DateTime ReadDateTime();
        DateTimeOffset ReadDateTimeOffset();
        TimeSpan ReadTimeSpan();
        DateTime ReadDeltaTime();
        decimal ReadDecimal();
        long ReadLong();
        ulong ReadULong();
        int ReadInt();
        uint ReadUInt();
        short ReadShort();
        ushort ReadUShort();
        double ReadDouble();
        float ReadFloat();
        char ReadChar();
        byte ReadByte();
        sbyte ReadSByte();
        bool ReadBool();
        int ReadEncodedInt();
        IPAddress ReadIPAddress();
        Point3D ReadPoint3D();
        Point2D ReadPoint2D();
        Rectangle2D ReadRect2D();
        Rectangle3D ReadRect3D();
        Map ReadMap();
        IEntity ReadEntity();
        Item ReadItem();
        Mobile ReadMobile();
        BaseGuild ReadGuild();
        T ReadItem<T>() where T : Item;
        T ReadMobile<T>() where T : Mobile;
        T ReadGuild<T>() where T : BaseGuild;
        List<Item> ReadStrongItemList();
        List<T> ReadStrongItemList<T>() where T : Item;
        List<Mobile> ReadStrongMobileList();
        List<T> ReadStrongMobileList<T>() where T : Mobile;
        List<BaseGuild> ReadStrongGuildList();
        List<T> ReadStrongGuildList<T>() where T : BaseGuild;
        HashSet<Item> ReadItemSet();
        HashSet<T> ReadItemSet<T>() where T : Item;
        HashSet<Mobile> ReadMobileSet();
        HashSet<T> ReadMobileSet<T>() where T : Mobile;
        HashSet<BaseGuild> ReadGuildSet();
        HashSet<T> ReadGuildSet<T>() where T : BaseGuild;
        Race ReadRace();
        bool End();
    }
}
