/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ISerializable.cs                                                *
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
using System.IO;

namespace Server;

public interface ISerializable : IGenericSerializable
{
    // Should be serialized/deserialized with the index so it can be referenced by IGenericReader
    DateTime Created { get; set; }

    long SavePosition { get; protected internal set; }
    BufferWriter SaveBuffer { get; protected internal set; }

    Serial Serial { get; }

    void Deserialize(IGenericReader reader);
    void Serialize(IGenericWriter writer);

    bool Deleted { get; }
    void Delete();

    public void InitializeSaveBuffer(byte[] buffer, ConcurrentQueue<Type> types)
    {
        SaveBuffer = new BufferWriter(buffer, true, types);
        if (World.DirtyTrackingEnabled)
        {
            SavePosition = SaveBuffer.Position;
        }
        else
        {
            SavePosition = -1;
        }
    }

    void IGenericSerializable.Serialize(ConcurrentQueue<Type> types)
    {
        SaveBuffer ??= new BufferWriter(true, types);

        // Clean, don't bother serializing
        if (SavePosition > -1)
        {
            SaveBuffer.Seek(SavePosition, SeekOrigin.Begin);
            return;
        }

        SaveBuffer.Seek(0, SeekOrigin.Begin);
        Serialize(SaveBuffer);

        if (World.DirtyTrackingEnabled)
        {
            SavePosition = SaveBuffer.Position;
        }
        else
        {
            this.MarkDirty();
        }
    }
}
