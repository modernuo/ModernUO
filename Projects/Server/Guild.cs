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

namespace Server
{
  public enum GuildType
  {
    Regular,
    Chaos,
    Order
  }

  public abstract class BaseGuild : ISerializable
  {
    private static uint m_NextID = 1;

    protected BaseGuild(uint Id) //serialization ctor
    {
      this.Id = Id;
      List.Add(this.Id, this);
      if (this.Id + 1 > m_NextID)
        m_NextID = this.Id + 1;
    }

    protected BaseGuild()
    {
      Id = m_NextID++;
      List.Add(Id, this);
    }

    [CommandProperty(AccessLevel.Counselor)]
    public uint Id{ get; }

    public abstract string Abbreviation{ get; set; }
    public abstract string Name{ get; set; }
    public abstract GuildType Type{ get; set; }
    public abstract bool Disbanded{ get; }

    public static Dictionary<uint, BaseGuild> List{ get; } = new Dictionary<uint, BaseGuild>();

    int ISerializable.TypeReference => 0;

    uint ISerializable.SerialIdentity => Id;
    public abstract void Serialize(GenericWriter writer);

    public abstract void Deserialize(GenericReader reader);
    public abstract void OnDelete(Mobile mob);

    public static BaseGuild Find(uint id)
    {
      List.TryGetValue(id, out BaseGuild g);

      return g;
    }

    public static BaseGuild FindByName(string name)
    {
      foreach (BaseGuild g in List.Values)
        if (g.Name == name)
          return g;

      return null;
    }

    public static BaseGuild FindByAbbrev(string abbr)
    {
      foreach (BaseGuild g in List.Values)
        if (g.Abbreviation == abbr)
          return g;

      return null;
    }

    public static List<BaseGuild> Search(string find)
    {
      string[] words = find.ToLower().Split(' ');
      List<BaseGuild> results = new List<BaseGuild>();

      foreach (BaseGuild g in List.Values)
      {
        bool match = true;
        string name = g.Name.ToLower();
        for (int i = 0; i < words.Length; i++)
          if (name.IndexOf(words[i]) == -1)
          {
            match = false;
            break;
          }

        if (match)
          results.Add(g);
      }

      return results;
    }

    public override string ToString()
    {
      return $"0x{Id:X} \"{Name} [{Abbreviation}]\"";
    }
  }
}