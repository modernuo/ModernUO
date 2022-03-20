using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Regions;
using Microsoft.Extensions.DependencyInjection;
using static Server.DataModel;

namespace Server.Gumps
{
    public class RegionView : BaseViewGump<RegionModel>
    {
        private IRegionStore RegionStore;
        public override bool TwoButton => true;
        
        public override void OnLoad()
        {
            RegionStore = DataStore.ServiceProvider.GetService<IRegionStore>();
            Collection = RegionStore.GetStore()?.ToList();
        }
        public RegionView(Mobile owner, int page = 0) : base(owner, page) { }
       
        public override void OnAdd(Mobile m, string name)
        {
            if (name.Length < 2)
            {
                m.SendMessage("The name cannot be less than 2 characters");
                return;
            }
            if (RegionStore.Exist(name))
            {
                m.SendMessage("Name is already used");
                return;
            }

            RegionStore.Add(name, new RegionModel() { Name = name });
            ReOpen();
        }

        public override void OnPressTwoButton(Mobile m, int index)
        {
            if (Collection.ElementAt(index).Value is RegionModel region)
            {
                if (region.MapBounds.X > 0 && region.MapBounds.Y > 0)
                {
                    var xWall = 0x398C;
                    var yWall = 0x3996;
                    //x
                    for (int i = region.MapBounds.X; i < region.MapBounds.X + region.MapBounds.Width; i++)
                    {
                        new ShowRegionField(xWall, new Point3D(i, region.MapBounds.Y, 0), region.MapControl, TimeSpan.FromMinutes(2));
                        new ShowRegionField(xWall, new Point3D(i, region.MapBounds.Y + region.MapBounds.Height, 0), region.MapControl, TimeSpan.FromMinutes(2));
                    }
                    //y
                    for (int i = region.MapBounds.Y; i < region.MapBounds.Y + region.MapBounds.Height; i++)
                    {
                        new ShowRegionField(yWall, new Point3D(region.MapBounds.X, i, 0), region.MapControl, TimeSpan.FromMinutes(2));
                        new ShowRegionField(yWall, new Point3D(region.MapBounds.X + region.MapBounds.Width, i, 0), region.MapControl, TimeSpan.FromMinutes(2));
                    }
                }
            }

            ReOpen();
        }
        public override void OnPropertyEdited(Mobile m, object m_Object)
        {
            if(m_Object is RegionModel region)
            {
                RegionStore.Add(region.Name, region);
                if (region.Active)
                {

                    region.RegistredRegion?.Unregister();
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

                    region.RegistredRegion?.Register();

                }
                else
                {
                    region.RegistredRegion?.Unregister();
                    region.RegistredRegion = null;
                }
            }
            ReOpen();
        }
        public override void OnDelete(Mobile m, int index)
        {
            if (Collection.ElementAt(index).Value is RegionModel region)
            {
                region.RegistredRegion?.Unregister();
                RegionStore.Delete(region.Name);
            }

            ReOpen();
        }

        public override void OnEdit(Mobile m, int index)
        {
            if (Collection.ElementAt(index).Value is RegionModel region)
            {
                CaptureHandler();
                m.SendGump(new PropertiesGump(m, region));
            }
        }
    }
}
