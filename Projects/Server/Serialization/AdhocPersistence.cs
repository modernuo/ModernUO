/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AdhocPersistence.cs                                             *
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
    public static class AdhocPersistence
    {
        public static void Serialize(string filePath, Action<IGenericWriter> serializer)
        {
            var fullPath = Path.Combine(Core.BaseDirectory, filePath);
            var file = new FileInfo(fullPath);
            file.Directory?.Create();

            using var bin = new BinaryFileWriter(fullPath, true);
            serializer(bin);
        }

        public static void Deserialize(string filePath, Action<IGenericReader> deserializer)
        {
            var fullPath = Path.Combine(Core.BaseDirectory, filePath);
            var file = new FileInfo(fullPath);
            file.Directory?.Create();

            if (!file.Exists)
            {
                return;
            }

            try
            {
                using FileStream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var br = new BinaryFileReader(fs);
                deserializer(br);
            }
            catch (Exception e)
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine($"***** Bad deserialize of {file.FullName} *****");
                Console.WriteLine(e.ToString());
                Utility.PopColor();
            }
        }
    }
}
