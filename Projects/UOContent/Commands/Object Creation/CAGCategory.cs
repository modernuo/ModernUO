/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: CAGCategory.cs                                                  *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Gumps;

namespace Server.Commands
{
    public class CAGCategory : CAGNode
    {
        private static CAGCategory m_Root;

        public CAGCategory(string title, CAGCategory parent = null)
        {
            Title = title;
            Parent = parent;
        }

        public override string Title { get; }

        public CAGNode[] Nodes { get; set; }

        public CAGCategory Parent { get; }

        public static CAGCategory Root => m_Root ??= CAGLoader.Load();

        public override void OnClick(Mobile from, int page)
        {
            from.SendGump(new CategorizedAddGump(from, this));
        }
    }
}
