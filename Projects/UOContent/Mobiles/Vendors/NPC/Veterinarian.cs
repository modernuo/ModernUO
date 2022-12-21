using ModernUO.Serialization;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Veterinarian : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Veterinarian() : base("the vet")
        {
            SetSkill(SkillName.AnimalLore, 85.0, 100.0);
            SetSkill(SkillName.Veterinary, 90.0, 100.0);
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBVeterinarian());
        }
    }
}
