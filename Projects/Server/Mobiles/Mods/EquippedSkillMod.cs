/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: EquippedSkillMod.cs                                             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using ModernUO.Serialization;

namespace Server;

[SerializationGenerator(0)]
public partial class EquippedSkillMod : SkillMod
{
    [SerializableField(0)]
    private Item _item;

    public EquippedSkillMod(Mobile owner) : base(owner)
    {
    }

    public EquippedSkillMod(SkillName skill, string name, bool relative, double value, Item item, Mobile owner)
        : base(skill, name, relative, value, owner) => _item = item;

    public override bool CheckCondition() => !_item.Deleted && Owner?.Deleted == false && _item.Parent == Owner;
}
