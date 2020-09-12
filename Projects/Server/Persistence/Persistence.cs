/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
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
            Serialize(new FileInfo(path), serializer);
        }

        public static void Serialize(FileInfo file, Action<IGenericWriter> serializer)
        {
            file.Refresh();

            if (file.Directory?.Exists == false)
            {
                file.Directory.Create();
            }

            if (!file.Exists)
            {
                file.Create().Close();
            }

            file.Refresh();

            using var fs = file.OpenWrite();
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

        public static void Deserialize(string path, Action<IGenericReader> deserializer)
        {
            Deserialize(path, deserializer, true);
        }

        public static void Deserialize(FileInfo file, Action<IGenericReader> deserializer)
        {
            Deserialize(file, deserializer, true);
        }

        public static void Deserialize(string path, Action<IGenericReader> deserializer, bool ensure)
        {
            Deserialize(new FileInfo(path), deserializer, ensure);
        }

        public static void Deserialize(FileInfo file, Action<IGenericReader> deserializer, bool ensure)
        {
            file.Refresh();

            if (file.Directory?.Exists == false)
            {
                if (!ensure)
                {
                    throw new DirectoryNotFoundException();
                }

                file.Directory.Create();
            }

            if (!file.Exists)
            {
                if (!ensure)
                {
                    throw new FileNotFoundException
                    {
                        Source = file.FullName
                    };
                }

                file.Create().Close();
            }

            file.Refresh();

            using var fs = file.OpenRead();
            var reader = new BinaryFileReader(new BinaryReader(fs));

            try
            {
                deserializer(reader);
            }
            catch (EndOfStreamException eos)
            {
                if (file.Length > 0)
                {
                    Console.WriteLine("[Persistence]: {0}", eos);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[Persistence]: {0}", e);
            }
            finally
            {
                reader.Close();
            }
        }
    }
}
