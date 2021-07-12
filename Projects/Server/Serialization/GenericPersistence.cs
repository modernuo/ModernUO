/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
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
using System.IO;

namespace Server
{
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
                saveBuffer ??= new BufferWriter(true);
                saveBuffer.Seek(0, SeekOrigin.Begin);

                serializer(saveBuffer);
            }

            void WriterSnapshot(string savePath)
            {
                var path = Path.Combine(savePath, name);

                AssemblyHandler.EnsureDirectory(path);

                string binPath = Path.Combine(path, $"{name}.bin");
                using var bin = new BinaryFileWriter(binPath, true);

                saveBuffer!.Resize((int)saveBuffer.Position);
                bin.Write(saveBuffer.Buffer);
            }

            void Deserialize(string savePath)
            {
                var path = Path.Combine(savePath, name);

                AssemblyHandler.EnsureDirectory(path);

                string binPath = Path.Combine(path, $"{name}.bin");

                if (!File.Exists(binPath))
                {
                    return;
                }

                try
                {
                    using FileStream fs = new FileStream(binPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var br = new BinaryFileReader(fs);
                    deserializer(br);
                }
                catch (Exception e)
                {
                    Utility.PushColor(ConsoleColor.Red);
                    Console.WriteLine($"***** Bad deserialize of {name} *****");
                    Console.WriteLine(e.ToString());
                    Utility.PopColor();
                }
            }

            Persistence.Register(name, Serialize, WriterSnapshot, Deserialize, priority);
        }
    }
}
