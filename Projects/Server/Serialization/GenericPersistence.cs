/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GenericPersistence.cs                                           *
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Server;

public static class GenericPersistence
{
    public static void Register(
        string name,
        Action<IGenericWriter> serializer,
        Action<IGenericReader> deserializer,
        int priority = Persistence.DefaultPriority
    )
    {
        BufferWriter saveBuffer = null;

        void Serialize()
        {
            saveBuffer ??= new BufferWriter(true, World.SerializedTypes);
            saveBuffer.Seek(0, SeekOrigin.Begin);

            serializer(saveBuffer);
        }

        void WriteSnapshot(string savePath)
        {
            string binPath = Path.Combine(savePath, name, $"{name}.bin");
            var buffer = saveBuffer!.Buffer.AsSpan(0, (int)saveBuffer.Position);
            AdhocPersistence.WriteSnapshot(new FileInfo(binPath), buffer);
        }

        void Deserialize(string savePath, Dictionary<ulong, string> typesDb) =>
            AdhocPersistence.Deserialize(Path.Combine(savePath, name, $"{name}.bin"), deserializer);

        Persistence.Register(name, Serialize, WriteSnapshot, Deserialize, priority);
    }
}
