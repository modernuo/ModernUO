using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Necromancy;
using Server.Spells.Ninjitsu;

namespace Server.Spells.Ninjitsu
{
    public class MirrorImage : NinjaSpell
    {
        private static readonly Dictionary<Mobile, int> m_CloneCount = new();

        private static readonly SpellInfo _info = new(
            "Mirror Image",
            null,
            -1,
            9002
        );

        public MirrorImage(Mobile caster, Item scroll) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.5);

        public override double RequiredSkill => Core.ML ? 20.0 : 40.0;
        public override int RequiredMana => 10;

        public override bool BlockedByAnimalForm => false;

        public static bool HasClone(Mobile m) => m_CloneCount.ContainsKey(m);

        public static void AddClone(Mobile m)
        {
            if (m == null)
            {
                return;
            }

            m_CloneCount[m] = 1 + (m_CloneCount.TryGetValue(m, out var count) ? count : 0);
        }

        public static void RemoveClone(Mobile m)
        {
            if (m == null || !m_CloneCount.TryGetValue(m, out var count))
            {
                return;
            }

            if (count <= 1)
            {
                m_CloneCount.Remove(m);
            }
            else
            {
                m_CloneCount[m]--;
            }
        }

        public override bool CheckCast()
        {
            if (Caster.Mounted)
            {
                Caster.SendLocalizedMessage(1063132); // You cannot use this ability while mounted.
                return false;
            }

            if (Caster.Followers + 1 > Caster.FollowersMax)
            {
                Caster.SendLocalizedMessage(
                    1063133
                ); // You cannot summon a mirror image because you have too many followers.
                return false;
            }

            if (TransformationSpellHelper.UnderTransformation(Caster, typeof(HorrificBeastSpell)))
            {
                Caster.SendLocalizedMessage(1061091); // You cannot cast that spell in this form.
                return false;
            }

            return base.CheckCast();
        }

        public override bool CheckDisturb(DisturbType type, bool firstCircle, bool resistable) => false;

        public override void OnBeginCast()
        {
            base.OnBeginCast();

            Caster.SendLocalizedMessage(1063134); // You begin to summon a mirror image of yourself.
        }

        public override void OnCast()
        {
            if (Caster.Mounted)
            {
                Caster.SendLocalizedMessage(1063132); // You cannot use this ability while mounted.
            }
            else if (Caster.Followers + 1 > Caster.FollowersMax)
            {
                Caster.SendLocalizedMessage(
                    1063133
                ); // You cannot summon a mirror image because you have too many followers.
            }
            else if (TransformationSpellHelper.UnderTransformation(Caster, typeof(HorrificBeastSpell)))
            {
                Caster.SendLocalizedMessage(1061091); // You cannot cast that spell in this form.
            }
            else if (CheckSequence())
            {
                Caster.FixedParticles(0x376A, 1, 14, 0x13B5, EffectLayer.Waist);
                Caster.PlaySound(0x511);

                new Clone(Caster).MoveToWorld(Caster.Location, Caster.Map);
            }

            FinishSequence();
        }
    }
}

namespace Server.Mobiles
{
    public class Clone : BaseCreature
    {
        private Mobile m_Caster;

        public Clone(Mobile caster) : base(AIType.AI_Melee, FightMode.None)
        {
            m_Caster = caster;

            Body = caster.Body;

            Hue = caster.Hue;
            Female = caster.Female;

            Name = caster.Name;
            NameHue = caster.NameHue;

            Title = caster.Title;
            Kills = caster.Kills;

            HairItemID = caster.HairItemID;
            HairHue = caster.HairHue;

            FacialHairItemID = caster.FacialHairItemID;
            FacialHairHue = caster.FacialHairHue;

            for (var i = 0; i < caster.Skills.Length; ++i)
            {
                Skills[i].Base = caster.Skills[i].Base;
                Skills[i].Cap = caster.Skills[i].Cap;
            }

            for (var i = 0; i < caster.Items.Count; i++)
            {
                AddItem(CloneItem(caster.Items[i]));
            }

            Warmode = true;

            Summoned = true;
            SummonMaster = caster;

            ControlOrder = OrderType.Follow;
            ControlTarget = caster;

            var duration = TimeSpan.FromSeconds(30 + caster.Skills.Ninjitsu.Fixed / 40);

            new UnsummonTimer(caster, this, duration).Start();
            SummonEnd = Core.Now + duration;

            MirrorImage.AddClone(m_Caster);
        }

        public Clone(Serial serial) : base(serial)
        {
        }

        protected override BaseAI ForcedAI => new CloneAI(this);

        public override bool DeleteCorpseOnDeath => true;

        public override bool IsDispellable => false;
        public override bool Commandable => false;

        public override bool IsHumanInTown() => false;

        private Item CloneItem(Item item)
        {
            var newItem = new Item(item.ItemID);
            newItem.Hue = item.Hue;
            newItem.Layer = item.Layer;

            return newItem;
        }

        public override void OnDamage(int amount, Mobile from, bool willKill)
        {
            Delete();
        }

        public override void OnDelete()
        {
            Effects.SendLocationParticles(
                EffectItem.Create(Location, Map, EffectItem.DefaultDuration),
                0x3728,
                10,
                15,
                5042
            );

            base.OnDelete();
        }

        public override void OnAfterDelete()
        {
            MirrorImage.RemoveClone(m_Caster);
            base.OnAfterDelete();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(m_Caster);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            m_Caster = reader.ReadEntity<Mobile>();

            MirrorImage.AddClone(m_Caster);
        }
    }
}

namespace Server.Mobiles
{
    public class CloneAI : BaseAI
    {
        public CloneAI(Clone m) : base(m) => m.CurrentSpeed = m.ActiveSpeed;

        public override bool CanDetectHidden => false;

        public override bool Think()
        {
            // Clones only follow their owners
            var master = m_Mobile.SummonMaster;

            if (master?.Map == m_Mobile.Map && master?.InRange(m_Mobile, m_Mobile.RangePerception) == true)
            {
                var iCurrDist = (int)m_Mobile.GetDistanceToSqrt(master);
                var bRun = iCurrDist > 5;

                WalkMobileRange(master, 2, bRun, 0, 1);
            }
            else
            {
                WalkRandom(2, 2, 1);
            }

            return true;
        }
    }
}
