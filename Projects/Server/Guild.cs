/***************************************************************************
 *                                 Guild.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System.Collections.Generic;
using System.Linq;

namespace Server.Guilds
{
  public enum GuildType
  {
    Regular,
    Chaos,
    Order
  }

  public abstract class BaseGuild : ISerializable
  {
    private readonly BufferWriter m_SaveBuffer;
    public BufferWriter SaveBuffer => m_SaveBuffer;

    private static Serial m_NextID = 1;

    protected BaseGuild(uint id) // serialization ctor
    {
      Serial = id;
      List.Add(Serial, this);
      if (Serial + 1 > m_NextID)
        m_NextID = Serial + 1;
      m_SaveBuffer = new BufferWriter(true);
    }

    protected BaseGuild()
    {
      Serial = m_NextID++;
      List.Add(Serial, this);
      m_SaveBuffer = new BufferWriter(true);
    }

    [CommandProperty(AccessLevel.Counselor)]
    public Serial Serial { get; }

    public abstract string Abbreviation { get; set; }
    public abstract string Name { get; set; }
    public abstract GuildType Type { get; set; }
    public abstract bool Disbanded { get; }

    public static Dictionary<uint, BaseGuild> List { get; } = new Dictionary<uint, BaseGuild>();

    public int TypeRef => 0;

    public void Serialize()
    {
      SaveBuffer.Flush();
      Serialize(SaveBuffer);
    }

    public abstract void Serialize(IGenericWriter writer);

    public abstract void Deserialize(IGenericReader reader);
    public abstract void OnDelete(Mobile mob);

    public static BaseGuild Find(uint id)
    {
      List.TryGetValue(id, out var g);

      return g;
    }

    public static BaseGuild FindByName(string name) => List.Values.FirstOrDefault(g => g.Name == name);

    public static BaseGuild FindByAbbrev(string abbr) => List.Values.FirstOrDefault(g => g.Abbreviation == abbr);

    public static List<BaseGuild> Search(string find)
    {
      var words = find.ToLower().Split(' ');
      var results = new List<BaseGuild>();

      foreach (var g in List.Values)
      {
        var name = g.Name.ToLower();

        if (words.All(t => name.IndexOf(t) != -1))
          results.Add(g);
      }

      return results;
    }

    public override string ToString() => $"0x{Serial:X} \"{Name} [{Abbreviation}]\"";
  }
}
