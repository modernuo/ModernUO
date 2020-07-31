/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: ActivatorUtil.cs - Created: 2020/02/19 - Updated: 2020/07/30    *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Linq;
using System.Reflection;

namespace Server.Utilities
{
  public static class ActivatorUtil
  {
    public static ConstructorInfo GetConstructor(Type type, Predicate<ConstructorInfo> predicate = null)
    {
      var emptyCtor = type.GetConstructor(Type.EmptyTypes);

      if (emptyCtor != null && predicate?.Invoke(emptyCtor) != false) return emptyCtor;

      var optionalCtor = type.GetConstructors().SingleOrDefault(info =>
        predicate?.Invoke(info) != false && info.GetParameters().All(x => x.IsOptional));

      if (optionalCtor != null) return optionalCtor;

      throw new TypeInitializationException(type.ToString(),
        new Exception($"There is no empty/default constructor for {type} that matches predicate."));
    }

    public static ConstructorInfo GetConstructor(Type type, Predicate<ConstructorInfo> predicate, params Type[] args)
    {
      try
      {
        ConstructorInfo ctor;

        if (args.All(x => x != null))
        {
          ctor = type.GetConstructor(args);

          if (ctor != null && predicate?.Invoke(ctor) != false) return ctor;
        }
        else
        {
          ctor = type.GetConstructors().SingleOrDefault(info =>
          {
            if (predicate?.Invoke(info) == false) return false;

            var paramList = info.GetParameters().ToList();

            // If more args are given than parameters, skip.
            if (args.Length > paramList.Count) return false;

            // check all given args map to params.
            for (var i = 0; i < args.Length; i++)
              // if a null reference is passed, but the type is not nullable
              if (args[i] == null && paramList[i].ParameterType.IsValueType
                  // or if an arg is not null and is not assignable to the parameter type, skip.
                  || !(args[i] == null || paramList[i].ParameterType.IsAssignableFrom(args[i])))
                return false;

            // If there are more parameters, check if they any are not optional, if any are not, skip.
            // Otherwise all checks have passed. We have found a match
            return args.Length <= paramList.Count || paramList.GetRange(args.Length, paramList.Count - args.Length)
              .All(x => x.IsOptional);
          });

          if (ctor != null) return ctor;
        }

        throw new Exception($"There is no empty/default constructor for {type} that matches predicate.");
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
        throw;
      }
    }

    public static object CreateInstance(Type type, Predicate<ConstructorInfo> constructorPredicate = null)
    {
      var cctor = GetConstructor(type, constructorPredicate);
      var args = cctor.GetParameters();

      if (args.Length == 0) return cctor.Invoke(Type.EmptyTypes);

      var argList = new object[args.Length];
      Array.Fill(argList, Type.Missing);
      return cctor.Invoke(argList);
    }

    public static object CreateInstance(Type type, Predicate<ConstructorInfo> constructorPredicate = null,
      params object[] args)
    {
      if (args == null || args.Length == 0) return CreateInstance(type, constructorPredicate);

      var cctor = GetConstructor(type, constructorPredicate, args.Select(x => x?.GetType()).ToArray());
      return cctor.Invoke(args);
    }

    public static object CreateInstance(Type type, params object[] args) => CreateInstance(type, null, args);

    public static T CreateInstance<T>(Predicate<ConstructorInfo> constructorPredicate = null) =>
      (T)CreateInstance(typeof(T), constructorPredicate);

    public static T CreateInstance<T>(params object[] args) => (T)CreateInstance(typeof(T), null, args);
  }
}
