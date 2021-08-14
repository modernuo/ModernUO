/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
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
using System.IO;

namespace Server
{
    public interface ISerializable
    {
        long SavePosition { get; protected internal set; }
        BufferWriter SaveBuffer { get; protected internal set; }
        int TypeRef { get; }
        Serial Serial { get; }
        void Deserialize(IGenericReader reader);
        void Serialize(IGenericWriter writer);
        void Delete();
        bool Deleted { get; }

        void SetTypeRef(Type type);

        public void InitializeSaveBuffer(byte[] buffer)
        {
            SaveBuffer = new BufferWriter(buffer, true);
            if (World.DirtyTrackingEnabled)
            {
                SavePosition = SaveBuffer.Position;
            }
            else
            {
                SavePosition = -1;
            }
        }

        public void Serialize()
        {
            SaveBuffer ??= new BufferWriter(true);

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
                SavePosition = -1;
            }
        }
    }
}
