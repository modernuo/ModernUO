using Server.Targeting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.CustomizableStore
{
    internal class StoreItemTarget : Target
    {
        private Mobile _vendor { get; set; }
        private List<StoreItem> _storeItems { get; set; }
        public StoreItemTarget()
            : base(2, false, TargetFlags.None)
        {

        }

        public StoreItemTarget(Mobile vendor, List<StoreItem> storeItems) : this()
        {
            _vendor = vendor;
            _storeItems = storeItems;
            
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            Item i = targeted as Item;

            if (_vendor == null || i == null)
            {
                return;
            }
                

            Item clone = ItemClone.Clone(i);

            if (clone != null)
            {
                _storeItems.Add(new StoreItem(clone, 1000, "Test", "Testdescription"));
            } //copy did not fail
                //from.SendGump(new EditItemGump(m_Vendor, new Reward(clone), null, false, true));
            else
                from.SendMessage("This item cannot be copied.");
        }


    }
}


