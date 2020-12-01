using System.Collections.Generic;

namespace Server.Mobiles
{
    public class Veterinarian : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Veterinarian() : base("the vet")
        {
            SetSkill(SkillName.AnimalLore, 85.0, 100.0);
            SetSkill(SkillName.Veterinary, 90.0, 100.0);
        }

        public Veterinarian(Serial serial) : base(serial)
        {
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBVeterinarian());
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
