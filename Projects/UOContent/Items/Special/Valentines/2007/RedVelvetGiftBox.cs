/*
 * Simply add this box with param true to create the entire valentine's 2007 package.
 * Adding it with no params or false will create an empty box.
 */

using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class RedVelvetGiftBox : BaseContainer
{
    [Constructible]
    public RedVelvetGiftBox(bool fill = false) : base(0xE7A)
    {
        Hue = 0x20;

        if (fill)
        {
            for (var i = 0; i < 5; i++)
            {
                AddToBox(new ValentinesCardSouth(), new Point3D(60 + i * 10, 47, 0));
                AddToBox(new ValentinesCardEast(), new Point3D(20 + i * 10, 72, 0));
            }

            AddToBox(new Bacon(), new Point3D(90, 85, 0));
            AddToBox(new RoseInAVase(), new Point3D(130, 55, 0));
        }
    }

    public override int DefaultGumpID => 0x3f;
    public override int LabelNumber => 1077596; // A Red Velvet Box

    public void AddToBox(Item item, Point3D loc)
    {
        DropItem(item);
        item.Location = loc;
    }
}
