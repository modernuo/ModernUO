using ModernUO.Serialization;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class KeeperOfChivalry : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public KeeperOfChivalry() : base("the Keeper of Chivalry")
        {
            SetSkill(SkillName.Fencing, 75.0, 85.0);
            SetSkill(SkillName.Macing, 75.0, 85.0);
            SetSkill(SkillName.Swords, 75.0, 85.0);
            SetSkill(SkillName.Chivalry, 100.0);
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBKeeperOfChivalry());
        }

        public override void InitOutfit()
        {
            AddItem(new PlateArms());
            AddItem(new PlateChest());
            AddItem(new PlateGloves());
            AddItem(new StuddedGorget());
            AddItem(new PlateLegs());

            AddItem(
                Utility.Random(4) switch
                {
                    0 => new PlateHelm(),
                    1 => new NorseHelm(),
                    2 => new CloseHelm(),
                    _ => new Helmet() // 3
                }
            );

            AddItem(
                Utility.Random(3) switch
                {
                    0 => new BodySash(0x482),
                    1 => new Doublet(0x482),
                    _ => new Tunic(0x482) // 2
                }
            );

            AddItem(new Broadsword());

            AddItem(new MetalKiteShield { Hue = Utility.RandomNondyedHue() });

            AddItem(Utility.RandomBool() ? new Boots() : new ThighBoots());

            PackGold(100, 200);
        }
    }
}
