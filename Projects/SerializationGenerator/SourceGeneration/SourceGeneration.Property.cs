/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: SourceGeneration.Property.cs                                    *
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
using System.Text;
using Humanizer;
using Microsoft.CodeAnalysis;

namespace SourceGeneration
{
    public static partial class SourceGeneration
    {
        public static string GetPropertyName(this IFieldSymbol fieldSymbol)
        {
            var fieldName = fieldSymbol.Name;

            var propertyName = fieldName;

            if (propertyName.StartsWith("m_", StringComparison.OrdinalIgnoreCase))
            {
                propertyName = propertyName.Substring(2);
            }
            else if (propertyName.StartsWith("_", StringComparison.OrdinalIgnoreCase))
            {
                propertyName = propertyName.Substring(1);
            }

            return propertyName.Dehumanize();
        }

        public static void GeneratePropertyStart(
            this StringBuilder source,
            AccessModifier accessors,
            IFieldSymbol fieldSymbol
        )
        {
            var propertyName = fieldSymbol.GetPropertyName();

            source.AppendLine($@"        {accessors.ToFriendlyString()} {fieldSymbol.Type} {propertyName}
        {{");
        }

        public static void GenerateAutoProperty(
            this StringBuilder source,
            AccessModifier accessors,
            string type,
            string propertyName,
            AccessModifier? getAccessor,
            AccessModifier? setAccessor,
            string indent,
            bool useInit = false,
            string defaultValue = null,
            bool isOverride = false
        )
        {
            if (getAccessor == null && setAccessor == null)
            {
                throw new ArgumentNullException($"Must specify a {nameof(getAccessor)} or {nameof(setAccessor)} parameter");
            }

            var getter = getAccessor == null ?
                "" :
                $"{(getAccessor != AccessModifier.None ? $"{getAccessor.Value.ToFriendlyString()} " : "")}get;";

            var getterSpace = getAccessor != null ? " " : "";
            var setOrInit = useInit ? "init;" : "set;";

            var setterAccessor = setAccessor is null or AccessModifier.None
                ? ""
                : $"{setAccessor.Value.ToFriendlyString() ?? ""} ";

            var setter = setAccessor == null ? "" : $"{getterSpace}{setterAccessor}{setOrInit}";

            var propertyAccessor = accessors == AccessModifier.None ? "" : $"{accessors.ToFriendlyString()} ";
            var printOverride = isOverride ? "override " : "";
            var printDefaultValue = defaultValue != null ? $"{(setAccessor != null ? " =" : "")} {defaultValue};" : "";
            var printGetterSetter = setAccessor == null ? "=>" : $"{{ {getter}{setter} }}";

            source.AppendLine($"{indent}{propertyAccessor}{printOverride}{type} {propertyName} {printGetterSetter}{printDefaultValue}");
        }

        public static void GeneratePropertyEnd(this StringBuilder source) => source.AppendLine("        }");

        public static void GeneratePropertyGetterReturnsField(this StringBuilder source, IFieldSymbol fieldSymbol) =>
            source.AppendLine($"            get => {fieldSymbol.Name};");

        public static void GeneratePropertyGetterStart(this StringBuilder source, bool useExpression) =>
            source.AppendLine($"            get{(useExpression ? " => " : "\n            {")}");

        public static void GeneratePropertyGetSetEnd(this StringBuilder source, bool useExpression)
        {
            if (!useExpression)
            {
                source.AppendLine("            }");
            }
        }

        public static void GeneratePropertySetterSetsValue(this StringBuilder source, IFieldSymbol fieldSymbol) =>
            source.AppendLine($"            set => {fieldSymbol.Name} = value;");

        public static void GeneratePropertySetterStart(this StringBuilder source, bool useExpression, bool useInit = false) =>
            source.AppendLine($"            {(useInit ? "init" : "set")}{(useExpression ? " => " : "\n            {")}");
    }
}
