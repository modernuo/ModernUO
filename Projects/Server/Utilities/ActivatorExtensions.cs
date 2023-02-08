/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ActivatorExtensions.cs                                          *
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
using System.Reflection;

namespace Server.Utilities;

public static class ActivatorExtensions
{
    public static ConstructorInfo GetConstructor(
        this Type type,
        Predicate<ConstructorInfo> predicate = null,
        Type[] args = null
    ) => type.GetConstructor(predicate, args, out _);

    public static ConstructorInfo GetConstructor(
        this Type type,
        Predicate<ConstructorInfo> predicate,
        Type[] args,
        out int paramCount
    )
    {
        args ??= Array.Empty<Type>();
        var ctors = type.GetConstructors();

        try
        {
            for (int i = 0; i < ctors.Length; i++)
            {
                ConstructorInfo info = ctors[i];

                if (predicate?.Invoke(info) == false)
                {
                    continue;
                }

                var parameters = info.GetParameters();
                paramCount = parameters.Length;

                if (args.Length > parameters.Length)
                {
                    continue;
                }

                bool validated = true;

                // Check that all args match params
                for (var j = 0; j < parameters.Length; j++)
                {
                    ParameterInfo param = parameters[j];

                    // All extra parameters must be optional
                    if (j >= args.Length)
                    {
                        if (!param.IsOptional)
                        {
                            validated = false;
                            break;
                        }

                        continue;
                    }

                    var arg = args[j];
                    if (arg == null && param.ParameterType.IsValueType)
                    {
                        validated = false;
                        break;
                    }

                    if (arg != null && !param.ParameterType.IsAssignableFrom(arg))
                    {
                        validated = false;
                        break;
                    }
                }

                if (validated)
                {
                    return info;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        paramCount = 0;
        return null;
    }

    public static T CreateInstance<T>(
        this Type type,
        params object[] args
    ) where T : class => type.CreateInstance<T>(null, args);

    public static T CreateInstance<T>(
        this Type type,
        Predicate<ConstructorInfo> predicate,
        object[] args = null
    ) where T : class
    {
        var argLength = args?.Length ?? 0;

        var types = argLength > 0 ? new Type[argLength] : Array.Empty<Type>();
        for (int i = 0; i < types.Length; i++)
        {
            types[i] = args![i]?.GetType();
        }

        var ctor = type.GetConstructor(predicate, types, out var paramCount);
        if (ctor == null)
        {
            Console.WriteLine("There is no constructor for {0} that matches the given predicate.", type);
            return default;
        }

        object[] paramArgs;
        if (paramCount == 0)
        {
            paramArgs = Array.Empty<object>();
        }
        else if (argLength == paramCount)
        {
            paramArgs = args;
        }
        else
        {
            paramArgs = new object[paramCount];
            for (int i = 0; i < paramCount; i++)
            {
                paramArgs[i] = i < argLength ? args![i] : Type.Missing;
            }
        }

        return ctor.Invoke(paramArgs) as T;
    }
}
