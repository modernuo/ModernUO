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

using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace SerializationGenerator
{
    public static partial class SerializableEntityGeneration
    {
        public static void GenerateSerializableProperty(
            this StringBuilder source,
            Compilation compilation,
            IFieldSymbol fieldSymbol,
            AccessModifier getter,
            AccessModifier setter
        )
        {
            var fieldName = fieldSymbol.Name;

            var invalidatePropertiesAttribute = fieldSymbol
                .GetAttributes()
                .OfType<AttributeData>()
                .FirstOrDefault(
                    attr => attr.AttributeClass?.Equals(
                        compilation.GetTypeByMetadataName(SymbolMetadata.INVALIDATEPROPERTIES_ATTRIBUTE),
                        SymbolEqualityComparer.Default
                    ) ?? false
                );

            const string indent = "        ";
            const string propertyIndent = "            ";
            var propertyAccessor = SourceGeneration.GetLeastRestrictiveModifier(getter, setter);
            var getterAccessor = getter == propertyAccessor ? AccessModifier.None : getter;
            var setterAccessor = setter == propertyAccessor ? AccessModifier.None : setter;

            source.GeneratePropertyStart(indent, propertyAccessor, fieldSymbol);

            // Getter
            source.GeneratePropertyGetterReturnsField(propertyIndent, fieldSymbol, getterAccessor);

            // Setter
            source.GeneratePropertySetterStart(propertyIndent, false, setterAccessor);
            const string innerIndent = "                ";
            source.AppendLine($"{innerIndent}if (value != {fieldName})");
            source.AppendLine($"{innerIndent}{{");
            source.AppendLine($"{innerIndent}    {fieldName} = value;");
            source.AppendLine($"{innerIndent}    ((ISerializable)this).MarkDirty();");

            if (invalidatePropertiesAttribute != null)
            {
                source.AppendLine($"{innerIndent}    InvalidateProperties();");
            }
            source.AppendLine($"{innerIndent}}}");
            source.GeneratePropertyGetSetEnd(propertyIndent, false);

            source.GeneratePropertyEnd(indent);
        }
    }
}
