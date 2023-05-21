using ModernUO.Serialization;
using System;
using Server.Items;
using System.Runtime.CompilerServices;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Sheep : BaseCreature, ICarvable
    {
        [Constructible]
        public Sheep() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 0xCF;
            BaseSoundID = 0xD6;

            SetStr(19);
            SetDex(25);
            SetInt(5);

            SetHits(12);
            SetMana(0);

            SetDamage(1, 2);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 5, 10);

            SetSkill(SkillName.MagicResist, 5.0);
            SetSkill(SkillName.Tactics, 6.0);
            SetSkill(SkillName.Wrestling, 5.0);

            Fame = 300;
            Karma = 0;

            VirtualArmor = 6;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 11.1;
        }

        public override string CorpseName => "a sheep corpse";

        [DeltaDateTime]
        [SerializableProperty(0)]
        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextWoolTime
        {
            get => _nextWoolTime;
            set
            {
                _nextWoolTime = value;
                SheepBody();
                this.MarkDirty();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SheepBody() => Body = Core.Now >= _nextWoolTime ? 0xCF : 0xDF;

        public override string DefaultName => "a sheep";

        public override int Meat => 3;
        public override MeatType MeatType => MeatType.LambLeg;
        public override FoodType FavoriteFood => FoodType.FruitsAndVegies | FoodType.GrainsAndHay;

        public override int Wool => Body == 0xCF ? 3 : 0;

        public void Carve(Mobile from, Item item)
        {
            if (Core.Now < _nextWoolTime)
            {
                // This sheep is not yet ready to be shorn.
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, 500449, from.NetState);
                return;
            }

            from.SendLocalizedMessage(500452); // You place the gathered wool into your backpack.
            from.AddToBackpack(new Wool(Map == Map.Felucca ? 2 : 1));

            NextWoolTime = Core.Now + TimeSpan.FromHours(3.0); // TODO: Proper time delay
        }

        public override void OnThink()
        {
            base.OnThink();
            SheepBody();
        }

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            SheepBody();
        }
    }
}
