using System;
using System.Text.Json.Serialization;
using Server.Gumps;

namespace Server.Commands
{
  public class CAGObject : CAGNode
  {
    public CAGObject()
    {
    }

    [JsonPropertyName("type")]
    public Type Type { get; set; }

    [JsonPropertyName("gfx")]
    public int ItemID { get; set; }

    [JsonPropertyName("hue")]
    public int? Hue { get; set; }

    public CAGCategory Parent { get; set; }

    public override string Title => Type == null ? "bad type" : Type.Name;

    public override void OnClick(Mobile from, int page)
    {
      if (Type == null)
      {
        from.SendMessage("That is an invalid type name.");
      }
      else
      {
        CommandSystem.Handle(from, $"{CommandSystem.Prefix}Add {Type.Name}");

        from.SendGump(new CategorizedAddGump(from, Parent, page));
      }
    }
  }
}
