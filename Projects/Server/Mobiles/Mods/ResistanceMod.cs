/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ResistanceMod.cs                                                *
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
public partial class ResistanceMod : MobileMod
{
    [SerializableField(0)]
    private ResistanceType _type;

    [SerializableFieldChanged(0)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnTypeChanged(ResistanceType oldValue, ResistanceType newValue) => Owner?.UpdateResistances();

    [SerializableField(1)]
    private int _offset;

    [SerializableFieldChanged(1)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnOffsetChanged(int oldValue, int newValue) => Owner?.UpdateResistances();

    public ResistanceMod(Mobile owner) : base(owner)
    {
    }

    public ResistanceMod(ResistanceType type, string name, int offset, Mobile owner = null) : base(owner, name)
    {
        _type = type;
        _offset = offset;
    }
}
