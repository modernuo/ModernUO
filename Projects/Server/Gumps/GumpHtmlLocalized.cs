/***************************************************************************
 *                            GumpHtmlLocalized.cs
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
using Server.Buffers;
using Server.Collections;

namespace Server.Gumps
{
  public enum GumpHtmlLocalizedType
  {
    Plain,
    Color,
    Args
  }

  public class GumpHtmlLocalized : GumpEntry
  {
    public GumpHtmlLocalized(int x, int y, int width, int height, int number,
      bool background = false, bool scrollbar = false)
    {
      X = x;
      Y = y;
      Width = width;
      Height = height;
      Number = number;
      Background = background;
      Scrollbar = scrollbar;

      Type = GumpHtmlLocalizedType.Plain;
    }

    public GumpHtmlLocalized(int x, int y, int width, int height, int number, int color,
      bool background = false, bool scrollbar = false)
    {
      X = x;
      Y = y;
      Width = width;
      Height = height;
      Number = number;
      Color = color;
      Background = background;
      Scrollbar = scrollbar;

      Type = GumpHtmlLocalizedType.Color;
    }

    public GumpHtmlLocalized(int x, int y, int width, int height, int number, string args, int color,
      bool background = false, bool scrollbar = false)
    {
      // Are multiple arguments unsupported? And what about non ASCII arguments?

      X = x;
      Y = y;
      Width = width;
      Height = height;
      Number = number;
      Args = args;
      Color = color;
      Background = background;
      Scrollbar = scrollbar;

      Type = GumpHtmlLocalizedType.Args;
    }

    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int Number { get; set; }

    public string Args { get; set; }

    public int Color { get; set; }

    public bool Background { get; set; }

    public bool Scrollbar { get; set; }

    public GumpHtmlLocalizedType Type { get; set; }

          Parent?.Invalidate();
        }
      }
    }

    public override string Compile(NetState ns)
    {
      return m_Type switch
      {
        GumpHtmlLocalizedType.Plain =>
        $"{{ xmfhtmlgump {m_X} {m_Y} {m_Width} {m_Height} {m_Number} {(m_Background ? 1 : 0)} {(m_Scrollbar ? 1 : 0)} }}",
        GumpHtmlLocalizedType.Color =>
        $"{{ xmfhtmlgumpcolor {m_X} {m_Y} {m_Width} {m_Height} {m_Number} {(m_Background ? 1 : 0)} {(m_Scrollbar ? 1 : 0)} {m_Color} }}",
        _ =>
        $"{{ xmfhtmltok {m_X} {m_Y} {m_Width} {m_Height} {(m_Background ? 1 : 0)} {(m_Scrollbar ? 1 : 0)} {m_Color} {m_Number} @{m_Args}@ }}"
      };
    }

    public override void AppendTo(NetState ns, IGumpWriter disp)
    {
      SpanWriter writer = new SpanWriter(buffer.GetSpan(90 + Args?.Length ?? 0));
      switch (Type)
      {
        case GumpHtmlLocalizedType.Plain:
        {
          writer.Write(m_LayoutNamePlain);
          writer.WriteAscii(X.ToString());
          writer.Write((byte)0x20); // ' '
          writer.WriteAscii(Y.ToString());
          writer.Write((byte)0x20); // ' '
          writer.WriteAscii(Width.ToString());
          writer.Write((byte)0x20); // ' '
          writer.WriteAscii(Height.ToString());
          writer.Write((byte)0x20); // ' '
          writer.WriteAscii(Number.ToString());
          writer.Write((byte)0x20); // ' '
          writer.WriteAscii(Background ? "1" : "0");
          writer.Write((byte)0x20); // ' '
          writer.WriteAscii(Scrollbar ? "1" : "0");

          break;
        }

        case GumpHtmlLocalizedType.Color:
        {
          writer.Write(m_LayoutNameColor);
          writer.WriteAscii(X.ToString());
          writer.Write((byte)0x20); // ' '
          writer.WriteAscii(Y.ToString());
          writer.Write((byte)0x20); // ' '
          writer.WriteAscii(Width.ToString());
          writer.Write((byte)0x20); // ' '
          writer.WriteAscii(Height.ToString());
          writer.Write((byte)0x20); // ' '
          writer.WriteAscii(Number.ToString());
          writer.Write((byte)0x20); // ' '
          writer.WriteAscii(Background ? "1" : "0");
          writer.Write((byte)0x20); // ' '
          writer.WriteAscii(Scrollbar ? "1" : "0");
          writer.Write((byte)0x20); // ' '
          writer.WriteAscii(Color.ToString());

          break;
        }

        case GumpHtmlLocalizedType.Args:
        {
          writer.Write(m_LayoutNameArgs);
          writer.WriteAscii(X.ToString());
          writer.Write((byte)0x20); // ' '
          writer.WriteAscii(Y.ToString());
          writer.Write((byte)0x20); // ' '
          writer.WriteAscii(Width.ToString());
          writer.Write((byte)0x20); // ' '
          writer.WriteAscii(Height.ToString());
          writer.Write((byte)0x20); // ' '
          writer.WriteAscii(Background ? "1" : "0");
          writer.Write((byte)0x20); // ' '
          writer.WriteAscii(Scrollbar ? "1" : "0");
          writer.Write((byte)0x20); // ' '
          writer.WriteAscii(Color.ToString());
          writer.Write((byte)0x20); // ' '
          writer.WriteAscii(Number.ToString());
          writer.Write((byte)0x20); // ' '
          writer.Write((byte)0x40); // '@'
          writer.WriteAscii(Args ?? "");
          writer.Write((byte)0x40); // '@'

          break;
        }
      }

      writer.Write((byte)0x20); // ' '
      writer.Write((byte)0x7D); // '}'
      buffer.Advance(writer.WrittenCount);
    }
  }
}
