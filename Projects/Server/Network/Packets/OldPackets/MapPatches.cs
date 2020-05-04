/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: MapPatches.cs - Created: 2020/05/03 - Updated: 2020/05/03       *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Network
{
  public sealed class MapPatches : Packet
  {
    // TODO: Base this on the client version and expansion
    public MapPatches() : base(0xBF)
    {
      EnsureCapacity(9 + 6 * 8);

      Stream.Write((short)0x0018);

      Stream.Write(6);

      Stream.Write(Map.Felucca.Tiles.Patch.StaticBlocks);
      Stream.Write(Map.Felucca.Tiles.Patch.LandBlocks);

      Stream.Write(Map.Trammel.Tiles.Patch.StaticBlocks);
      Stream.Write(Map.Trammel.Tiles.Patch.LandBlocks);

      Stream.Write(Map.Ilshenar.Tiles.Patch.StaticBlocks);
      Stream.Write(Map.Ilshenar.Tiles.Patch.LandBlocks);

      Stream.Write(Map.Malas.Tiles.Patch.StaticBlocks);
      Stream.Write(Map.Malas.Tiles.Patch.LandBlocks);

      Stream.Write(Map.Tokuno.Tiles.Patch.StaticBlocks);
      Stream.Write(Map.Tokuno.Tiles.Patch.LandBlocks);

      Stream.Write(Map.TerMur.Tiles.Patch.StaticBlocks);
      Stream.Write(Map.TerMur.Tiles.Patch.LandBlocks);
    }
  }
}
