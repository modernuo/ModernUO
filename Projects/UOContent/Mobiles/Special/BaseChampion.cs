using System;
using System.Collections.Generic;
using Server.Engines.CannedEvil;
using Server.Items;

namespace Server.Mobiles
{
  public abstract class BaseChampion : BaseCreature
  {
    public BaseChampion(AIType aiType, FightMode mode = FightMode.Closest) : base(aiType, mode, 18, 1, 0.1, 0.2)
    {
    }

    public BaseChampion(Serial serial) : base(serial)
    {
    }

    public override bool CanMoveOverObstacles => true;
    public override bool CanDestroyObstacles => true;

    public abstract ChampionSkullType SkullType { get; }

    public abstract Type[] UniqueList { get; }
    public abstract Type[] SharedList { get; }
    public abstract Type[] DecorativeList { get; }
    public abstract MonsterStatuetteType[] StatueTypes { get; }

    public virtual bool NoGoodies => false;

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }

    public Item GetArtifact()
    {
      double random = Utility.RandomDouble();
      if (random <= 0.05)
        return CreateArtifact(UniqueList);
      if (random <= 0.15)
        return CreateArtifact(SharedList);
      if (random <= 0.30)
        return CreateArtifact(DecorativeList);

      return null;
    }

    public Item CreateArtifact(Type[] list)
    {
      if (list.Length == 0)
        return null;

      Type type = list.RandomElement();

      Item artifact = Loot.Construct(type);

      if (StatueTypes.Length > 0 && artifact is MonsterStatuette statuette)
      {
        statuette.Type = StatueTypes.RandomElement();
        statuette.LootType = LootType.Regular;
      }

      return artifact;
    }

    private PowerScroll CreateRandomPowerScroll()
    {
      int level;
      double random = Utility.RandomDouble();

      if (random <= 0.05)
        level = 20;
      else if (random <= 0.4)
        level = 15;
      else
        level = 10;

      return PowerScroll.CreateRandomNoCraft(level, level);
    }

    public void GivePowerScrolls()
    {
      if (Map != Map.Felucca)
        return;

      List<Mobile> toGive = new List<Mobile>();
      List<DamageStore> rights = GetLootingRights(DamageEntries, HitsMax);

      for (int i = rights.Count - 1; i >= 0; --i)
      {
        DamageStore ds = rights[i];

        if (ds.m_HasRight)
          toGive.Add(ds.m_Mobile);
      }

      if (toGive.Count == 0)
        return;

      for (int i = 0; i < toGive.Count; i++)
      {
        Mobile m = toGive[i];

        if (!(m is PlayerMobile))
          continue;

        bool gainedPath = false;

        int pointsToGain = 800;

        if (VirtueHelper.Award(m, VirtueName.Valor, pointsToGain, ref gainedPath))
        {
          if (gainedPath)
            m.SendLocalizedMessage(1054032); // You have gained a path in Valor!
          else
            m.SendLocalizedMessage(1054030); // You have gained in Valor!

          // No delay on Valor gains
        }
      }

      // Randomize
      toGive.Shuffle();

      for (int i = 0; i < 6; ++i)
      {
        Mobile m = toGive[i % toGive.Count];

        PowerScroll ps = CreateRandomPowerScroll();

        GivePowerScrollTo(m, ps);
      }
    }

    public static void GivePowerScrollTo(Mobile m, PowerScroll ps)
    {
      if (ps == null || m == null) // sanity
        return;

      m.SendLocalizedMessage(1049524); // You have received a scroll of power!

      if (!Core.SE || m.Alive)
      {
        m.AddToBackpack(ps);
      }
      else
      {
        if (m.Corpse?.Deleted == false)
          m.Corpse.DropItem(ps);
        else
          m.AddToBackpack(ps);
      }

      if (!(m is PlayerMobile pm))
        return;

      for (int j = 0; j < pm.JusticeProtectors.Count; ++j)
      {
        Mobile prot = pm.JusticeProtectors[j];

        if (prot.Map != pm.Map || prot.Kills >= 5 || prot.Criminal || !JusticeVirtue.CheckMapRegion(pm, prot))
          continue;

        var chance = VirtueHelper.GetLevel(prot, VirtueName.Justice) switch
        {
          VirtueLevel.Seeker => 60,
          VirtueLevel.Follower => 80,
          VirtueLevel.Knight => 100,
          _ => 0
        };

        if (chance > Utility.Random(100))
        {
          PowerScroll powerScroll = new PowerScroll(ps.Skill, ps.Value);

          prot.SendLocalizedMessage(1049368); // You have been rewarded for your dedication to Justice!

          if (!Core.SE || prot.Alive)
          {
            prot.AddToBackpack(powerScroll);
          }
          else
          {
            if (prot.Corpse?.Deleted == false)
              prot.Corpse.DropItem(powerScroll);
            else
              prot.AddToBackpack(powerScroll);
          }
        }
      }
    }

    public override bool OnBeforeDeath()
    {
      if (!NoKillAwards)
      {
        GivePowerScrolls();

        if (NoGoodies)
          return base.OnBeforeDeath();

        Map map = Map;

        if (map != null)
          for (int x = -12; x <= 12; ++x)
            for (int y = -12; y <= 12; ++y)
            {
              double dist = Math.Sqrt(x * x + y * y);

              if (dist <= 12)
                new GoodiesTimer(map, X + x, Y + y).Start();
            }
      }

      return base.OnBeforeDeath();
    }

    public override void OnDeath(Container c)
    {
      if (Map == Map.Felucca)
      {
        // TODO: Confirm SE change or AoS one too?
        List<DamageStore> rights = GetLootingRights(DamageEntries, HitsMax);
        List<Mobile> toGive = new List<Mobile>();

        for (int i = rights.Count - 1; i >= 0; --i)
        {
          DamageStore ds = rights[i];

          if (ds.m_HasRight)
            toGive.Add(ds.m_Mobile);
        }

        if (toGive.Count > 0)
          toGive.RandomElement().AddToBackpack(new ChampionSkull(SkullType));
        else
          c.DropItem(new ChampionSkull(SkullType));
      }

      base.OnDeath(c);
    }

    private class GoodiesTimer : Timer
    {
      private readonly Map m_Map;
      private readonly int m_X;
      private readonly int m_Y;

      public GoodiesTimer(Map map, int x, int y) : base(TimeSpan.FromSeconds(Utility.RandomDouble() * 10.0))
      {
        m_Map = map;
        m_X = x;
        m_Y = y;
      }

      protected override void OnTick()
      {
        int z = m_Map.GetAverageZ(m_X, m_Y);
        bool canFit = m_Map.CanFit(m_X, m_Y, z, 6, false, false);

        for (int i = -3; !canFit && i <= 3; ++i)
        {
          canFit = m_Map.CanFit(m_X, m_Y, z + i, 6, false, false);

          if (canFit)
            z += i;
        }

        if (!canFit)
          return;

        Gold g = new Gold(500, 1000);

        g.MoveToWorld(new Point3D(m_X, m_Y, z), m_Map);

        if (Utility.RandomDouble() <= 0.5)
          switch (Utility.Random(3))
          {
            case 0: // Fire column
              {
                Effects.SendLocationParticles(EffectItem.Create(g.Location, g.Map, EffectItem.DefaultDuration),
                  0x3709, 10, 30, 5052);
                Effects.PlaySound(g, g.Map, 0x208);

                break;
              }
            case 1: // Explosion
              {
                Effects.SendLocationParticles(EffectItem.Create(g.Location, g.Map, EffectItem.DefaultDuration),
                  0x36BD, 20, 10, 5044);
                Effects.PlaySound(g, g.Map, 0x307);

                break;
              }
            case 2: // Ball of fire
              {
                Effects.SendLocationParticles(EffectItem.Create(g.Location, g.Map, EffectItem.DefaultDuration),
                  0x36FE, 10, 10, 5052);

                break;
              }
          }
      }
    }
  }
}
