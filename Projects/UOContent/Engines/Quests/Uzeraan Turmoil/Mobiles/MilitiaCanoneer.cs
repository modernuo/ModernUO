using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Haven
{
    public class MilitiaCanoneer : BaseQuester
    {
        [Constructible]
        public MilitiaCanoneer() : base("the Militia Canoneer") => Active = true;

        public MilitiaCanoneer(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active { get; set; }

        public override void InitBody()
        {
            InitStats(100, 125, 25);

            Hue = Race.Human.RandomSkinHue();

            Female = false;
            Body = 0x190;
            Name = NameList.RandomName("male");
        }

        public override void InitOutfit()
        {
            Utility.AssignRandomHair(this);
            Utility.AssignRandomFacialHair(this, HairHue);

            AddItem(new PlateChest());
            AddItem(new PlateArms());
            AddItem(new PlateGloves());
            AddItem(new PlateLegs());

            var torch = new Torch();
            torch.Movable = false;
            AddItem(torch);
            torch.Ignite();
        }

        public override bool CanTalkTo(PlayerMobile to) => false;

        public override void OnTalk(PlayerMobile player, bool contextMenu)
        {
        }

        public override bool IsEnemy(Mobile m)
        {
            if (m.Player || m is BaseVendor)
            {
                return false;
            }

            if (m is BaseCreature bc)
            {
                var master = bc.GetMaster();
                if (master != null)
                {
                    return IsEnemy(master);
                }
            }

            return m.Karma < 0;
        }

        public bool WillFire(Cannon cannon, Mobile target)
        {
            if (Active && IsEnemy(target))
            {
                Direction = GetDirectionTo(target);
                Say(Utility.RandomList(500651, 1049098, 1049320, 1043149));
                return true;
            }

            return false;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(Active);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            Active = reader.ReadBool();
        }
    }
}
