/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: TimedSkillMod.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using ModernUO.Serialization;

namespace Server;

[SerializationGenerator(0)]
public partial class TimedSkillMod : SkillMod
{
    [SerializableField(0, setter: "private")]
    private DateTime _expire;

    public TimedSkillMod(Mobile owner) : base(owner)
    {
    }

    public TimedSkillMod(SkillName skill, bool relative, double value, TimeSpan delay)
        : this(skill, relative, value, Core.Now + delay)
    {
    }

    public TimedSkillMod(SkillName skill, bool relative, double value, DateTime expire)
        : base(skill, relative, value) => _expire = expire;

    public override bool CheckCondition() => Core.Now < _expire;
}
