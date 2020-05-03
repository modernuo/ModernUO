/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: Point3dConverter.cs                                             *
 * Created: 2020/04/12 - Updated: 2020/05/02                             *
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

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Server.Json
{
  public class Point3dConverter : JsonConverter<Point3D>
  {
    public override Point3D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      if (reader.TokenType != JsonTokenType.StartArray)
        throw new JsonException("Point3d must be an array of x, y, z");

      var data = new int[3];
      var count = 0;

      while (true)
      {
        reader.Read();
        if (reader.TokenType == JsonTokenType.EndArray)
          break;

        if (reader.TokenType == JsonTokenType.Number)
        {
          if (count < 3)
            data[count] = reader.GetInt32();

          count++;
        }
      }

      if (count < 2 || count > 3)
        throw new JsonException("Point3d must be an array of x, y, z");

      return new Point3D(data[0], data[1], data[2]);
    }

    public override void Write(Utf8JsonWriter writer, Point3D value, JsonSerializerOptions options)
    {
      writer.WriteStartArray();
      writer.WriteNumberValue(value.X);
      writer.WriteNumberValue(value.Y);
      writer.WriteNumberValue(value.Z);
      writer.WriteEndArray();
    }
  }
}
