/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializedTypeResolver.cs                                       *
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
using System.Collections.Generic;
using Server.Logging;

namespace Server;

internal static class SerializedTypeResolver
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(SerializedTypeResolver));

    /// <summary>
    /// Resolves a serialized type hash, falling back to the legacy types database for hashes of
    /// types that were renamed. Logs and returns null when the type cannot be found.
    /// </summary>
    public static Type Resolve(ulong hash, Dictionary<ulong, string> typesDb, long position)
    {
        var t = AssemblyHandler.FindTypeByHash(hash);

        if (t != null)
        {
            return t;
        }

        if (typesDb == null)
        {
            logger.Error(
                new Exception($"The file SerializedTypes.db was not loaded. Type hash '{hash}' could not be found."),
                "Invalid {Hash} at position {Position}",
                hash,
                position
            );

            return null;
        }

        if (!typesDb.TryGetValue(hash, out var typeName))
        {
            logger.Error(
                new Exception($"Type hash '{hash}' is not present in the serialized types database."),
                "Invalid type hash {Hash} at position {Position}",
                hash,
                position
            );

            return null;
        }

        t = AssemblyHandler.FindTypeByFullName(typeName, false);

        if (t == null)
        {
            logger.Error(
                new Exception($"Type '{typeName}' was not found."),
                "Type {Type} was not found.",
                typeName
            );
        }

        return t;
    }
}
