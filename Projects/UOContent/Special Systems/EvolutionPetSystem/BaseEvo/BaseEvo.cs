using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Gumps;
using Server.Network;
using Server.Targeting;
using Xanthos.Interfaces;
using EvolutionPetSystem.Abilities;
using System.Collections.Generic;

namespace EvolutionPetSystem
{
    [CorpseName("an evolution creature corpse")]
    public abstract class BaseEvo : BaseCreature, IEvoCreature
    {
        private static double kOverLimitLossChance = 0.02;  // Chance that loyalty will be lost if over followers limit

        protected int m_Ep;
        protected int m_Stage;
        protected int m_FinalStage;
        protected int m_EpMinDivisor;
        protected int m_EpMaxDivisor;
        protected int m_DustMultiplier;
        protected int m_NextEpThreshold;
        protected bool m_CanAttackPlayers;
        protected bool m_AlwaysHappy;
        protected DateTime m_NextHappyTime;
        protected BaseAI m_ForcedAI;


        // Ability System
        protected int m_AbilityCount;
        protected List<BaseAbility> m_Abilities;

        public delegate void OnAlterMeleeDamageToHandler(Mobile to, int damage);

        public event OnAlterMeleeDamageToHandler OnAlterMeleeDamageToEvent;

        // Implement these 3 in your subclass to return BaseEvoSpec & BaseEvoEgg subclasses & Dust Type
        public abstract BaseEvoSpec GetEvoSpec();
        public abstract BaseEvoEgg GetEvoEgg();
        public abstract Type GetEvoDustType();

        // Implement these 2 in your subclass to control where exp points are accumulated
        public abstract bool AddPointsOnDamage { get; }
        public abstract bool AddPointsOnMelee { get; }


        [CommandProperty(AccessLevel.Administrator)]
        public int Ep
        {
            get { return m_Ep; }
            set { m_Ep = value; }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int Stage
        {
            get { return m_Stage; }
        }



        public BaseEvo(string name, AIType ai, double dActiveSpeed) : base(ai, FightMode.Closest, 10, 1, dActiveSpeed, 0.4)
        {
            Name = name;
            Init();
            InitAI();
        }

        public BaseEvo(Serial serial) : base(serial)
        {
            InitAI();
        }

        protected virtual void Init()
        {
            // Abililities
            m_AbilityCount = 0;
            m_Abilities = new List<BaseAbility>();

            //new RageAbility(this);

            BaseEvoSpec spec = GetEvoSpec();

            if (null != spec && null != spec.Stages)
            {
                m_Stage = -1;
                m_FinalStage = spec.Stages.Length - 1;
                Tamable = spec.Tamable;
                m_CanAttackPlayers = false;

                if (null != spec.Skills)
                {
                    double skillTotals = 0.0;

                    for (int i = 0; i < spec.Skills.Length; i++)
                    {
                        Skills[spec.Skills[i]].Cap = spec.MaxSkillValues[i];
                        skillTotals += spec.MaxSkillValues[i];
                        SetSkill(spec.Skills[i], (double)(spec.MinSkillValues[i]), (double)(spec.MaxSkillValues[i]));
                    }

                    if ((skillTotals *= 10) > SkillsCap)
                    {
                        SkillsCap = (int)skillTotals;
                    }
                }
                if (this is IEvoGuardian)
                {
                    // Go all the way
                    while (m_Stage < m_FinalStage)
                    {
                        m_Ep = m_NextEpThreshold;
                        Evolve(false);
                    }
                }
                else
                    Evolve(true);   // Evolve once as a new born
            }
        }

        protected override BaseAI ForcedAI { get { return m_ForcedAI; } }

        public List<BaseAbility> Abilities { get => m_Abilities; set => m_Abilities = value; }

        private void InitAI()
        {
            switch (AI)
            {
                case AIType.AI_Mage:
                    m_ForcedAI = new EvoMageAI(this, m_CanAttackPlayers);
                    break;
                default:
                    m_ForcedAI = null;
                    break;
            }
            ChangeAIType(AI);
        }

        // We don't need no stinking paragons
        public override void OnBeforeSpawn(Point3D location, Map m)
        {
            base.OnBeforeSpawn(location, m);
            Paragon.UnConvert(this);
        }

        public override void AlterMeleeDamageTo(Mobile to, ref int damage)
        {
            OnAlterMeleeDamageToEvent?.Invoke(to, damage);
            if (to is PlayerMobile)
            {
                if (this.Controlled)
                {
                    damage /= 4;
                }
            }
        }



        public override void OnThink()
        {
            base.OnThink();

            if (this is IEvoGuardian)
                return;

            else if (null != ControlMaster && ControlMaster.Followers > ControlMaster.FollowersMax && kOverLimitLossChance >= Utility.RandomDouble())
            {
                ControlMaster.SendMessage(Name + " is losing confidence in your ability to control so many creatures!");
                Say(1043270, Name); // * ~1_NAME~ looks around desperately *
                PlaySound(GetIdleSound());
                if (Loyalty > BaseCreature.MaxLoyalty / 10)
                    Loyalty--;
            }
            else if (m_AlwaysHappy && DateTime.Now >= m_NextHappyTime && null != ControlMaster && ControlMaster.Map == Map)
            {
                Loyalty = BaseCreature.MaxLoyalty;
                m_NextHappyTime = DateTime.Now + TimeSpan.FromMinutes(1.0);
            }
        }

        public override void Damage(int amount, Mobile from = null, bool informMount = true)
        {
            if (AddPointsOnDamage)
                AddPoints(from);
            base.Damage(amount, from, informMount);
        }

        public override void OnGaveMeleeAttack(Mobile defender)
        {


            if (AddPointsOnMelee)
                AddPoints(defender);

            base.OnGaveMeleeAttack(defender);
        }

        private void AddPoints(Mobile defender)
        {
            if (defender == null || defender.Deleted)
                return;

            else if (defender is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)defender;

                if (bc.Controlled != true)
                    m_Ep += Utility.RandomMinMax(5 + (bc.HitsMax) / m_EpMinDivisor, 5 + (bc.HitsMax) / m_EpMaxDivisor);

                if (m_Stage < m_FinalStage && m_Ep >= m_NextEpThreshold)
                {
                    Evolve(false);
                }
            }
        }

        protected virtual void Evolve(bool hatching)
        {
            BaseEvoSpec spec = GetEvoSpec();

            if (null != spec && null != spec.Stages)
            {
                BaseEvoStage stage = spec.Stages[++m_Stage];

                if (null != stage)
                {
                    int OldControlSlots = ControlSlots;

                    if (null != stage.Title) Title = stage.Title;
                    if (0 != stage.BaseSoundID) BaseSoundID = stage.BaseSoundID;
                    if (0 != stage.BodyValue) Body = stage.BodyValue;
                    if (0 != stage.VirtualArmor) VirtualArmor = stage.VirtualArmor;
                    if (0 != stage.ControlSlots) ControlSlots = stage.ControlSlots;
                    if (0 != stage.MinTameSkill) MinTameSkill = stage.MinTameSkill;
                    if (0 != stage.EpMinDivisor) m_EpMinDivisor = stage.EpMinDivisor;
                    if (0 != stage.EpMaxDivisor) m_EpMaxDivisor = stage.EpMaxDivisor;
                    if (0 != stage.DustMultiplier) m_DustMultiplier = stage.DustMultiplier;
                    m_NextEpThreshold = stage.NextEpThreshold;

                    SetStr(stage.StrMin, stage.StrMax);
                    SetDex(stage.DexMin, stage.DexMax);
                    SetInt(stage.IntMin, stage.IntMax);
                    SetDamage(stage.DamageMin, stage.DamageMax);

                    if (null != stage.ResistanceTypes)
                    {
                        for (int i = 0; i < stage.ResistanceTypes.Length; i++)
                            SetResistance(stage.ResistanceTypes[i], Utility.RandomMinMax(stage.MinResistances[i], stage.MaxResistances[i]));
                    }

                    Hue = GetEvoHue(spec, stage);

                    if (null != ControlMaster && stage.ControlSlots > 0 && ControlSlots > 0)
                        ControlMaster.Followers += stage.ControlSlots - OldControlSlots;

                    if (!(hatching || this is IEvoGuardian))
                    {
                        PlaySound(665);
                        Emote("*" + Name + " " + stage.EvolutionMessage + "*");
                    }
                    Warmode = false;
                }
            }
        }

        private int GetEvoHue(BaseEvoSpec spec, BaseEvoStage stage)
        {
            if (stage.Hue == 0)
                return Hue;

            return stage.Hue;
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            PlayerMobile player = from as PlayerMobile;

            if (this is IEvoGuardian)
                return base.OnDragDrop(from, dropped);

            if (null != ControlMaster && ControlMaster.Followers > ControlMaster.FollowersMax)
            {
                ControlMaster.SendMessage(Name + " is not interested in that now!");
                return false;
            }

            if (null != player && dropped.GetType() == GetEvoDustType())
            {
                BaseEvoDust dust = dropped as BaseEvoDust;

                if (null != dust)
                {
                    int amount = (dust.Amount * m_DustMultiplier);

                    m_Ep += amount;
                    PlaySound(665);
                    dust.Delete();
                    Emote("*" + this.Name + " absorbs the " + dust.Name + " gaining " + amount + " experience points*");
                    return true;
                }
                return false;
            }

            if (null != player && dropped.GetType() == typeof(MandrakeRoot))
            {
                BaseEvoDust dust = dropped as BaseEvoDust;

                if (null != dust)
                {
                    int amount = (dust.Amount * m_DustMultiplier);

                    m_Ep += amount;
                    PlaySound(665);
                    dust.Delete();
                    Emote("*" + this.Name + " absorbs the " + dust.Name + " gaining " + amount + " experience points*");
                    return true;
                }
                return false;
            }

            return base.OnDragDrop(from, dropped);
        }

        public override int GetMaxResistance(ResistanceType type)
        {
            if (this is IEvoGuardian)
                return base.GetMaxResistance(type);

            int resistance = base.GetMaxResistance(type);
            BaseEvoSpec spec = GetEvoSpec();
            return (spec == null ? resistance : resistance > spec.MaxEvoResistance ? spec.MaxEvoResistance : resistance);
        }

        public void LoadSpecValues()
        {
            BaseEvoSpec spec = GetEvoSpec();

            if (null != spec && null != spec.Stages)
            {
                BaseEvoStage stage = spec.Stages[m_Stage];
                if (null != stage)
                {
                    m_FinalStage = spec.Stages.Length - 1;
                    m_EpMinDivisor = stage.EpMinDivisor;
                    m_EpMaxDivisor = stage.EpMaxDivisor;
                    m_DustMultiplier = stage.DustMultiplier;
                    m_NextEpThreshold = stage.NextEpThreshold;
                    m_AlwaysHappy = spec.AlwaysHappy;
                    m_NextHappyTime = DateTime.Now;
                    m_CanAttackPlayers = spec.CanAttackPlayers;
                }
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
            writer.Write((int)m_Ep);
            writer.Write((int)m_Stage);

            // Ability System
            writer.Write((int)m_AbilityCount);
            if (m_Abilities != null)
            {
                foreach (var item in m_Abilities)
                {
                    item.Serialize(writer);
                }
            }
            



        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_Ep = reader.ReadInt();
            m_Stage = reader.ReadInt();

            // Ability System
            m_AbilityCount = reader.ReadInt();
            for (int i = 0; i < m_AbilityCount; i++)
            {
                m_Abilities[i].Deserialize(reader);
            }


            LoadSpecValues();
        }
    }
}
