using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Items
{
    [Serializable(0)]
    public partial class TestItem : Item
    {
        [SerializableField(0)]
        private Dictionary<PlayerMobile, List<Item>> _playerItems;
    }
}
