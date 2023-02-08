using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class UnholyBone : Item, ICarvable
{
    private SpawnTimer m_Timer;

    [Constructible]
    public UnholyBone() : base(0xF7E)
    {
        Movable = false;
        Hue = 0x497;

        m_Timer = new SpawnTimer(this);
        m_Timer.Start();
    }

    public override string DefaultName => "unholy bone";

    public void Carve(Mobile from, Item item)
    {
        Effects.PlaySound(GetWorldLocation(), Map, 0x48F);
        Effects.SendLocationEffect(GetWorldLocation(), Map, 0x3728, 10);

        if (Utility.RandomDouble() < 0.3)
        {
            if (ItemID == 0xF7E)
            {
                from.SendMessage("You destroy the bone.");
            }
            else
            {
                from.SendMessage("You destroy the bone pile.");
            }

            var gold = new Gold(25, 100);

            gold.MoveToWorld(GetWorldLocation(), Map);

            Delete();

            m_Timer.Stop();
        }
        else if (ItemID == 0xF7E)
        {
            from.SendMessage("You damage the bone.");
        }
        else
        {
            from.SendMessage("You damage the bone pile.");
        }
    }

    [AfterDeserialization]
    private void AfterDeserialize()
    {
        m_Timer = new SpawnTimer(this);
        m_Timer.Start();
    }

    private class SpawnTimer : Timer
    {
        private Item m_Item;

        public SpawnTimer(Item item) : base(TimeSpan.FromSeconds(Utility.RandomMinMax(5, 10))) => m_Item = item;

        protected override void OnTick()
        {
            if (m_Item?.Deleted != false)
            {
                return;
            }

            Mobile spawn = Utility.Random(12) switch
            {
                0  => new Skeleton(),
                1  => new Zombie(),
                2  => new Wraith(),
                3  => new Spectre(),
                4  => new Ghoul(),
                5  => new Mummy(),
                6  => new Bogle(),
                7  => new RottingCorpse(),
                8  => new BoneKnight(),
                9  => new SkeletalKnight(),
                10 => new Lich(),
                11 => new LichLord(),
                _  => new Skeleton()
            };

            spawn.MoveToWorld(m_Item.Location, m_Item.Map);

            m_Item.Delete();
        }
    }
}
