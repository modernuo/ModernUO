/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: JsonConfig.cs - Created: 2020/05/02 - Updated: 2020/05/02       *
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

using System.IO;
using System.Text.Json;

namespace Server.Json
{
  public static class JsonConfig
  {
    public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
      ReadCommentHandling = JsonCommentHandling.Skip,
      WriteIndented = true,
      AllowTrailingCommas = true,
      IgnoreNullValues = true
    };

    public static T Deserialize<T>(string filePath, JsonSerializerOptions options = null)
    {
      if (!File.Exists(filePath)) return default;
      string text = File.ReadAllText(filePath, Utility.UTF8);
      return JsonSerializer.Deserialize<T>(text, options ?? Options);
    }

    public static void Serialize(string filePath, object value, JsonSerializerOptions options = null)
    {
      if (File.Exists(filePath)) File.Delete(filePath);

      File.WriteAllText(filePath, JsonSerializer.Serialize(value, options ?? Options));
    }
  }
}
