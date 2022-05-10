using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(2, false)]
    public abstract partial class BaseHides : Item, ICommodity
    {
        [InvalidateProperties]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        [SerializableField(0)]
        private CraftResource _resource;

        public BaseHides(CraftResource resource, int amount = 1) : base(0x1079)
        {
            Stackable = true;
            Weight = 5.0;
            Amount = amount;
            Hue = CraftResources.GetHue(resource);

            _resource = resource;
        }

        public override int LabelNumber
        {
            get
            {
                if (_resource >= CraftResource.SpinedLeather && _resource <= CraftResource.BarbedLeather)
                {
                    return 1049687 + (_resource - CraftResource.SpinedLeather);
                }

                return 1047023;
            }
        }

        int ICommodity.DescriptionNumber => LabelNumber;
        bool ICommodity.IsDeedable => true;

        private void Deserialize(IGenericReader reader, int version)
        {
            switch (version)
            {
                case 1:
                    {
                        _resource = (CraftResource)reader.ReadInt();
                        break;
                    }
                case 0:
                    {
                        var info = new OreInfo(reader.ReadInt(), reader.ReadInt(), reader.ReadString());

                        _resource = CraftResources.GetFromOreInfo(info);
                        break;
                    }
            }
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            if (Amount > 1)
            {
                list.Add(1050039, "{0}\t#{1}", Amount, 1024216); // ~1_NUMBER~ ~2_ITEMNAME~
            }
            else
            {
                list.Add(1024216); // pile of hides
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (!CraftResources.IsStandard(_resource))
            {
                var num = CraftResources.GetLocalizationNumber(_resource);

                if (num > 0)
                {
                    list.Add(num);
                }
                else
                {
                    list.Add(CraftResources.GetName(_resource));
                }
            }
        }
    }

    [SerializationGenerator(0, false)]
    [Flippable(0x1079, 0x1078)]
    public partial class Hides : BaseHides, IScissorable
    {
        [Constructible]
        public Hides(int amount = 1) : base(CraftResource.RegularLeather, amount)
        {
        }

        public bool Scissor(Mobile from, Scissors scissors)
        {
            if (Deleted || !from.CanSee(this))
            {
                return false;
            }

            if (Core.AOS && !IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(502437); // Items you wish to cut must be in your backpack
                return false;
            }

            ScissorHelper(from, new Leather(), 1);

            return true;
        }
    }

    [SerializationGenerator(0, false)]
    [Flippable(0x1079, 0x1078)]
    public partial class SpinedHides : BaseHides, IScissorable
    {
        [Constructible]
        public SpinedHides(int amount = 1) : base(CraftResource.SpinedLeather, amount)
        {
        }

        public bool Scissor(Mobile from, Scissors scissors)
        {
            if (Deleted || !from.CanSee(this))
            {
                return false;
            }

            if (Core.AOS && !IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(502437); // Items you wish to cut must be in your backpack
                return false;
            }

            ScissorHelper(from, new SpinedLeather(), 1);

            return true;
        }
    }

    [SerializationGenerator(0, false)]
    [Flippable(0x1079, 0x1078)]
    public partial class HornedHides : BaseHides, IScissorable
    {
        [Constructible]
        public HornedHides(int amount = 1) : base(CraftResource.HornedLeather, amount)
        {
        }

        public bool Scissor(Mobile from, Scissors scissors)
        {
            if (Deleted || !from.CanSee(this))
            {
                return false;
            }

            if (Core.AOS && !IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(502437); // Items you wish to cut must be in your backpack
                return false;
            }

            ScissorHelper(from, new HornedLeather(), 1);

            return true;
        }
    }

    [SerializationGenerator(0, false)]
    [Flippable(0x1079, 0x1078)]
    public partial class BarbedHides : BaseHides, IScissorable
    {
        [Constructible]
        public BarbedHides(int amount = 1) : base(CraftResource.BarbedLeather, amount)
        {
        }

        public bool Scissor(Mobile from, Scissors scissors)
        {
            if (Deleted || !from.CanSee(this))
            {
                return false;
            }

            if (Core.AOS && !IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(502437); // Items you wish to cut must be in your backpack
                return false;
            }

            ScissorHelper(from, new BarbedLeather(), 1);

            return true;
        }
    }
}
