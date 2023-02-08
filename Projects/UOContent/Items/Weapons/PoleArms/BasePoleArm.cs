using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.ContextMenus;
using Server.Engines.ConPVP;
using Server.Engines.Harvest;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public abstract partial class BasePoleArm : BaseMeleeWeapon, IUsesRemaining
    {
        [SerializableField(0)]
        [InvalidateProperties]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private bool _showUsesRemaining;

        [SerializableField(1)]
        [InvalidateProperties]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _usesRemaining;

        public BasePoleArm(int itemID) : base(itemID) => _usesRemaining = 150;

        public override int DefHitSound => 0x237;
        public override int DefMissSound => 0x238;

        public override SkillName DefSkill => SkillName.Swords;
        public override WeaponType DefType => WeaponType.Polearm;
        public override WeaponAnimation DefAnimation => WeaponAnimation.Slash2H;

        public virtual HarvestSystem HarvestSystem => Lumberjacking.System;

        public override void OnDoubleClick(Mobile from)
        {
            if (HarvestSystem == null)
            {
                return;
            }

            if (IsChildOf(from.Backpack) || Parent == from)
            {
                HarvestSystem.BeginHarvesting(from, this);
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (HarvestSystem != null)
            {
                BaseHarvestTool.AddContextMenuEntries(from, this, list, HarvestSystem);
            }
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
                    attacker.PlaySound(0x11C);
                }
            }
        }
    }
}
