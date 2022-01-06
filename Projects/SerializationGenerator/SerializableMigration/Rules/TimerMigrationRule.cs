/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: TimerMigrationRule.cs                                           *
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
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using SerializationGenerator;

namespace SerializableMigration;

public class TimerMigrationRule : MigrationRule, IPostDeserializeMethod
{
    public override string RuleName => nameof(TimerMigrationRule);

    public override bool GenerateRuleState(
        Compilation compilation,
        ISymbol symbol,
        ImmutableArray<AttributeData> attributes,
        ImmutableArray<INamedTypeSymbol> serializableTypes,
        ImmutableArray<INamedTypeSymbol> embeddedSerializableTypes,
        ISymbol? parentSymbol,
        out string[] ruleArguments
    )
    {
        if (!(symbol is ITypeSymbol typeSymbol && typeSymbol.IsTimer(compilation)))
        {
            ruleArguments = null;
            return false;
        }

        ruleArguments = attributes.Any(a => a.IsTimerDrift(compilation))
            ? new[] { "@TimerDrift" }
            : new[] { "" };

        return true;
    }

    public override void GenerateMigrationProperty(
        StringBuilder source, Compilation compilation, string indent, SerializableProperty serializableProperty
    )
    {
        source.AppendLine($"{indent}internal readonly System.DateTime {serializableProperty.Name}Next;");
        source.AppendLine($"{indent}internal readonly System.TimeSpan {serializableProperty.Name}Delay;");
    }

    public override void GenerateDeserializationMethod(
        StringBuilder source, string indent, SerializableProperty property, string? parentReference, bool isMigration = false
    )
    {
        var expectedRule = RuleName;
        var ruleName = property.Rule;
        if (expectedRule != ruleName)
        {
            throw new ArgumentException($"Invalid rule applied to property {ruleName}. Expecting {expectedRule}, but received {ruleName}.");
        }

        var propertyName = property.Name;
        var ruleArguments = property.RuleArguments;
        var driftTimer = ruleArguments![0].Contains("@TimerDrift");

        var readTimer = driftTimer ? "reader.ReadDeltaTime()" : "reader.ReadDateTime()";
        var useVar = isMigration ? "" : "var ";
        source.AppendLine($"{indent}{useVar}{propertyName}Next = {readTimer};");
        source.AppendLine($"{indent}{useVar}{propertyName}Delay = {propertyName}Next == System.DateTime.MinValue ? System.TimeSpan.MinValue : {propertyName}Next - Core.Now;");
    }

    public override void GenerateSerializationMethod(StringBuilder source, string indent, SerializableProperty property)
    {
        var expectedRule = RuleName;
        var ruleName = property.Rule;
        if (expectedRule != ruleName)
        {
            throw new ArgumentException($"Invalid rule applied to property {ruleName}. Expecting {expectedRule}, but received {ruleName}.");
        }

        var propertyName = property.Name;
        var ruleArguments = property.RuleArguments;
        var driftTimer = ruleArguments![0].Contains("@TimerDrift");

        var writerMethod = driftTimer ? "WriteDeltaTime" : "Write";
        source.AppendLine($"{indent}writer.{writerMethod}({propertyName}?.Next ?? System.DateTime.MinValue);");
    }

    public void PostDeserializeMethod(
        StringBuilder source, string indent, SerializableProperty property, Compilation compilation, INamedTypeSymbol classSymbol
    )
    {
        var deserializeTimerMethod = classSymbol
            .GetMembers()
            .OfType<IMethodSymbol>()
            .FirstOrDefault(
                m =>
                {
                    if (!m.ReturnsVoid || m.Parameters.Length != 1 || !m.Parameters[0].Type.IsTimeSpan(compilation))
                    {
                        return false;
                    }

                    return m.GetAttributes()
                        .FirstOrDefault(
                            attr =>
                            {
                                if (!SymbolEqualityComparer.Default.Equals(
                                        attr.AttributeClass,
                                        compilation.GetTypeByMetadataName(
                                            SymbolMetadata.DESERIALIZE_TIMER_FIELD_ATTRIBUTE
                                        )
                                    ))
                                {
                                    return false;
                                }

                                var order = (int)attr.ConstructorArguments[0].Value!;
                                return order == property.Order;
                            }
                        ) != null;
                }
            ) ?? throw new Exception("Serializing a timer requires a method with the DeserializeTimerField attribute to handle creating the timer itself.");

        source.AppendLine($"{indent}{deserializeTimerMethod.Name}({property.Name}Delay);");
    }
}
