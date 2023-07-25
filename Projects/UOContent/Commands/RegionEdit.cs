using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Gumps;
using Server.Regions;
using Microsoft.Extensions.DependencyInjection;
using static Server.DataModel;

namespace Server.Commands
{
    public static class RegionEdit 
    {
        public static void Initialize()
        {
            CommandSystem.Register("Region", AccessLevel.GameMaster, Region_OnCommand);
            RegisterRegion();
        }
        public static void RegisterRegion()
        {
            var RegionStore = DataStore.ServiceProvider.GetService<IRegionStore>();
            var regions = RegionStore.GetStore();

            foreach (var region in regions.Values)
            {
                if (region.Active)
                {
                    region.RegistredRegion = new CustomRegion(region.Name,
                    region.MapControl,
                    region.Priority,
                    region.Guarded,
                    region.MountsAllowed,
                    region.ResurrectionAllowed,
                    region.LogoutAllowed,
                    region.Housellowed,
                    region.CanMark,
                    region.TravelTo,
                    region.TravelFrom,
                    region.AttackAllowed,
                    region.CastAllowed,
                    region.ExcludedSpell(),
                    region.EnterMessage,
                    region.OutMessage,
                    region.MapBounds);

                    region.RegistredRegion.Register();
                }
            }
        }

        private static void Region_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendGump(new RegionView(e.Mobile));
        }
    }
}
