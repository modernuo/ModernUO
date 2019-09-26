/***************************************************************************
 *                                GumpEntry.cs
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

using System.Buffers;
using Server.Collections;

namespace Server.Gumps
{
  public abstract class GumpEntry
  {
    private Gump m_Parent;

    public Gump Parent
    {
      get => m_Parent;
      set
      {
        if (m_Parent != value)
        {
          m_Parent?.Remove(this);

          m_Parent = value;

          m_Parent?.Add(this);
        }
      }
    }

    public abstract string Compile(ArraySet<string> strings);
    public abstract void AppendTo(ArrayBufferWriter<byte> buffer, ArraySet<string> strings, ref int entries, ref int switches);
  }
}
