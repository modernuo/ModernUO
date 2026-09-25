/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
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

namespace Server;

public interface ISerializable : IGenericSerializable
{
    // Should be serialized/deserialized with the index that way it can be referenced by IGenericReader
    DateTime Created { get; set; }

    Serial Serial { get; }

    void Deserialize(IGenericReader reader);

    bool Deleted { get; }
    void Delete();

    /// <summary>
    /// True for entities that exist only at runtime and must not be written to a save.
    /// Decided by the save worker during the freeze.
    /// </summary>
    bool SkipSerialization => false;

    /// <summary>
    /// True when the entity may have changed since the bytes at <see cref="SavePlacement" />
    /// were written. Set through <see cref="ISerializableExtensions.MarkDirty" /> by every
    /// mutation of serialized state; cleared by a save worker when the entity is serialized.
    /// The default keeps an entity that stores no state always dirty, so it always serializes.
    /// </summary>
    bool SaveDirty
    {
        get => true;
        set
        {
        }
    }

    /// <summary>
    /// Where this entity's record sits in the last committed save file, packed by
    /// <see cref="Server.SavePlacement" />; <see cref="Server.SavePlacement.None" /> when it has
    /// none (new since the last save, loaded from disk, or too large to place). Written only by
    /// the snapshot writer during <see cref="WorldState.WritingSave" /> and read only during the
    /// freeze, which never overlaps a write.
    /// </summary>
    long SavePlacement
    {
        get => Server.SavePlacement.None;
        set
        {
        }
    }
}
