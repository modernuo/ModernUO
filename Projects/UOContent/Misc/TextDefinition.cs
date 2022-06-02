using Server.Gumps;
using Server.Network;

namespace Server
{
    [Parsable]
    public class TextDefinition
    {
        public TextDefinition(string text) : this(0, text)
        {
        }

        public TextDefinition(int number = 0, string text = null)
        {
            Number = number;
            String = text;
        }

        public int Number { get; }

        public string String { get; }

        public bool IsEmpty => Number <= 0 && String == null;

        public override string ToString() => Number > 0 ? $"#{Number}" : String ?? "";

        public string Format(bool propsGump) =>
            Number > 0 ? $"{Number} (0x{Number:X})" :
            String != null ? $"\"{String}\"" :
            propsGump ? "-empty-" : "empty";

        public string GetValue() => Number > 0 ? Number.ToString() : String ?? "";

        public static void Serialize(IGenericWriter writer, TextDefinition def)
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

        public static TextDefinition Deserialize(IGenericReader reader)
        {
            var type = reader.ReadEncodedInt();

            return type switch
            {
                0 => new TextDefinition(),
                1 => new TextDefinition(reader.ReadEncodedInt()),
                2 => new TextDefinition(reader.ReadString()),
                _ => null
            };
        }

        public static void AddTo(IPropertyList list, TextDefinition def)
        {
            if (def == null)
            {
                return;
            }

            if (def.Number > 0)
            {
                list.Add(def.Number);
            }
            else if (def.String != null)
            {
                list.Add(def.String);
            }
        }

        public static implicit operator TextDefinition(int v) => new(v);

        public static implicit operator TextDefinition(string s) => new(s);

        public static implicit operator int(TextDefinition m) => m?.Number ?? 0;

        public static implicit operator string(TextDefinition m) => m?.String;

        public static void AddHtmlText(
            Gump g, int x, int y, int width, int height, TextDefinition def, bool back,
            bool scroll, int numberColor, int stringColor
        )
        {
            if (def == null)
            {
                return;
            }

            if (def.Number > 0)
            {
                if (numberColor >= 0) // 5 bits per RGB component (15 bit RGB)
                {
                    g.AddHtmlLocalized(x, y, width, height, def.Number, numberColor, back, scroll);
                }
                else
                {
                    g.AddHtmlLocalized(x, y, width, height, def.Number, back, scroll);
                }
            }
            else if (def.String != null)
            {
                if (stringColor >= 0) // 8 bits per RGB component (24 bit RGB)
                {
                    g.AddHtml(
                        x,
                        y,
                        width,
                        height,
                        $"<BASEFONT COLOR=#{stringColor:X6}>{def.String}</BASEFONT>",
                        back,
                        scroll
                    );
                }
                else
                {
                    g.AddHtml(x, y, width, height, def.String, back, scroll);
                }
            }
        }

        public static void AddHtmlText(
            Gump g, int x, int y, int width, int height, TextDefinition def, bool back,
            bool scroll
        )
        {
            AddHtmlText(g, x, y, width, height, def, back, scroll, -1, -1);
        }

        public static void SendMessageTo(Mobile m, TextDefinition def)
        {
            if (def == null)
            {
                return;
            }

            if (def.Number > 0)
            {
                m.SendLocalizedMessage(def.Number);
            }
            else if (def.String != null)
            {
                m.SendMessage(def.String);
            }
        }

        public static void SendMessageTo(Mobile m, TextDefinition def, int hue)
        {
            if (def == null)
            {
                return;
            }

            if (def.Number > 0)
            {
                m.SendLocalizedMessage(def.Number, "", hue);
            }
            else if (def.String != null)
            {
                m.SendMessage(hue, def.String);
            }
        }

        public static void PublicOverheadMessage(Mobile m, MessageType messageType, int hue, TextDefinition def)
        {
            if (def == null)
            {
                return;
            }

            if (def.Number > 0)
            {
                m.PublicOverheadMessage(messageType, hue, def.Number);
            }
            else if (def.String != null)
            {
                m.PublicOverheadMessage(messageType, hue, false, def.String);
            }
        }

        public static TextDefinition Parse(string value)
        {
            if (value == null)
            {
                return null;
            }

            return Utility.ToInt32(value, out var i) ? new TextDefinition(i) : new TextDefinition(value);
        }

        public static bool IsNullOrEmpty(TextDefinition def) => def?.IsEmpty != false;
    }
}
