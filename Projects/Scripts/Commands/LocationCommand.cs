using Server.Commands.Generic;
using Server.Items;
using Server.Targeting;
using System.Globalization;
using System.Linq;

namespace Server.Commands
{
  public class LocationCommand : BaseCommand
  {
    public LocationCommand() : base()
    {
      AccessLevel = AccessLevel.Counselor;
      Commands = new[] { "Location", "Loc", "Pos" };
      ObjectTypes = ObjectTypes.All;
      Supports = CommandSupport.Single | CommandSupport.Multi;
      Usage = "Location [itemIds ...]";
      Description = "Retrieves the positional coordinates of a target. Displaying a message above the item, mobile, or environment. Environment targets will also have a graphic temporarily displayed on the tile. This graphic can be changed to the provided list of itemIds.";
    }

    public override void Execute(CommandEventArgs e, object obj)
    {
      int[] graphics = new[] { 0x17AF, 0X17B0, 0X17B1, 0X17B2 };
      if (e.Arguments.Length > 0)
        graphics = e.Arguments
          .Select(x =>
          {
            int result;
            if (x.StartsWith("0x"))
              return int.TryParse(x.Substring(2), NumberStyles.HexNumber, CultureInfo.CurrentCulture, out result) ? result : (int?)null;
            return int.TryParse(x, out result) ? result : (int?)null;
          })
          .Where(x => x.HasValue)
          .Select(x => x.Value)
          .ToArray();
      if (obj is IPoint3D point)
      {
        var label = $"(x:{point.X}, y:{point.Y}, z:{point.Z})";
        if (obj is LandTarget || obj is StaticTarget)
        {
          var item = EffectItem.Create(new Point3D(point), e.Mobile.Map, EffectItem.DefaultDuration);
          foreach (int graphic in graphics)
            Effects.SendLocationParticles(item, graphic, 10, 50, 2023);
          item.LabelTo(e.Mobile, label);
        }
        else if (obj is Mobile entity) entity.SayTo(e.Mobile, label);
        else if (obj is Item item) item.LabelTo(e.Mobile, label);
        AddResponse($"Location: (x:{point.X}, y:{point.Y}, z:{point.Z})");
      }
      else
      {
        LogFailure("That is not locatable.");
      }
    }
  }
}
