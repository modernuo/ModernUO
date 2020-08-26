/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: MenuPackets.cs - Created: 2020/05/08 - Updated: 2020/05/26      *
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
using Server.ContextMenus;
using Server.Menus;
using Server.Menus.ItemLists;
using Server.Menus.Questions;

namespace Server.Network
{
    [Flags]
    public enum CMEFlags
    {
        None = 0x00,
        Disabled = 0x01,
        Arrow = 0x02,
        Highlighted = 0x04,
        Colored = 0x20
    }

    public sealed class DisplayItemListMenu : Packet
    {
        public DisplayItemListMenu(ItemListMenu menu) : base(0x7C)
        {
            EnsureCapacity(256);

            Stream.Write(((IMenu)menu).Serial);
            Stream.Write((short)0);

            var question = menu.Question;

            if (question == null)
            {
                Stream.Write((byte)0);
            }
            else
            {
                var questionLength = question.Length;
                Stream.Write((byte)questionLength);
                Stream.WriteAsciiFixed(question, questionLength);
            }

            var entries = menu.Entries;

            int entriesLength = (byte)entries.Length;

            Stream.Write((byte)entriesLength);

            for (var i = 0; i < entriesLength; ++i)
            {
                var e = entries[i];

                Stream.Write((ushort)e.ItemID);
                Stream.Write((short)e.Hue);

                var name = e.Name;

                if (name == null)
                {
                    Stream.Write((byte)0);
                }
                else
                {
                    var nameLength = name.Length;
                    Stream.Write((byte)nameLength);
                    Stream.WriteAsciiFixed(name, nameLength);
                }
            }
        }
    }

    public sealed class DisplayQuestionMenu : Packet
    {
        public DisplayQuestionMenu(QuestionMenu menu) : base(0x7C)
        {
            EnsureCapacity(256);

            Stream.Write(((IMenu)menu).Serial);
            Stream.Write((short)0);

            var question = menu.Question;

            if (question == null)
            {
                Stream.Write((byte)0);
            }
            else
            {
                var questionLength = question.Length;
                Stream.Write((byte)questionLength);
                Stream.WriteAsciiFixed(question, questionLength);
            }

            var answers = menu.Answers;

            int answersLength = (byte)answers.Length;

            Stream.Write((byte)answersLength);

            for (var i = 0; i < answersLength; ++i)
            {
                Stream.Write(0);

                var answer = answers[i];

                if (answer == null)
                {
                    Stream.Write((byte)0);
                }
                else
                {
                    var answerLength = answer.Length;
                    Stream.Write((byte)answerLength);
                    Stream.WriteAsciiFixed(answer, answerLength);
                }
            }
        }
    }

    public sealed class DisplayContextMenu : Packet
    {
        public DisplayContextMenu(ContextMenu menu) : base(0xBF)
        {
            var entries = menu.Entries;

            int length = (byte)entries.Length;

            EnsureCapacity(12 + length * 8);

            Stream.Write((short)0x14);
            Stream.Write((short)0x02);

            var target = menu.Target;

            Stream.Write(target.Serial);

            Stream.Write((byte)length);

            var p = target switch
            {
                Mobile _  => target.Location,
                Item item => item.GetWorldLocation(),
                _         => Point3D.Zero
            };

            for (var i = 0; i < length; ++i)
            {
                var e = entries[i];

                Stream.Write(e.Number);
                Stream.Write((short)i);

                var range = e.Range;

                if (range == -1)
                    range = 18;

                var flags = e.Flags;
                if (!(e.Enabled && menu.From.InRange(p, range)))
                    flags |= CMEFlags.Disabled;

                Stream.Write((short)flags);
            }
        }
    }

    public sealed class DisplayContextMenuOld : Packet
    {
        public DisplayContextMenuOld(ContextMenu menu) : base(0xBF)
        {
            var entries = menu.Entries;

            int length = (byte)entries.Length;

            EnsureCapacity(12 + length * 8);

            Stream.Write((short)0x14);
            Stream.Write((short)0x01);

            var target = menu.Target;

            Stream.Write(target.Serial);

            Stream.Write((byte)length);

            var p = target switch
            {
                Mobile _  => target.Location,
                Item item => item.GetWorldLocation(),
                _         => Point3D.Zero
            };

            for (var i = 0; i < length; ++i)
            {
                var e = entries[i];

                Stream.Write((short)i);
                Stream.Write((ushort)(e.Number - 3000000));

                var range = e.Range;

                if (range == -1)
                    range = 18;

                var flags = e.Flags;
                if (!(e.Enabled && menu.From.InRange(p, range)))
                    flags |= CMEFlags.Disabled;

                var color = e.Color & 0xFFFF;

                if (color != 0xFFFF)
                    flags |= CMEFlags.Colored;

                Stream.Write((short)flags);

                if ((flags & CMEFlags.Colored) != 0)
                    Stream.Write((short)color);
            }
        }
    }
}
