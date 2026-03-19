using Server.Items;
using Server.Targeting;

namespace Server.Engines.Craft.T2A;

public class T2ACraftToolTarget : Target
{
    private readonly BaseTool _tool;
    private readonly CraftSystem _system;

    public T2ACraftToolTarget(BaseTool tool, CraftSystem system) : base(2, false, TargetFlags.None)
    {
        _tool = tool;
        _system = system;
    }

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (targeted == _tool)
        {
            // Make Last: repeat last crafted item
            var context = _system.GetContext(from);
            var lastMade = context?.LastMade;

            if (lastMade != null)
            {
                var num = _system.CanCraft(from, _tool, lastMade.ItemType);

                if (num > 0)
                {
                    from.SendLocalizedMessage(num);
                    return;
                }

                var res = lastMade.UseSubRes2 ? _system.CraftSubRes2 : _system.CraftSubRes;
                var resIndex = lastMade.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
                var type = resIndex > -1 ? res[resIndex].ItemType : null;

                // Jewelry requires gem targeting — re-prompt instead of crafting directly
                if (typeof(BaseJewel).IsAssignableFrom(lastMade.ItemType))
                {
                    context.PendingGemType = GemType.None;
                    context.PendingGemCount = 0;
                    from.SendAsciiMessage("Target the gemstone you wish to use.");
                    from.Target = new TinkeringMenu.GemSelectTarget(from, _tool, lastMade.ItemType, type);
                    return;
                }

                if (context.LastHue >= 0)
                {
                    _system.CreateItem(from, lastMade.ItemType, type, _tool, lastMade, context.LastHue);
                }
                else
                {
                    _system.CreateItem(from, lastMade.ItemType, type, _tool, lastMade);
                }
            }
            else
            {
                from.SendAsciiMessage("You have not yet crafted anything.");
                T2ACraftSystem.ShowMenu(from, _system, _tool);
            }
        }
        else
        {
            // Normal flow: pass the targeted object through so resource selection
            // can use it directly instead of requiring a second target.
            T2ACraftSystem.ShowMenu(from, _system, _tool, targeted as Item);
        }
    }
}
