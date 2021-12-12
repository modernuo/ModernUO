using Server.Mobiles;
using Server.Targeting;
using System.Collections.Generic;

namespace Server.CustomizableStore
{
    public class CustomizableStoreVendor : Banker
    {
        private List<StoreItem> _storeItems { get; set; } = new List<StoreItem>();

        [Constructible]
        public CustomizableStoreVendor(Serial serial) : base(serial)
        {
        }

        public CustomizableStoreVendor(IGenericReader reader)
        {
            Deserialize(reader);
        }

        public override void OnDoubleClick(Mobile from)
        {
            from.RevealingAction();
            from.SendMessage("Target the item you want to add to the shop.");
            from.Target = new StoreItemTarget(this, _storeItems);

            foreach (var item in _storeItems)
            {
                from.SendMessage(item.ToString());
            }
                       
            base.OnDoubleClick(from);
            
           
        }



        public void Serialize(IGenericWriter writer)
        {

            writer.Write(0); // version
            writer.Write(_storeItems.Count);
            foreach (var item in _storeItems)
            {
                item.Serialize(writer);
            }

        }
        public void Deserialize(IGenericReader reader)
        {
            int version = reader.ReadInt();
            var storeItemCount = reader.ReadInt();
            for (int i = 0; i < storeItemCount; i++)
            {
                _storeItems.Add(new StoreItem(reader));
            }


        }

        
    }

    
}
