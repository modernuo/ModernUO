using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.Vendors
{
    public class FireBossPriest : BaseCreature
    {
        public FireBoss Owner { get; private set; }
        public FireBossPriest(FireBoss owner) : base(AIType.AI_Animal, FightMode.Closest, 10, 1, 2, 2)
        {
            Owner = owner;

            Body = 770;
            Hue = 1358;

            SetStr(1185);
            SetDex(255);
            SetInt(250);

            SetHits(725);

            SetDamage(25);

            Fame = 24000;
            Karma = -24000;

            VirtualArmor = 90;
        }
        public FireBossPriest(Serial serial) : base(serial)
        {
        }
        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(Owner);
        }
        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
            Owner = reader.ReadEntity<FireBoss>();
        }
    }
}
