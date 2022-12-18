using ModernUO.Serialization;
using System;
using Server.Items;
using Server.Spells;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Changeling : BaseCreature
    {
        private static readonly int[] m_FireNorth =
        {
            -1, -1,
            1, -1,
            -1, 2,
            1, 2
        };

        private static readonly int[] m_FireEast =
        {
            -1, 0,
            2, 0
        };

        private DateTime m_LastMorph;
        private DateTime m_NextFireRing;

        [Constructible]
        public Changeling()
            : base(AIType.AI_Mage)
        {
            Body = 264;
            Hue = DefaultHue;

            SetStr(36, 105);
            SetDex(212, 262);
            SetInt(317, 399);

            SetHits(201, 211);
            SetStam(212, 262);
            SetMana(317, 399);

            SetDamage(9, 15);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 81, 90);
            SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 40, 49);
            SetResistance(ResistanceType.Poison, 40, 50);
            SetResistance(ResistanceType.Energy, 43, 50);

            SetSkill(SkillName.Wrestling, 10.4, 12.5);
            SetSkill(SkillName.Tactics, 101.1, 108.3);
            SetSkill(SkillName.MagicResist, 121.6, 132.2);
            SetSkill(SkillName.Magery, 91.6, 99.5);
            SetSkill(SkillName.EvalInt, 91.5, 98.8);
            SetSkill(SkillName.Meditation, 91.7, 98.5);

            Fame = 15000;
            Karma = -15000;

            PackScroll(1, 7);
            PackItem(new Arrow(35));
            PackItem(new Bolt(25));
            PackGem(2);

            PackArcaneScroll(0, 1);
        }

        public override string CorpseName => "a changeling corpse";
        public override string DefaultName => "a changeling";
        public virtual int DefaultHue => 0;

        public override bool ShowFameTitle => false;
        public override bool InitialInnocent => _morphedInto != null;

        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(0)]
        public Mobile MorphedInto
        {
            get => _morphedInto;
            set
            {
                if (value == this)
                {
                    value = null;
                }

                if (_morphedInto != value)
                {
                    Revert();

                    if (value != null)
                    {
                        Morph(value);
                        m_LastMorph = Core.Now;
                    }

                    _morphedInto = value;
                    Delta(MobileDelta.Noto);
                    this.MarkDirty();
                }
            }
        }

        public override void GenerateLoot()
        {
            AddLoot(LootPack.AosRich, 3);
        }

        public override int GetAngerSound() => 0x46E;

        public override int GetIdleSound() => 0x470;

        public override int GetAttackSound() => 0x46D;

        public override int GetHurtSound() => 0x471;

        public override int GetDeathSound() => 0x46F;

        public override void OnThink()
        {
            base.OnThink();

            if (Combatant != null)
            {
                if (m_NextFireRing <= Core.Now && Utility.RandomDouble() < 0.02)
                {
                    FireRing();
                    m_NextFireRing = Core.Now + TimeSpan.FromMinutes(2);
                }

                if (Combatant.Player && _morphedInto != Combatant && Utility.RandomDouble() < 0.05)
                {
                    MorphedInto = Combatant;
                }
            }
        }

        public override bool CheckIdle()
        {
            var idle = base.CheckIdle();

            if (idle && _morphedInto != null && Core.Now - m_LastMorph > TimeSpan.FromSeconds(30))
            {
                MorphedInto = null;
            }

            return idle;
        }

        private void FireEffects(int itemID, int[] offsets)
        {
            for (var i = 0; i < offsets.Length; i += 2)
            {
                var p = Location;

                p.X += offsets[i];
                p.Y += offsets[i + 1];

                if (SpellHelper.AdjustField(ref p, Map, 12, false))
                {
                    Effects.SendLocationEffect(p, Map, itemID, 50);
                }
            }
        }

        protected virtual void FireRing()
        {
            FireEffects(0x3E27, m_FireNorth);
            FireEffects(0x3E31, m_FireEast);
        }

        protected virtual void Morph(Mobile m)
        {
            Body = m.Body;
            Hue = m.Hue;
            Female = m.Female;
            Name = m.Name;
            NameHue = m.NameHue;
            Title = m.Title;
            Kills = m.Kills;
            HairItemID = m.HairItemID;
            HairHue = m.HairHue;
            FacialHairItemID = m.FacialHairItemID;
            FacialHairHue = m.FacialHairHue;

            // TODO: Skills?

            foreach (var item in m.Items)
            {
                if (item.Layer != Layer.Backpack && item.Layer != Layer.Mount && item.Layer != Layer.Bank)
                {
                    AddItem(new ClonedItem(item)); // TODO: Clone weapon/armor attributes
                }
            }

            PlaySound(0x511);
            FixedParticles(0x376A, 1, 14, 5045, EffectLayer.Waist);
        }

        protected virtual void Revert()
        {
            Body = 264;
            Hue = IsParagon && DefaultHue == 0 ? Paragon.Hue : DefaultHue;
            Female = false;
            Name = null;
            NameHue = -1;
            Title = null;
            Kills = 0;
            HairItemID = 0;
            HairHue = 0;
            FacialHairItemID = 0;
            FacialHairHue = 0;

            DeleteClonedItems();

            PlaySound(0x511);
            FixedParticles(0x376A, 1, 14, 5045, EffectLayer.Waist);
        }

        public void DeleteClonedItems()
        {
            for (var i = Items.Count - 1; i >= 0; --i)
            {
                var item = Items[i];

                if (item is ClonedItem)
                {
                    item.Delete();
                }
            }

            if (Backpack != null)
            {
                for (var i = Backpack.Items.Count - 1; i >= 0; --i)
                {
                    var item = Backpack.Items[i];

                    if (item is ClonedItem)
                    {
                        item.Delete();
                    }
                }
            }
        }

        public override void OnAfterDelete()
        {
            DeleteClonedItems();

            base.OnAfterDelete();
        }

        public override void ClearHands()
        {
        }

        [AfterDeserialization]
        private void AfterDeserialize()
        {
            if (_morphedInto != null)
            {
                ValidationQueue<Changeling>.Add(this);
            }
        }

        public void Validate()
        {
            Revert();
        }

        [SerializationGenerator(0, false)]
        public partial class ClonedItem : Item
        {
            public ClonedItem(Item item)
                : base(item.ItemID)
            {
                Name = item.Name;
                Weight = item.Weight;
                Hue = item.Hue;
                Layer = item.Layer;
                Movable = false;
            }
        }
    }
}
