/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
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

public abstract class GenericPersistence : Persistence, IGenericSerializable
{
    private long _initialSize = 1024 * 1024;
    private MemoryMapFileWriter _fileToSave;

    public string Name { get; }

    public GenericPersistence(string name, int priority) : base(priority) => Name = name;

    public override void Preserialize(string savePath, ConcurrentQueue<Type> types)
    {
        var path = Path.Combine(savePath, Name);
        var filePath = Path.Combine(path, $"{Name}.bin");
        PathUtility.EnsureDirectory(path);

        _fileToSave = new MemoryMapFileWriter(new FileStream(filePath, FileMode.Create), _initialSize, types);
    }

    public override void Serialize()
    {
        World.ResetRoundRobin();
        World.PushToCache((this, this));
    }

    public override void WriteSnapshot()
    {
        string folderPath = null;
        using (var fs = _fileToSave.FileStream)
        {
            if (fs.Position > _initialSize)
            {
                _initialSize = fs.Position;
            }

            _fileToSave.Dispose();
            if (_fileToSave.Position == 0)
            {
                folderPath = Path.GetDirectoryName(fs.Name);
            }
        }

        if (folderPath != null)
        {
            Directory.Delete(folderPath);
        }
    }

    public override void Serialize(IGenericSerializable e, int threadIndex) => Serialize(_fileToSave);

    public abstract void Serialize(IGenericWriter writer);

    public override void Deserialize(string savePath, Dictionary<ulong, string> typesDb) =>
        AdhocPersistence.Deserialize(Path.Combine(savePath, Name, $"{Name}.bin"), Deserialize);

    public abstract void Deserialize(IGenericReader reader);
}
