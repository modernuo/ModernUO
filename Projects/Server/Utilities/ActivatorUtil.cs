/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ActivatorUtil.cs                                                *
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

namespace Server.Utilities
{
    public static class ActivatorUtil
    {
        public static ConstructorInfo GetConstructor<T>(
            Predicate<ConstructorInfo> predicate = null,
            Type[] args = null
        ) => GetConstructor(typeof(T), predicate, args);

        public static ConstructorInfo GetConstructor(
            this Type type,
            Predicate<ConstructorInfo> predicate = null,
            Type[] args = null
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

                    var paramList = info.GetParameters();

                    if (args.Length > paramList.Length)
                    {
                        continue;
                    }

                    bool validated = true;

                    // Check that all args match params
                    for (var j = 0; j < paramList.Length; j++)
                    {
                        ParameterInfo param = paramList[j];
                        if (j > args.Length && !param.IsOptional)
                        {
                            validated = false;
                            break;
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

            return null;
        }

        public static T CreateInstance<T>(
            params object[] args
        ) => (T)CreateInstance(typeof(T), null, args);

        public static T CreateInstance<T>(
            Predicate<ConstructorInfo> constructorPredicate = null,
            object[] args = null
        ) => (T)CreateInstance(typeof(T), constructorPredicate, args);

        public static object CreateInstance(
            this Type type,
            params object[] args
        ) => type.CreateInstance(null, args);

        public static object CreateInstance(
            this Type type,
            Predicate<ConstructorInfo> constructorPredicate,
            object[] args = null
        )
        {
            ConstructorInfo ctor;

            if (args == null || args.Length == 0)
            {
                ctor = type.GetConstructor(constructorPredicate);
                if (ctor == null)
                {
                    Console.WriteLine("There is no constructor for {0} that matches the given predicate.", type);
                    return null;
                }

                return ctor.Invoke(Array.Empty<object>());
            }

            var types = new Type[args.Length];
            for (int i = 0; i < types.Length; i++)
            {
                types[i] = args[i]?.GetType();
            }

            ctor = type.GetConstructor(constructorPredicate, types);
            if (ctor == null)
            {
                Console.WriteLine("There is no constructor for {0} that matches the given predicate.", type);
                return null;
            }

            return ctor.Invoke(args);
        }
    }
}
