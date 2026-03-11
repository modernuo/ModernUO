/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
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

using System.Runtime.CompilerServices;
using ModernUO.Serialization;

namespace Server;

[SerializationGenerator(0)]
public abstract partial class SkillMod : MobileMod
{
    [SerializableField(0)]
    private bool _obeyCap;

    [SerializableFieldChanged(0)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnObeCapChanged(bool oldValue, bool newValue) => Owner?.Skills[_skill]?.Update();

    [SerializableField(1)]
    private SkillName _skill;

    [SerializableFieldChanged(1)]
    private void OnSkillChanged(SkillName oldValue, SkillName newValue)
    {
        Owner?.Skills[newValue]?.Update();
        Owner?.Skills[oldValue]?.Update();
    }

    [SerializableField(2)]
    private bool _relative;

    [SerializableFieldChanged(2)]
    private void OnRelativeChanged(bool oldValue, bool newValue) => Owner?.Skills[_skill]?.Update();

    [SerializableField(3)]
    private double _value;

    [SerializableFieldChanged(3)]
    private void OnValueChanged(double oldValue, double newValue) => Owner?.Skills[_skill]?.Update();

    public SkillMod(Mobile owner) : base(owner)
    {
    }

    public SkillMod(SkillName skill, string name, bool relative, double value, Mobile owner = null) : base(owner, name)
    {
        _skill = skill;
        _relative = relative;
        _value = value;
    }

    public bool Absolute
    {
        get => !Relative;
        set => Relative = !value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove() => Owner?.RemoveSkillMod(this);

    public abstract bool CheckCondition();
}
