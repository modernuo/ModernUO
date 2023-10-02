/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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
using System.Collections.Generic;
using System.IO;

namespace Server;

public abstract class GenericPersistence : Persistence
{
    private BufferWriter _saveBuffer;

    public string Name { get; }

    public GenericPersistence(string name, int priority) : base(priority) => Name = name;

    public override void Serialize()
    {
        _saveBuffer ??= new BufferWriter(true, World.SerializedTypes);
        _saveBuffer.Seek(0, SeekOrigin.Begin);

        Serialize(_saveBuffer);
    }

    public abstract void Serialize(IGenericWriter writer);

    public override void WriteSnapshot(string basePath)
    {
        string binPath = Path.Combine(basePath, Name, $"{Name}.bin");
        var buffer = _saveBuffer!.Buffer.AsSpan(0, (int)_saveBuffer.Position);
        AdhocPersistence.WriteSnapshot(new FileInfo(binPath), buffer);
    }

    public override void Deserialize(string savePath, Dictionary<ulong, string> typesDb) =>
        AdhocPersistence.Deserialize(Path.Combine(savePath, Name, $"{Name}.bin"), Deserialize);

    public abstract void Deserialize(IGenericReader reader);
}
