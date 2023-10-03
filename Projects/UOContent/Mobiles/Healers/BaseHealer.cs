using System;
using System.Collections.Generic;
using System.IO;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(2, false)]
public abstract partial class BaseHealer : BaseVendor
{
    private static readonly TimeSpan ResurrectDelay = TimeSpan.FromSeconds(2.0);

    private DateTime _nextResurrect;

    public BaseHealer()
    {
        if (!IsInvulnerable)
        {
            AI = AIType.AI_Mage;
            ActiveSpeed = 0.2;
            PassiveSpeed = 0.8;
            RangePerception = DefaultRangePerception;
            FightMode = FightMode.Aggressor;
        }

        SpeechHue = 0;

        SetStr(304, 400);
        SetDex(102, 150);
        SetInt(204, 300);

        SetDamage(10, 23);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 40, 50);
        SetResistance(ResistanceType.Fire, 40, 50);
        SetResistance(ResistanceType.Cold, 40, 50);
        SetResistance(ResistanceType.Poison, 40, 50);
        SetResistance(ResistanceType.Energy, 40, 50);

        SetSkill(SkillName.Anatomy, 75.0, 97.5);
        SetSkill(SkillName.EvalInt, 82.0, 100.0);
        SetSkill(SkillName.Healing, 75.0, 97.5);
        SetSkill(SkillName.Magery, 82.0, 100.0);
        SetSkill(SkillName.MagicResist, 82.0, 100.0);
        SetSkill(SkillName.Tactics, 82.0, 100.0);

        Fame = 1000;
        Karma = 10000;

        PackItem(new Bandage(Utility.RandomMinMax(5, 10)));
        PackItem(new HealPotion());
        PackItem(new CurePotion());
    }

    protected override List<SBInfo> SBInfos { get; } = new();

    public override bool IsActiveVendor => false;
    public override bool IsInvulnerable => false;

    public override VendorShoeType ShoeType => VendorShoeType.Sandals;

    public virtual bool HealsYoungPlayers => true;

    public override void InitSBInfo()
    {
    }

    public virtual int GetRobeColor() => Utility.RandomYellowHue();

    public override void InitOutfit()
    {
        base.InitOutfit();

        AddItem(new Robe(GetRobeColor()));
    }

    public virtual bool CheckResurrect(Mobile m) => true;

    public virtual void OfferResurrection(Mobile m)
    {
        Direction = GetDirectionTo(m);

        m.PlaySound(0x1F2);
        m.FixedEffect(0x376A, 10, 16);

        m.CloseGump<ResurrectGump>();
        m.SendGump(new ResurrectGump(m, ResurrectMessage.Healer));
    }

    public virtual void OfferHeal(PlayerMobile m)
    {
        Direction = GetDirectionTo(m);

        if (m.CheckYoungHealTime())
        {
            Say(501229); // You look like you need some healing my child.

            m.PlaySound(0x1F2);
            m.FixedEffect(0x376A, 9, 32);

            m.Hits = m.HitsMax;
        }
        else
        {
            Say(501228); // I can do no more for you at this time.
        }
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (!m.Frozen && Core.Now >= _nextResurrect && InRange(m, 4) && !InRange(oldLocation, 4) && InLOS(m))
        {
            if (!m.Alive)
            {
                _nextResurrect = Core.Now + ResurrectDelay;

                if (m.Map?.CanFit(m.Location, 16, false, false) != true)
                {
                    m.SendLocalizedMessage(502391); // Thou can not be resurrected there!
                }
                else if (CheckResurrect(m))
                {
                    OfferResurrection(m);
                }
            }
            else if (HealsYoungPlayers && m.Hits < m.HitsMax && m is PlayerMobile mobile && mobile.Young)
            {
                OfferHeal(mobile);
            }
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        // NOTE: This is to fix a previous RunUO serialization issue with this class:
        // This would be a breaking change if there is a derived class that is version 2 or higher
        // If that is the case, change the SerializationGenerator to a version higher than that before merging this change
        reader.Seek(-4, SeekOrigin.Current);
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (!IsInvulnerable)
        {
            AI = AIType.AI_Mage;
            ActiveSpeed = 0.2;
            PassiveSpeed = 0.8;
            RangePerception = DefaultRangePerception;
            FightMode = FightMode.Aggressor;
        }
    }
}
