/***************************************************************************
 *                                  Body.cs
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

using System;
using System.IO;

namespace Server
{
  public enum BodyType : byte
  {
    Empty,
    Monster,
    Sea,
    Animal,
    Human,
    Equipment
  }

  public struct Body
  {
    private static BodyType[] m_Types;

    static Body()
    {
      if (File.Exists("Data/bodyTable.cfg"))
      {
        using (StreamReader ip = new StreamReader("Data/bodyTable.cfg"))
        {
          m_Types = new BodyType[0x1000];

          string line;

          while ((line = ip.ReadLine()) != null)
          {
            if (line.Length == 0 || line.StartsWith("#"))
              continue;

            string[] split = line.Split('\t');

            if (int.TryParse(split[0], out int bodyID) && Enum.TryParse(split[1], true, out BodyType type) && bodyID >= 0 &&
                bodyID < m_Types.Length)
            {
              m_Types[bodyID] = type;
            }
            else
            {
              Console.WriteLine("Warning: Invalid bodyTable entry:");
              Console.WriteLine(line);
            }
          }
        }
      }
      else
      {
        Console.WriteLine("Warning: Data/bodyTable.cfg does not exist");

        m_Types = new BodyType[0];
      }
    }

    public Body(int bodyID)
    {
      BodyID = bodyID;
    }

    public BodyType Type
    {
      get
      {
        if (BodyID >= 0 && BodyID < m_Types.Length)
          return m_Types[BodyID];
        return BodyType.Empty;
      }
    }

    public bool IsHuman => BodyID >= 0
                           && BodyID < m_Types.Length
                           && m_Types[BodyID] == BodyType.Human
                           && BodyID != 402
                           && BodyID != 403
                           && BodyID != 607
                           && BodyID != 608
                           && BodyID != 694
                           && BodyID != 695
                           && BodyID != 970;

    public bool IsGargoyle => BodyID == 666
                              || BodyID == 667
                              || BodyID == 694
                              || BodyID == 695;

    public bool IsMale => BodyID == 183
                          || BodyID == 185
                          || BodyID == 400
                          || BodyID == 402
                          || BodyID == 605
                          || BodyID == 607
                          || BodyID == 666
                          || BodyID == 694
                          || BodyID == 750;

    public bool IsFemale => BodyID == 184
                            || BodyID == 186
                            || BodyID == 401
                            || BodyID == 403
                            || BodyID == 606
                            || BodyID == 608
                            || BodyID == 667
                            || BodyID == 695
                            || BodyID == 751;

    public bool IsGhost => BodyID == 402
                           || BodyID == 403
                           || BodyID == 607
                           || BodyID == 608
                           || BodyID == 694
                           || BodyID == 695
                           || BodyID == 970;

    public bool IsMonster => BodyID >= 0
                             && BodyID < m_Types.Length
                             && m_Types[BodyID] == BodyType.Monster;

    public bool IsAnimal => BodyID >= 0
                            && BodyID < m_Types.Length
                            && m_Types[BodyID] == BodyType.Animal;

    public bool IsEmpty => BodyID >= 0
                           && BodyID < m_Types.Length
                           && m_Types[BodyID] == BodyType.Empty;

    public bool IsSea => BodyID >= 0
                         && BodyID < m_Types.Length
                         && m_Types[BodyID] == BodyType.Sea;

    public bool IsEquipment => BodyID >= 0
                               && BodyID < m_Types.Length
                               && m_Types[BodyID] == BodyType.Equipment;

    public int BodyID{ get; }

    public static implicit operator int(Body a)
    {
      return a.BodyID;
    }

    public static implicit operator Body(int a)
    {
      return new Body(a);
    }

    public override string ToString()
    {
      return $"0x{BodyID:X}";
    }

    public override int GetHashCode()
    {
      return BodyID;
    }

    public override bool Equals(object o)
    {
      if (!(o is Body)) return false;

      return ((Body)o).BodyID == BodyID;
    }

    public static bool operator ==(Body l, Body r)
    {
      return l.BodyID == r.BodyID;
    }

    public static bool operator !=(Body l, Body r)
    {
      return l.BodyID != r.BodyID;
    }

    public static bool operator >(Body l, Body r)
    {
      return l.BodyID > r.BodyID;
    }

    public static bool operator >=(Body l, Body r)
    {
      return l.BodyID >= r.BodyID;
    }

    public static bool operator <(Body l, Body r)
    {
      return l.BodyID < r.BodyID;
    }

    public static bool operator <=(Body l, Body r)
    {
      return l.BodyID <= r.BodyID;
    }
  }
}