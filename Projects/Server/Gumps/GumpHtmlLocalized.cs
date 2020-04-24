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

using Server.Network;

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
    private static readonly byte[] m_LayoutNamePlain = Gump.StringToBuffer("xmfhtmlgump");
    private static readonly byte[] m_LayoutNameColor = Gump.StringToBuffer("xmfhtmlgumpcolor");
    private static readonly byte[] m_LayoutNameArgs = Gump.StringToBuffer("xmfhtmltok");

    private GumpHtmlLocalizedType m_Type;
    private int m_Width, m_Height;
    private int m_X, m_Y;

    public GumpHtmlLocalized(int x, int y, int width, int height, int number,
      bool background = false, bool scrollbar = false)
    {
      m_X = x;
      m_Y = y;
      m_Width = width;
      m_Height = height;
      Number = number;
      Background = background;
      Scrollbar = scrollbar;

      m_Type = GumpHtmlLocalizedType.Plain;
    }

    public GumpHtmlLocalized(int x, int y, int width, int height, int number, int color,
      bool background = false, bool scrollbar = false)
    {
      m_X = x;
      m_Y = y;
      m_Width = width;
      m_Height = height;
      Number = number;
      Color = color;
      Background = background;
      Scrollbar = scrollbar;

      m_Type = GumpHtmlLocalizedType.Color;
    }

    public GumpHtmlLocalized(int x, int y, int width, int height, int number, string args, int color,
      bool background = false, bool scrollbar = false)
    {
      // Are multiple arguments unsupported? And what about non ASCII arguments?

      m_X = x;
      m_Y = y;
      m_Width = width;
      m_Height = height;
      Number = number;
      Args = args;
      Color = color;
      Background = background;
      Scrollbar = scrollbar;

      m_Type = GumpHtmlLocalizedType.Args;
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

    public GumpHtmlLocalizedType Type
    {
      get => m_Type;
      set
      {
        if (m_Type != value)
        {
          m_Type = value;
        }
      }
    }

    public override string Compile(NetState ns)
    {
      return m_Type switch
      {
        GumpHtmlLocalizedType.Plain =>
        $"{{ xmfhtmlgump {X} {Y} {Width} {Height} {Number} {(Background ? 1 : 0)} {(Scrollbar ? 1 : 0)} }}",
        GumpHtmlLocalizedType.Color =>
        $"{{ xmfhtmlgumpcolor {X} {Y} {Width} {Height} {Number} {(Background ? 1 : 0)} {(Scrollbar ? 1 : 0)} {Color} }}",
        _ =>
        $"{{ xmfhtmltok {X} {Y} {Width} {Height} {(Background ? 1 : 0)} {(Scrollbar ? 1 : 0)} {Color} {Number} @{Args}@ }}"
      };
    }

    public override void AppendTo(NetState ns, IGumpWriter disp)
    {
      switch (m_Type)
      {
        case GumpHtmlLocalizedType.Plain:
          {
            disp.AppendLayout(m_LayoutNamePlain);

            disp.AppendLayout(m_X);
            disp.AppendLayout(m_Y);
            disp.AppendLayout(m_Width);
            disp.AppendLayout(m_Height);
            disp.AppendLayout(Number);
            disp.AppendLayout(Background);
            disp.AppendLayout(Scrollbar);

            break;
          }

        case GumpHtmlLocalizedType.Color:
          {
            disp.AppendLayout(m_LayoutNameColor);

            disp.AppendLayout(m_X);
            disp.AppendLayout(m_Y);
            disp.AppendLayout(m_Width);
            disp.AppendLayout(m_Height);
            disp.AppendLayout(Number);
            disp.AppendLayout(Background);
            disp.AppendLayout(Scrollbar);
            disp.AppendLayout(Color);

            break;
          }

        case GumpHtmlLocalizedType.Args:
          {
            disp.AppendLayout(m_LayoutNameArgs);

            disp.AppendLayout(m_X);
            disp.AppendLayout(m_Y);
            disp.AppendLayout(m_Width);
            disp.AppendLayout(m_Height);
            disp.AppendLayout(Background);
            disp.AppendLayout(Scrollbar);
            disp.AppendLayout(Color);
            disp.AppendLayout(Number);
            disp.AppendLayout(Args);

            break;
          }
      }
    }
  }
}
