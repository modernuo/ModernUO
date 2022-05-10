using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class GiantWeb1 : BaseAddon
    {
        [Constructible]
        public GiantWeb1()
        {
            var itemID = 4280;
            var count = 5;

            for (var i = 0; i < count; ++i)
            {
                AddComponent(
                    new AddonComponent(itemID++),
                    count - 1 - i,
                    -(count - 1 - i),
                    0
                );
            }
        }
    }

    [SerializationGenerator(0)]
    public partial class GiantWeb2 : BaseAddon
    {
        [Constructible]
        public GiantWeb2()
        {
            var itemID = 4285;
            var count = 5;

            for (var i = 0; i < count; ++i)
            {
                AddComponent(
                    new AddonComponent(itemID++),
                    i,
                    -i,
                    0
                );
            }
        }
    }

    [SerializationGenerator(0)]
    public partial class GiantWeb3 : BaseAddon
    {
        [Constructible]
        public GiantWeb3()
        {
            var itemID = 4290;
            var count = 4;

            for (var i = 0; i < count; ++i)
            {
                AddComponent(
                    new AddonComponent(itemID++),
                    i,
                    -i,
                    0
                );
            }
        }
    }

    [SerializationGenerator(0)]
    public partial class GiantWeb4 : BaseAddon
    {
        [Constructible]
        public GiantWeb4()
        {
            var itemID = 4294;
            var count = 4;

            for (var i = 0; i < count; ++i)
            {
                AddComponent(
                    new AddonComponent(itemID++),
                    count - 1 - i,
                    -(count - 1 - i),
                    0
                );
            }
        }
    }

    [SerializationGenerator(0)]
    public partial class GiantWeb5 : BaseAddon
    {
        [Constructible]
        public GiantWeb5()
        {
            var itemID = 4298;
            var count = 4;

            for (var i = 0; i < count; ++i)
            {
                AddComponent(
                    new AddonComponent(itemID++),
                    i,
                    -i,
                    0
                );
            }
        }
    }

    [SerializationGenerator(0)]
    public partial class GiantWeb6 : BaseAddon
    {
        [Constructible]
        public GiantWeb6()
        {
            var itemID = 4302;
            var count = 4;

            for (var i = 0; i < count; ++i)
            {
                AddComponent(
                    new AddonComponent(itemID++),
                    count - 1 - i,
                    -(count - 1 - i),
                    0
                );
            }
        }
    }
}
