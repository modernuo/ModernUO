using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.ContextMenus;
using Server.Engines.ConPVP;
using Server.Engines.Harvest;

namespace Server.Items
{
    public interface IAxe
    {
        bool Axe(Mobile from, BaseAxe axe);
    }

    [SerializationGenerator(0, false)]
    public abstract partial class BaseAxe : BaseMeleeWeapon
    {
        [SerializableField(0)]
        [InvalidateProperties]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private bool _showUsesRemaining;

        [SerializableField(1)]
        [InvalidateProperties]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _usesRemaining;

        public BaseAxe(int itemID) : base(itemID) => _usesRemaining = 150;

        public override int DefHitSound => 0x232;
        public override int DefMissSound => 0x23A;

        public override SkillName DefSkill => SkillName.Swords;
        public override WeaponType DefType => WeaponType.Axe;
        public override WeaponAnimation DefAnimation => WeaponAnimation.Slash2H;

        public virtual HarvestSystem HarvestSystem => Lumberjacking.System;

        public virtual int GetUsesScalar()
        {
            if (Quality == WeaponQuality.Exceptional)
            {
                return 200;
            }

            return 100;
        }

        public override void UnscaleDurability()
        {
            base.UnscaleDurability();

            var scale = GetUsesScalar();

            UsesRemaining = (_usesRemaining * 100 + (scale - 1)) / scale;
            InvalidateProperties();
        }

        public override void ScaleDurability()
        {
            base.ScaleDurability();

            var scale = GetUsesScalar();

            UsesRemaining = (_usesRemaining * scale + 99) / 100;
            InvalidateProperties();
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (HarvestSystem == null || Deleted)
            {
                return;
            }

            var loc = GetWorldLocation();

            if (!from.InLOS(loc) || !from.InRange(loc, 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3E9, 1019045); // I can't reach that
                return;
            }

            if (!IsAccessibleTo(from))
            {
                PublicOverheadMessage(MessageType.Regular, 0x3E9, 1061637); // You are not allowed to access this.
                return;
            }

            if (HarvestSystem is not Mining)
            {
                from.SendLocalizedMessage(1010018); // What do you want to use this item on?
            }

            HarvestSystem.BeginHarvesting(from, this);
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (HarvestSystem != null)
            {
                BaseHarvestTool.AddContextMenuEntries(from, this, list, HarvestSystem);
            }
        }

        private void Deserialize(IGenericReader reader, int version)
        {
        }

        public override void OnHit(Mobile attacker, Mobile defender, double damageBonus = 1)
        {
            base.OnHit(attacker, defender, damageBonus);

            if (!Core.AOS && (attacker.Player || attacker.Body.IsHuman) && Layer == Layer.TwoHanded &&
                attacker.Skills.Anatomy.Value >= 80 &&
                attacker.Skills.Anatomy.Value / 400.0 >= Utility.RandomDouble() &&
                DuelContext.AllowSpecialAbility(attacker, "Concussion Blow", false))
            {
                var mod = defender.GetStatMod("Concussion");

                if (mod == null)
                {
                    defender.SendMessage("You receive a concussion blow!");
                    defender.AddStatMod(
                        new StatMod(
                            StatType.Int,
                            "Concussion",
                            -(defender.RawInt / 2),
                            TimeSpan.FromSeconds(30.0)
                        )
                    );

                    attacker.SendMessage("You deliver a concussion blow!");
                    attacker.PlaySound(0x308);
                }
            }
        }
    }
}
