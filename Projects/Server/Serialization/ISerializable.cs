/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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

public interface ISerializable
{
    // Should be serialized/deserialized with the index so it can be referenced by IGenericReader
    DateTime Created { get; set; }

    // Should be serialized/deserialized with the index so it can be referenced by IGenericReader
    DateTime LastSerialized { get; protected internal set; }
    long SavePosition { get; protected internal set; }
    BufferWriter SaveBuffer { get; protected internal set; }

    Serial Serial { get; }

    // Executed on every entity, before it's serialized.
    // For example, this is used to clean up weak references and mark them dirty.
    void BeforeSerialize();
    void Deserialize(IGenericReader reader);
    void Serialize(IGenericWriter writer);
    void Delete();
    bool Deleted { get; }

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

    public void Serialize(ConcurrentQueue<Type> types)
    {
        SaveBuffer ??= new BufferWriter(true, types);

        BeforeSerialize();

        // Clean, don't bother serializing
        if (SavePosition > -1)
        {
            SaveBuffer.Seek(SavePosition, SeekOrigin.Begin);
            return;
        }

        LastSerialized = Core.Now;
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
