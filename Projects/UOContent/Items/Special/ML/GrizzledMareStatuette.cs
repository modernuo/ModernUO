using System;
using Server.Mobiles;

namespace Server.Items
{
    public class GrizzledMareStatuette : BaseImprisonedMobile
    {
        [Constructible]
        public GrizzledMareStatuette() : base(0x2617) => Weight = 1.0;

        public GrizzledMareStatuette(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074475; // Grizzled Mare Statuette
        public override BaseCreature Summon => new GrizzledMare();

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

namespace Server.Mobiles
{
    public class GrizzledMare : HellSteed
    {
        private static readonly string m_Myname = "a grizzled mare";

        [Constructible]
        public GrizzledMare()
            : base(m_Myname)
        {
        }

        public GrizzledMare(Serial serial) : base(serial)
        {
        }

        public override bool DeleteOnRelease => true;

        public virtual void OnAfterDeserialize_Callback()
        {
            SetStats(this);

            Name = m_Myname;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version < 1)
            {
                Timer.StartTimer(TimeSpan.FromSeconds(0), OnAfterDeserialize_Callback);
            }
        }
    }
}
