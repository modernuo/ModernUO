using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Mobiles;
using Server.Network;
using Server.Spells;
using Server.Engines.Craft;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public abstract partial class BaseRanged : BaseMeleeWeapon
    {
        [SerializableField(0)]
        [InvalidateProperties]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private bool _balanced;

        [SerializableField(1)]
        [InvalidateProperties]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private int _velocity;

        private TimerExecutionToken _recoveryTimerToken;

        public BaseRanged(int itemID) : base(itemID)
        {
        }

        public abstract int EffectID { get; }
        public abstract Type AmmoType { get; }
        public abstract Item Ammo { get; }

        public override int DefHitSound => 0x234;
        public override int DefMissSound => 0x238;

        public override SkillName DefSkill => SkillName.Archery;
        public override WeaponType DefType => WeaponType.Ranged;
        public override WeaponAnimation DefAnimation => WeaponAnimation.ShootXBow;

        public override SkillName AccuracySkill => SkillName.Archery;

        public override TimeSpan OnSwing(Mobile attacker, Mobile defender, double damageBonus = 1.0)
        {
            // WeaponAbility a = WeaponAbility.GetCurrentAbility( attacker );

            // Make sure we've been standing still for .25/.5/1 second depending on Era
            if (Core.TickCount - attacker.LastMoveTime >= (Core.SE ? 250 : Core.AOS ? 500 : 1000) ||
                Core.AOS && WeaponAbility.GetCurrentAbility(attacker) is MovingShot)
            {
                var canSwing = true;

                if (Core.AOS)
                {
                    canSwing = !attacker.Paralyzed && !attacker.Frozen;

                    if (canSwing)
                    {
                        // On OSI you can swing + hold a cast at the same time
                        canSwing = attacker.Spell is not Spell { IsCasting: true, BlocksMovement: true };
                    }
                }

                if ((attacker as PlayerMobile)?.DuelContext?.CheckItemEquip(attacker, this) == false)
                {
                    canSwing = false;
                }

                if (canSwing && attacker.HarmfulCheck(defender))
                {
                    attacker.DisruptiveAction();
                    attacker.NetState.SendSwing(attacker.Serial, defender.Serial);

                    if (attacker is BaseCreature bc && bc.TriggerAbility(MonsterAbilityTrigger.CombatAction, defender))
                    {
                        return GetDelay(attacker);
                    }

                    if (OnFired(attacker, defender))
                    {
                        if (CheckHit(attacker, defender))
                        {
                            OnHit(attacker, defender);
                        }
                        else
                        {
                            OnMiss(attacker, defender);
                        }
                    }
                    else if (WeaponAbility.GetCurrentAbility(attacker) is LightningArrow lightningArrow)
                    {
                        // Lightning Arrow doesn't require ammunition
                        attacker.MovingEffect(defender, EffectID, 18, 1, false, false);
                        lightningArrow.OnHit(attacker, defender, 0, new WorldLocation(defender));
                    }
                }

                attacker.RevealingAction();

                return GetDelay(attacker);
            }

            attacker.RevealingAction();

            return TimeSpan.FromSeconds(0.25);
        }

        public override void OnHit(Mobile attacker, Mobile defender, double damageBonus = 1)
        {
            if (attacker.Player && !defender.Player && (defender.Body.IsAnimal || defender.Body.IsMonster) &&
                Utility.RandomDouble() < 0.4 && Ammo is { } ammo && !defender.AddToBackpack(ammo))
            {
                ammo.Delete();
            }

            if (Core.ML && _velocity > 0)
            {
                var bonus = (int)attacker.GetDistanceToSqrt(defender);

                if (bonus > 0 && _velocity > Utility.Random(100))
                {
                    AOS.Damage(defender, attacker, bonus * 3, 100, 0, 0, 0, 0);

                    if (attacker.Player)
                    {
                        attacker.SendLocalizedMessage(1072794); // Your arrow hits its mark with velocity!
                    }

                    if (defender.Player)
                    {
                        defender.SendLocalizedMessage(1072795); // You have been hit by an arrow with velocity!
                    }
                }
            }

            base.OnHit(attacker, defender, damageBonus);
        }

        public override void OnMiss(Mobile attacker, Mobile defender)
        {
            if (attacker.Player && Utility.RandomDouble() < 0.4)
            {
                if (Core.SE)
                {
                    if (attacker is PlayerMobile pm)
                    {
                        var ammo = AmmoType;

                        if (ammo != null)
                        {
                            pm.RecoverableAmmo ??= new Dictionary<Type, int>();
                            pm.RecoverableAmmo.TryGetValue(ammo, out var result);
                            pm.RecoverableAmmo[ammo] = result + 1;
                        }

                        if (!pm.Warmode)
                        {
                            if (!_recoveryTimerToken.Running)
                            {
                                Timer.StartTimer(TimeSpan.FromSeconds(10),
                                    () =>
                                    {
                                        _recoveryTimerToken.Cancel();
                                        pm.RecoverAmmo();
                                    },
                                    out _recoveryTimerToken
                                );
                            }
                        }
                    }
                }
                else
                {
                    Ammo?.MoveToWorld(
                        new Point3D(
                            defender.X + Utility.RandomMinMax(-1, 1),
                            defender.Y + Utility.RandomMinMax(-1, 1),
                            defender.Z
                        ),
                        defender.Map
                    );
                }
            }

            base.OnMiss(attacker, defender);
        }

        public virtual bool OnFired(Mobile attacker, Mobile defender)
        {
            if (attacker.Player)
            {
                var quiver = attacker.FindItemOnLayer<BaseQuiver>(Layer.Cloak);
                var pack = attacker.Backpack;

                if (quiver == null || Utility.Random(100) >= quiver.LowerAmmoCost)
                {
                    // consume ammo
                    if (quiver?.ConsumeTotal(AmmoType) == true)
                    {
                        quiver.InvalidateWeight();
                    }
                    else if (pack?.ConsumeTotal(AmmoType) != true)
                    {
                        return false;
                    }
                }
                else if (quiver.FindItemByType(AmmoType) == null && pack?.FindItemByType(AmmoType) == null)
                {
                    // lower ammo cost should not work when we have no ammo at all
                    return false;
                }
            }

            attacker.MovingEffect(defender, EffectID, 18, 1, false, false);
            return true;
        }

        public override int OnCraft(
            int quality,
            bool makersMark,
            Mobile from,
            CraftSystem craftSystem,
            Type typeRes,
            BaseTool tool,
            CraftItem craftItem,
            int resHue
        )
        {
            var result = base.OnCraft(quality, makersMark, from, craftSystem, typeRes, tool, craftItem, resHue);
            if (Core.ML)
            {
                ApplyWoodBonuses(typeRes);
            }

            return result;
        }

        private void ApplyWoodBonuses(Type resource)
        {
            var craftResource = CraftResources.GetFromType(resource);

            switch (craftResource)
            {
                case CraftResource.OakWood:
                {
                    Attributes.Luck += 40;
                    Attributes.WeaponDamage += 5;
                    break;
                }
                case CraftResource.AshWood:
                {
                    Attributes.WeaponSpeed += 10;
                    WeaponAttributes.LowerStatReq += 20;
                    break;
                }
                case CraftResource.YewWood:
                {
                    Attributes.AttackChance += 5;
                    Attributes.WeaponDamage += 10;
                    break;
                }
                case CraftResource.Bloodwood:
                {
                    Attributes.RegenHits += 2;
                    WeaponAttributes.HitLeechHits += 16;
                    break;
                }
                case CraftResource.Heartwood:
                {
                    ApplyHeartwoodBonus();
                    break;
                }
                case CraftResource.Frostwood:
                {
                    AosElementDamages.Physical = 60;
                    AosElementDamages.Cold = 40;
                    Attributes.WeaponDamage += 12;
                    break;
                }
            }
        }

        private void ApplyHeartwoodBonus()
        {
            switch (Utility.Random(6))
            {
                case 0:
                    {
                        Attributes.Luck += 40;
                        break;
                    }
                case 1:
                    {
                        WeaponAttributes.DurabilityBonus += 50;
                        break;
                    }
                case 2:
                    {
                        WeaponAttributes.LowerStatReq += 20;
                        break;
                    }
                case 3:
                    {
                        Attributes.WeaponSpeed += 10;
                        break;
                    }
                case 4:
                    {
                        Attributes.AttackChance += 5;
                        break;
                    }
                case 5:
                    {
                        WeaponAttributes.HitLeechHits += 10;
                        break;
                    }
            }
        }
    }
}
