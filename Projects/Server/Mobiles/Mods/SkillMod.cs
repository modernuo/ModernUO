/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SkillMod.cs                                                     *
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

namespace Server.Mobiles;

[SerializationGenerator(0)]
public abstract partial class SkillMod
{
    private bool m_ObeyCap;
    [DirtyTrackingEntity]
    private Mobile m_Owner;

    private bool m_Relative;
    private SkillName m_Skill;
    private double m_Value;

    protected SkillMod(Mobile owner) => m_Owner = owner;

    protected SkillMod(SkillName skill, bool relative, double value, Mobile owner = null)
    {
        m_Owner = owner;
        m_Skill = skill;
        m_Relative = relative;
        m_Value = value;
    }

    [SerializableField(0)]
    public bool ObeyCap
    {
        get => m_ObeyCap;
        set
        {
            m_ObeyCap = value;

            var sk = m_Owner?.Skills[m_Skill];
            sk?.Update();
            m_Owner?.MarkDirty();
        }
    }

    public Mobile Owner
    {
        get => m_Owner;
        set
        {
            if (m_Owner != value)
            {
                m_Owner?.RemoveSkillMod(this);
                m_Owner = value;
                m_Owner?.AddSkillMod(this);
            }
        }
    }

    [SerializableField(1)]
    public SkillName Skill
    {
        get => m_Skill;
        set
        {
            if (m_Skill != value)
            {
                var oldUpdate = m_Owner?.Skills[m_Skill];

                m_Skill = value;

                var sk = m_Owner?.Skills[m_Skill];
                sk?.Update();
                oldUpdate?.Update();
                m_Owner?.MarkDirty();
            }
        }
    }

    [SerializableField(2)]
    public bool Relative
    {
        get => m_Relative;
        set
        {
            if (m_Relative != value)
            {
                m_Relative = value;

                var sk = m_Owner?.Skills[m_Skill];
                sk?.Update();
                m_Owner?.MarkDirty();
            }
        }
    }

    [SerializableField(3)]
    public bool Absolute
    {
        get => !m_Relative;
        set
        {
            if (m_Relative == value)
            {
                m_Relative = !value;

                var sk = m_Owner?.Skills[m_Skill];
                sk?.Update();
                m_Owner?.MarkDirty();
            }
        }
    }

    [SerializableField(4)]
    public double Value
    {
        get => m_Value;
        set
        {
            if (m_Value != value)
            {
                m_Value = value;

                var sk = m_Owner?.Skills[m_Skill];
                sk?.Update();
                m_Owner?.MarkDirty();
            }
        }
    }

    public void Remove()
    {
        Owner = null;
    }

    public abstract bool CheckCondition();
}
