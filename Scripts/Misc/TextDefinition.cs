using System.Globalization;
using Server.Gumps;
using Server.Network;

namespace Server
{
  [Parsable]
  public class TextDefinition
  {
    public TextDefinition() : this(0, null)
    {
    }

    public TextDefinition(int number) : this(number, null)
    {
    }

    public TextDefinition(string text) : this(0, text)
    {
    }

    public TextDefinition(int number, string text)
    {
      Number = number;
      String = text;
    }

    public int Number{ get; }

    public string String{ get; }

    public bool IsEmpty => Number <= 0 && String == null;

    public override string ToString()
    {
      if (Number > 0)
        return string.Concat("#", Number.ToString());
      if (String != null)
        return String;

      return "";
    }

    public string Format(bool propsGump)
    {
      if (Number > 0)
        return string.Format("{0} (0x{0:X})", Number);
      if (String != null)
        return $"\"{String}\"";

      return propsGump ? "-empty-" : "empty";
    }

    public string GetValue()
    {
      if (Number > 0)
        return Number.ToString();
      if (String != null)
        return String;

      return "";
    }

    public static void Serialize(GenericWriter writer, TextDefinition def)
    {
      if (def == null)
      {
        writer.WriteEncodedInt(3);
      }
      else if (def.Number > 0)
      {
        writer.WriteEncodedInt(1);
        writer.WriteEncodedInt(def.Number);
      }
      else if (def.String != null)
      {
        writer.WriteEncodedInt(2);
        writer.Write(def.String);
      }
      else
      {
        writer.WriteEncodedInt(0);
      }
    }

    public static TextDefinition Deserialize(GenericReader reader)
    {
      int type = reader.ReadEncodedInt();

      switch (type)
      {
        case 0: return new TextDefinition();
        case 1: return new TextDefinition(reader.ReadEncodedInt());
        case 2: return new TextDefinition(reader.ReadString());
      }

      return null;
    }

    public static void AddTo(ObjectPropertyList list, TextDefinition def)
    {
      if (def == null)
        return;

      if (def.Number > 0)
        list.Add(def.Number);
      else if (def.String != null)
        list.Add(def.String);
    }

    public static implicit operator TextDefinition(int v)
    {
      return new TextDefinition(v);
    }

    public static implicit operator TextDefinition(string s)
    {
      return new TextDefinition(s);
    }

    public static implicit operator int(TextDefinition m)
    {
      if (m == null)
        return 0;

      return m.Number;
    }

    public static implicit operator string(TextDefinition m)
    {
      return m?.String;
    }

    public static void AddHtmlText(Gump g, int x, int y, int width, int height, TextDefinition def, bool back,
      bool scroll, int numberColor, int stringColor)
    {
      if (def == null)
        return;

      if (def.Number > 0)
      {
        if (numberColor >= 0) // 5 bits per RGB component (15 bit RGB)
          g.AddHtmlLocalized(x, y, width, height, def.Number, numberColor, back, scroll);
        else
          g.AddHtmlLocalized(x, y, width, height, def.Number, back, scroll);
      }
      else if (def.String != null)
      {
        if (stringColor >= 0) // 8 bits per RGB component (24 bit RGB)
          g.AddHtml(x, y, width, height, $"<BASEFONT COLOR=#{stringColor:X6}>{def.String}</BASEFONT>", back,
            scroll);
        else
          g.AddHtml(x, y, width, height, def.String, back, scroll);
      }
    }

    public static void AddHtmlText(Gump g, int x, int y, int width, int height, TextDefinition def, bool back,
      bool scroll)
    {
      AddHtmlText(g, x, y, width, height, def, back, scroll, -1, -1);
    }

    public static void SendMessageTo(Mobile m, TextDefinition def)
    {
      if (def == null)
        return;

      if (def.Number > 0)
        m.SendLocalizedMessage(def.Number);
      else if (def.String != null)
        m.SendMessage(def.String);
    }

    public static void SendMessageTo(Mobile m, TextDefinition def, int hue)
    {
      if (def == null)
        return;

      if (def.Number > 0)
        m.SendLocalizedMessage(def.Number, "", hue);
      else if (def.String != null)
        m.SendMessage(hue, def.String);
    }

    public static void PublicOverheadMessage(Mobile m, MessageType messageType, int hue, TextDefinition def)
    {
      if (def == null)
        return;

      if (def.Number > 0)
        m.PublicOverheadMessage(messageType, hue, def.Number);
      else if (def.String != null)
        m.PublicOverheadMessage(messageType, hue, false, def.String);
    }

    public static TextDefinition Parse(string value)
    {
      if (value == null)
        return null;

      int i;
      bool isInteger;

      if (value.StartsWith("0x"))
        isInteger = int.TryParse(value.Substring(2), NumberStyles.HexNumber, null, out i);
      else
        isInteger = int.TryParse(value, out i);

      if (isInteger)
        return new TextDefinition(i);
      return new TextDefinition(value);
    }

    public static bool IsNullOrEmpty(TextDefinition def)
    {
      return def == null || def.IsEmpty;
    }
  }
}