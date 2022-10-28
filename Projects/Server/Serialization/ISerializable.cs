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

    void Deserialize(IGenericReader reader);
    void Serialize(IGenericWriter writer);

    // Determines if AfterSerialize should execute. This is checked on a worker thread.
    bool ShouldExecuteAfterSerialize { get; }

    // Executes after serialization if ShouldExecuteAfterSerialize is true. This is run on the game thread synchronously.
    void AfterSerialize();

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

    public void Serialize(ConcurrentQueue<Type> types)
    {
        SaveBuffer ??= new BufferWriter(true, types);

        // Queue for post serialization if this entity has it enabled
        // This will run AfterSerialize in the main game thread after the world is done saving
        if (ShouldExecuteAfterSerialize)
        {
            World.EnqueueAfterSerialization(this);
        }

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
