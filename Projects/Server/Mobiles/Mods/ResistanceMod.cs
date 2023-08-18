/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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

using ModernUO.Serialization;

namespace Server;

[SerializationGenerator(0)]
public partial class ResistanceMod : MobileMod
{
    public ResistanceMod(Mobile owner) : base(owner)
    {
    }

    public ResistanceMod(ResistanceType type, string name, int offset, Mobile owner = null) : base(owner, name)
    {
        _type = type;
        _offset = offset;
    }

    [SerializableProperty(0)]
    public ResistanceType Type
    {
        get => _type;
        set
        {
            if (_type != value)
            {
                _type = value;

                Owner?.UpdateResistances();
                MarkDirty();
            }
        }
    }

    [SerializableProperty(1)]
    public int Offset
    {
        get => _offset;
        set
        {
            if (_offset != value)
            {
                _offset = value;

                Owner?.UpdateResistances();
                MarkDirty();
            }
        }
    }
}
