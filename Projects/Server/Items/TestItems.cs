using System.Collections.Generic;

namespace Server.Items
{
    [Serializable(1)]
    public partial class TestItem1 : Item
    {
        [SerializableField(1)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.Administrator)]")]
        private List<Item> _someProperty;

        private void MigrateFrom(V0Content content)
        {
            _someProperty = new List<Item>
            {
                content.SomeProperty
            };
        }
    }
}
