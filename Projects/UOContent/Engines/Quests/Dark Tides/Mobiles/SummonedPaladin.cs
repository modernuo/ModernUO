using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Necro;

[SerializationGenerator(0, false)]
public partial class SummonedPaladin : BaseCreature
{
    [SerializableField(0)]
    private PlayerMobile _necromancer;

    [SerializableField(1)]
    private bool _toDelete;

    public SummonedPaladin(PlayerMobile necromancer) : base(AIType.AI_Melee, FightMode.Aggressor)
    {
        _necromancer = necromancer;

        InitStats(45, 30, 5);
        Title = "the Paladin";

        Hue = 0x83F3;

        Female = false;
        Body = 0x190;
        Name = NameList.RandomName("male");

        Utility.AssignRandomHair(this);
        Utility.AssignRandomFacialHair(this, false);

        FacialHairHue = HairHue;

        AddItem(new Boots(0x1));
        AddItem(new ChainChest());
        AddItem(new ChainLegs());
        AddItem(new RingmailArms());
        AddItem(new PlateHelm());
        AddItem(new PlateGloves());
        AddItem(new PlateGorget());

        AddItem(new Cloak(0xCF));

        AddItem(new ThinLongsword());

        SetSkill(SkillName.Swords, 50.0);
        SetSkill(SkillName.Tactics, 50.0);

        PackGold(500);
    }

    public override bool ClickTitle => false;

    public override bool PlayerRangeSensitive => false;

    public override bool IsHarmfulCriminal(Mobile target) => target != _necromancer && base.IsHarmfulCriminal(target);

    public override void OnThink()
    {
        if (!_toDelete && !Frozen)
        {
            if (_necromancer?.Deleted != false || _necromancer.Map == Map.Internal)
            {
                Delete();
                return;
            }

            if (Combatant != _necromancer)
            {
                Combatant = _necromancer;
            }

            if (!_necromancer.Alive)
            {
                var qs = _necromancer.Quest;

                if (qs is DarkTidesQuest && qs.FindObjective<FindMardothEndObjective>() == null)
                {
                    qs.AddObjective(new FindMardothEndObjective(false));
                }

                Say(1060139, _necromancer.Name); // You have made my work easy for me, ~1_NAME~.  My task here is done.

                _toDelete = true;

                Timer.StartTimer(TimeSpan.FromSeconds(5.0), Delete);
            }
            else if (_necromancer.Map != Map || GetDistanceToSqrt(_necromancer) > RangePerception + 1)
            {
                Effects.SendLocationParticles(
                    EffectItem.Create(Location, Map, EffectItem.DefaultDuration),
                    0x3728,
                    10,
                    10,
                    2023
                );
                Effects.SendLocationParticles(
                    EffectItem.Create(_necromancer.Location, _necromancer.Map, EffectItem.DefaultDuration),
                    0x3728,
                    10,
                    10,
                    5023
                );

                Map = _necromancer.Map;
                Location = _necromancer.Location;

                PlaySound(0x1FE);

                Say(1060140); // You cannot escape me, knave of evil!
            }
        }

        base.OnThink();
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        var qs = _necromancer.Quest;

        if (qs is DarkTidesQuest && qs.FindObjective<FindMardothEndObjective>() == null)
        {
            qs.AddObjective(new FindMardothEndObjective(true));
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_toDelete)
        {
            Timer.DelayCall(Delete);
        }
    }

    public static void BeginSummon(PlayerMobile player)
    {
        new SummonTimer(player).Start();
    }

    private class SummonTimer : Timer
    {
        private readonly PlayerMobile _player;
        private SummonedPaladin _paladin;
        private int m_Step;

        public SummonTimer(PlayerMobile player) : base(TimeSpan.FromSeconds(4.0))
        {

            _player = player;
        }

        protected override void OnTick()
        {
            if (_player.Deleted)
            {
                if (m_Step > 0)
                {
                    _paladin.Delete();
                }

                return;
            }

            if (m_Step > 0 && _paladin.Deleted)
            {
                return;
            }

            if (m_Step == 0)
            {
                var moongate = new SummonedPaladinMoongate();
                moongate.MoveToWorld(new Point3D(2091, 1348, -90), Map.Malas);

                Effects.PlaySound(moongate.Location, moongate.Map, 0x20E);

                _paladin = new SummonedPaladin(_player);
                _paladin.Frozen = true;

                _paladin.Location = moongate.Location;
                _paladin.Map = moongate.Map;

                Delay = TimeSpan.FromSeconds(2.0);
                Start();
            }
            else if (m_Step == 1)
            {
                _paladin.Direction = _paladin.GetDirectionTo(_player);
                _paladin.Say(1060122); // STOP WICKED ONE!

                Delay = TimeSpan.FromSeconds(3.0);
                Start();
            }
            else
            {
                _paladin.Frozen = false;

                _paladin.Say(1060123); // I will slay you before I allow you to complete your evil rites!

                _paladin.Combatant = _player;
            }

            m_Step++;
        }
    }
}

[SerializationGenerator(0, false)]
public partial class SummonedPaladinMoongate : Item
{
    public SummonedPaladinMoongate() : base(0xF6C)
    {
        Movable = false;
        Hue = 0x482;
        Light = LightType.Circle300;

        Timer.StartTimer(TimeSpan.FromSeconds(10.0), Delete);
    }
}
