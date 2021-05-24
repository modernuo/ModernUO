/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableEntityGeneration.Property.cs                        *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Text;
using Microsoft.CodeAnalysis;

namespace SerializationGenerator
{
    public static partial class SerializableEntityGeneration
    {
        public static void GenerateSerializableProperty(
            this StringBuilder source,
            IFieldSymbol fieldSymbol
        )
        {
            var fieldName = fieldSymbol.Name;

            source.GeneratePropertyStart(AccessModifier.Public, fieldSymbol);

            // Getter
            source.GeneratePropertyGetterReturnsField(fieldSymbol);

            // Setter
            source.GeneratePropertySetterStart(false);
            source.AppendLine(
                $@"                if (value != {fieldName})
                {{
                    ((ISerializable)this).MarkDirty();
                    {fieldName} = value;
                }}"
            );
            source.GeneratePropertyGetSetEnd(false);

            source.GeneratePropertyEnd();
        }
    }
}
