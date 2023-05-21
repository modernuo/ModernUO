/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: EntityActivatorExtensions.cs                                    *
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

public static class EntityActivatorExtensions
{
    public static T CreateEntityInstance<T>(
        this Type type,
        params object[] args
    ) where T : IEntity => type.CreateEntityInstance<T>(null, args);

    public static T CreateEntityInstance<T>(
        this Type type,
        Predicate<ConstructorInfo> predicate,
        object[] args = null
    ) where T : IEntity
    {
        var entity = type.CreateInstance<IEntity>(predicate, args);

        if (entity is T t)
        {
            return t;
        }

        // Handles memory leaks by deleting the offending entity
        entity?.Delete();
        return default;
    }
}
