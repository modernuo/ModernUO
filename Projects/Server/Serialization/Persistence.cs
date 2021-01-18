/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: Persistence.cs                                                  *
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
    public static class Persistence
    {
        public static void Serialize(string path, Action<IGenericWriter> serializer)
        {
            var file = new FileInfo(path);

            file.Refresh();

            AssemblyHandler.EnsureDirectory(file.Directory?.Name);

            if (!file.Exists)
            {
                file.Create().Close();
            }

            file.Refresh();

            using var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Write, FileShare.None);
            var writer = new BinaryFileWriter(fs, true);

            try
            {
                serializer(writer);
            }
            finally
            {
                writer.Flush();
                writer.Close();
            }
        }

        public static void Deserialize(string path, Action<IGenericReader> deserializer, bool ensure = true)
        {
            AssemblyHandler.EnsureDirectory(Path.GetDirectoryName(path));

            if (!File.Exists(path))
            {
                if (ensure)
                {
                    new FileInfo(path).Create().Close();
                }

                return;
            }

            using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            // TODO: Support files larger than 2GB
            var buffer = GC.AllocateUninitializedArray<byte>((int)fs.Length);

            try
            {
                deserializer(new BufferReader(buffer));
            }
            catch (Exception e)
            {
                Console.WriteLine("[Persistence]: {0}", e);
            }
        }
    }
}
