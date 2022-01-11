using Server.Network;

namespace Server.Items
{
    public class EnchantedSextant : Item
    {
        private const double m_LongDistance = 300.0;

        private const double m_ShortDistance = 5.0;

        // TODO: Trammel/Haven
        private static readonly Point2D[] m_TrammelBanks =
        {
            new(652, 820),
            new(1813, 2825),
            new(3734, 2149),
            new(2503, 552),
            new(3764, 1317),
            new(587, 2146),
            new(1655, 1606),
            new(1425, 1690),
            new(4471, 1156),
            new(1317, 3773),
            new(2881, 684),
            new(2731, 2192),
            new(3620, 2617),
            new(2880, 3472),
            new(1897, 2684),
            new(5346, 74),
            new(5275, 3977),
            new(5669, 3131)
        };

        private static readonly Point2D[] m_FeluccaBanks =
        {
            new(652, 820),
            new(1813, 2825),
            new(3734, 2149),
            new(2503, 552),
            new(3764, 1317),
            new(3695, 2511),
            new(587, 2146),
            new(1655, 1606),
            new(1425, 1690),
            new(4471, 1156),
            new(1317, 3773),
            new(2881, 684),
            new(2731, 2192),
            new(2880, 3472),
            new(1897, 2684),
            new(5346, 74),
            new(5275, 3977),
            new(5669, 3131)
        };

        private static readonly Point2D[] m_IlshenarBanks =
        {
            new(854, 680),
            new(855, 603),
            new(1226, 554),
            new(1610, 556)
        };

        private static readonly Point2D[] m_MalasBanks =
        {
            new(996, 519),
            new(2048, 1345)
        };

        [Constructible]
        public EnchantedSextant() : base(0x1058) => Weight = 2.0;

        public EnchantedSextant(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1046226; // an enchanted sextant

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                return;
            }

            Point2D[] banks;
            PMList moongates;
            if (from.Map == Map.Trammel)
            {
                banks = m_TrammelBanks;
                moongates = PMList.Trammel;
            }
            else if (from.Map == Map.Felucca)
            {
                banks = m_FeluccaBanks;
                moongates = PMList.Felucca;
            }
            else if (from.Map == Map.Ilshenar)
            {
#if false
        banks = m_IlshenarBanks;
        moongates = PMList.Ilshenar;
#else
                // The magic of the sextant fails...
                from.NetState.SendMessageLocalized(Serial, ItemID, MessageType.Label, 0x482, 3, 1061684);

                return;
#endif
            }
            else if (from.Map == Map.Malas)
            {
                banks = m_MalasBanks;
                moongates = PMList.Malas;
            }
            else
            {
                banks = null;
                moongates = null;
            }

            var closestMoongate = Point3D.Zero;
            var moongateDistance = double.MaxValue;
            if (moongates != null)
            {
                foreach (var entry in moongates.Entries)
                {
                    var dist = from.GetDistanceToSqrt(entry.Location);
                    if (moongateDistance > dist)
                    {
                        closestMoongate = entry.Location;
                        moongateDistance = dist;
                    }
                }
            }

            var closestBank = Point2D.Zero;
            var bankDistance = double.MaxValue;
            if (banks != null)
            {
                foreach (var p in banks)
                {
                    var dist = from.GetDistanceToSqrt(p);
                    if (bankDistance > dist)
                    {
                        closestBank = p;
                        bankDistance = dist;
                    }
                }
            }

            int moonMsg = moongateDistance switch
            {
                double.MaxValue   => 1048021, // The sextant fails to find a Moongate nearby.
                > m_LongDistance  => 1046449 + (int)from.GetDirectionTo(closestMoongate), // A moongate is * from here
                > m_ShortDistance => 1048010 + (int)from.GetDirectionTo(closestMoongate), // There is a Moongate * of here.
                _                 => 1048018 // You are next to a Moongate at the moment.
            };

            from.NetState.SendMessageLocalized(Serial, ItemID, MessageType.Label, 0x482, 3, moonMsg);

            int bankMsg = bankDistance switch
            {
                double.MaxValue   => 1048020, // The sextant fails to find a Bank nearby.
                > m_LongDistance  => 1046462 + (int)from.GetDirectionTo(closestBank), // A town is * from here
                > m_ShortDistance => 1048002 + (int)from.GetDirectionTo(closestBank), // There is a city Bank * of here.
                _                 => 1048019  // You are next to a Bank at the moment.
            };

            from.NetState.SendMessageLocalized(Serial, ItemID, MessageType.Label, 0x5AA, 3, bankMsg);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
