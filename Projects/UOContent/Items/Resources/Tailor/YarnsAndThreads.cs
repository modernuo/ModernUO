using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public abstract partial class BaseClothMaterial : Item, IDyable
    {
        public BaseClothMaterial(int itemID, int amount = 1) : base(itemID)
        {
            Stackable = true;
            Weight = 1.0;
            Amount = amount;
        }

        public bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted)
            {
                return false;
            }

            Hue = sender.DyedHue;

            return true;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(500366); // Select a loom to use that on.
                from.Target = new PickLoomTarget(this);
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }

        private class PickLoomTarget : Target
        {
            private readonly BaseClothMaterial m_Material;

            public PickLoomTarget(BaseClothMaterial material) : base(3, false, TargetFlags.None) => m_Material = material;

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Material.Deleted)
                {
                    return;
                }

                var loom = targeted as ILoom;

                if (loom == null && targeted is AddonComponent component)
                {
                    loom = component.Addon as ILoom;
                }

                if (loom != null)
                {
                    if (!m_Material.IsChildOf(from.Backpack))
                    {
                        from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                    }
                    else if (loom.Phase < 4)
                    {
                        m_Material.Consume();

                        if (targeted is Item item)
                        {
                            item.SendLocalizedMessageTo(from, 1010001 + loom.Phase++);
                        }
                    }
                    else
                    {
                        Item create = new BoltOfCloth();
                        create.Hue = m_Material.Hue;

                        m_Material.Consume();
                        loom.Phase = 0;
                        from.SendLocalizedMessage(500368); // You create some cloth and put it in your backpack.
                        from.AddToBackpack(create);
                    }
                }
                else
                {
                    from.SendLocalizedMessage(500367); // Try using that on a loom.
                }
            }
        }
    }

    [SerializationGenerator(0, false)]
    public partial class DarkYarn : BaseClothMaterial
    {
        [Constructible]
        public DarkYarn(int amount = 1) : base(0xE1D, amount)
        {
        }
    }

    [SerializationGenerator(0, false)]
    public partial class LightYarn : BaseClothMaterial
    {
        [Constructible]
        public LightYarn(int amount = 1) : base(0xE1E, amount)
        {
        }
    }

    [SerializationGenerator(0, false)]
    public partial class LightYarnUnraveled : BaseClothMaterial
    {
        [Constructible]
        public LightYarnUnraveled(int amount = 1) : base(0xE1F, amount)
        {
        }
    }

    [SerializationGenerator(0, false)]
    public partial class SpoolOfThread : BaseClothMaterial
    {
        [Constructible]
        public SpoolOfThread(int amount = 1) : base(0xFA0, amount)
        {
        }
    }
}
