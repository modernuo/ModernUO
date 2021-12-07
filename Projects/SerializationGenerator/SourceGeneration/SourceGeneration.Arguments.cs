/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: SourceGeneration.Arguments.cs                                   *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace SerializationGenerator;

public static partial class SourceGeneration
{
    public static void GetTypesFromTypedConstant(TypedConstant arg, List<ITypeSymbol> list)
    {
        if (arg.Kind == TypedConstantKind.Type)
        {
            list.Add((ITypeSymbol)arg.Value);
        }
        else if (arg.Kind == TypedConstantKind.Array)
        {
            for (var i = 0; i < arg.Values.Length; i++)
            {
                GetTypesFromTypedConstant(arg.Values[i], list);
            }
        }
    }

    public static void GenerateSignatureArguments(this StringBuilder source, ImmutableArray<(ITypeSymbol, string)> parameters)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            var (t, v) = parameters[i];
            source.AppendFormat("{0} {1}", t.ToDisplayString(), v);
            if (i < parameters.Length - 1)
            {
                source.Append(", ");
            }
        }
    }

    public static void GenerateNamedArgument(this StringBuilder source, KeyValuePair<string, TypedConstant> namedArg)
    {
        source.AppendFormat("{0} = ", namedArg.Key);
        source.GenerateTypedConstant(namedArg.Value);
    }

    public static void GenerateTypedConstants(this StringBuilder source, ImmutableArray<TypedConstant> args)
    {
        source.Append("new []{");
        for (var i = 0; i < args.Length; i++)
        {
            source.GenerateTypedConstant(args[i]);
            if (i < args.Length - 1)
            {
                source.Append(", ");
            }
        }
        source.Append('}');
    }

    public static void GenerateTypedConstant(this StringBuilder source, TypedConstant arg)
    {
        if (arg.IsNull)
        {
            source.Append("null");
            return;
        }

        switch (arg.Kind)
        {
            default:
                {
                    return;
                }
            case TypedConstantKind.Primitive:
                {

                    if (arg.Value is string str)
                    {
                        source.AppendFormat("\"{0}\"", str);
                    }
                    else
                    {
                        source.Append(arg.Value);
                    }
                    break;
                }
            case TypedConstantKind.Enum:
                {
                    if (arg.Type == null || arg.Value == null)
                    {
                        source.Append("null");
                    }
                    else
                    {
                        source.AppendFormat("({0}){1}", arg.Type.ToDisplayString(), arg.Value);
                    }
                    break;
                }
            case TypedConstantKind.Type:
                {
                    source.AppendFormat("typeof({0})", ((ITypeSymbol)arg.Value)?.Name);
                    break;
                }
            case TypedConstantKind.Array:
                {
                    source.GenerateTypedConstants(arg.Values);
                    break;
                }
        }
    }
}