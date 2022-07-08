using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class AquariumFishNet : SpecialFishingNet
    {
        [Constructible]
        public AquariumFishNet()
        {
            ItemID = 0xDC8;
        }

        public override int LabelNumber => 1074463; // An aquarium fishing net

        public override bool RequireDeepWater => false;

        protected override void AddNetProperties(IPropertyList list)
        {
        }

        protected override void FinishEffect(Point3D p, Map map, Mobile from)
        {
            if (from.Skills.Fishing.Value < 10)
            {
                from.SendLocalizedMessage(1074487); // The creatures are too quick for you!
            }
            else
            {
                var fish = GiveFish(from);
                var bowl = Aquarium.GetEmptyBowl(from);

                if (bowl != null)
                {
                    fish.StopTimer();
                    bowl.AddItem(fish);
                    from.SendLocalizedMessage(1074489); // A live creature jumps into the fish bowl in your pack!
                    Delete();
                    return;
                }

                if (from.PlaceInBackpack(fish))
                {
                    from.PlaySound(0x5A2);
                    from.SendLocalizedMessage(
                        1074490
                    ); // A live creature flops around in your pack before running out of air.

                    fish.Kill();
                    Delete();
                    return;
                }

                fish.Delete();

                from.SendLocalizedMessage(1074488); // You could not hold the creature.
            }

            InUse = false;
            Movable = true;

            if (!from.PlaceInBackpack(this))
            {
                if (from.Map == null || from.Map == Map.Internal)
                {
                    Delete();
                }
                else
                {
                    MoveToWorld(from.Location, from.Map);
                }
            }
        }

        private BaseFish GiveFish(Mobile from)
        {
            var skill = from.Skills.Fishing.Value;

            if (skill / 100.0 >= Utility.RandomDouble())
            {
                var max = (int)skill / 5;

                if (max > 20)
                {
                    max = 20;
                }

                return Utility.Random(max) switch
                {
                    0  => new MinocBlueFish(),
                    1  => new Shrimp(),
                    2  => new FandancerFish(),
                    3  => new GoldenBroadtail(),
                    4  => new RedDartFish(),
                    5  => new AlbinoCourtesanFish(),
                    6  => new MakotoCourtesanFish(),
                    7  => new NujelmHoneyFish(),
                    8  => new Jellyfish(),
                    9  => new SpeckledCrab(),
                    10 => new LongClawCrab(),
                    11 => new AlbinoFrog(),
                    12 => new KillerFrog(),
                    13 => new VesperReefTiger(),
                    14 => new PurpleFrog(),
                    15 => new BritainCrownFish(),
                    16 => new YellowFinBluebelly(),
                    17 => new SpottedBuccaneer(),
                    18 => new SpinedScratcherFish(),
                    _  => new SmallMouthSuckerFin()
                };
            }

            return new MinocBlueFish();
        }
    }
}
