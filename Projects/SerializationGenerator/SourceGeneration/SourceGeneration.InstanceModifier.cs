/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SourceGeneration.InstanceModifier.cs                            *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace SerializationGenerator;

public enum InstanceModifier
{
    None,
    Const,
    ReadOnly,
    Static,
    StaticReadOnly
}

public static partial class SourceGeneration
{
    public static string ToFriendlyString(this InstanceModifier modifier) =>
        modifier switch
        {
            InstanceModifier.Const          => "const",
            InstanceModifier.ReadOnly       => "readonly",
            InstanceModifier.Static         => "static",
            InstanceModifier.StaticReadOnly => "static readonly",
            _                               => ""
        };
}