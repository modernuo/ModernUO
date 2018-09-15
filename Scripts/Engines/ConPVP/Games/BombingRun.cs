using System;
using System.Collections;
using System.Text;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Engines.ConPVP
{
  public class BRBomb : Item
  {
    private bool m_Flying;

    private BRGame m_Game;

    private ArrayList m_Helpers;

    private Point3DList m_Path = new Point3DList();
    private int m_PathIdx;
    private EffectTimer m_Timer;

    public BRBomb(BRGame game) :
      base(0x103C) // 0x103C = bread, 0x1042 = pie, 0x1364 = rock, 0x13a8 = pillow, 0x2256 = bagball
    {
      Movable = false;
      Hue = 0x35;

      m_Game = game;

      m_Helpers = new ArrayList();

      m_Timer = new EffectTimer(this);
      m_Timer.Start();
    }

    public BRBomb(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "da bomb";

    public Mobile Thrower{ get; private set; }

    private Mobile FindOwner(object parent)
    {
      if (parent is Item item)
        return item.RootParent as Mobile;

      if (parent is Mobile mobile)
        return mobile;

      return null;
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      switch (version)
      {
        case 0:
        {
          break;
        }
      }

      Timer.DelayCall(TimeSpan.Zero, Delete).Start(); // delete this after the world loads
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void OnAdded(IEntity parent)
    {
      base.OnAdded(parent);

      Mobile mob = FindOwner(parent);

      if (mob != null) mob.SolidHueOverride = 0x0499;
    }

    public override void OnRemoved(IEntity parent)
    {
      base.OnRemoved(parent);

      Mobile mob = FindOwner(parent);

      if (mob != null && m_Game != null)
        mob.SolidHueOverride = m_Game.GetColor(mob);
    }

    public void DropTo(Mobile mob, Mobile killer)
    {
      if (mob != null && !mob.Deleted)
        MoveToWorld(mob.Location, mob.Map);
      else if (killer != null && !killer.Deleted)
        MoveToWorld(killer.Location, killer.Map);
      else
        m_Game?.ReturnBomb();
    }

    public override bool OnMoveOver(Mobile m)
    {
      if (m_Flying || !Visible || m_Game == null || m == null || !m.Alive)
        return true;

      BRTeamInfo useTeam = m_Game.GetTeamInfo(m);
      if (useTeam == null)
        return true;

      return TakeBomb(m, useTeam, "picked up");
    }

    public override void OnLocationChange(Point3D oldLocation)
    {
      base.OnLocationChange(oldLocation);

      if (m_Flying || !Visible || m_Game == null || Parent != null)
        return;

      IPooledEnumerable<NetState> eable = GetClientsInRange(0);
      foreach (NetState ns in eable)
      {
        Mobile m = ns.Mobile;

        if (m == null || !m.Player || !m.Alive)
          continue;

        BRTeamInfo useTeam = m_Game.GetTeamInfo(m);
        if (useTeam != null)
          TakeBomb(m, useTeam, "got");
      }
    }

    public override void OnAfterDelete()
    {
      base.OnAfterDelete();

      m_Timer?.Stop();
    }

    public override void OnDoubleClick(Mobile m)
    {
      if (m_Game == null || !Visible || m == null || !m.Alive)
        return;

      if (!m_Flying && IsChildOf(m.Backpack))
        m.Target = new BombTarget(this, m);
      else if (Parent == null)
        if (m.InRange(Location, 1) && m.Location.Z != Z)
        {
          BRTeamInfo useTeam = m_Game.GetTeamInfo(m);
          if (useTeam == null)
            return;

          TakeBomb(m, useTeam, "grabbed");
        }
    }

    private bool OnBombTarget(Mobile from, object obj)
    {
      if (m_Game == null)
        return true;

      if (!IsChildOf(from.Backpack))
        return true;

      // don't let them throw it to themselves
      if (obj == from)
        return false;

      if (!(obj is IPoint3D))
        return false;

      Point3D pt = new Point3D((IPoint3D)obj);

      if (obj is Mobile)
        pt.Z += 10;
      else if (obj is Item item)
        pt.Z += item.ItemData.CalcHeight + 1;

      m_Flying = true;
      Visible = false;
      Thrower = from;
      MoveToWorld(GetWorldLocation(), from.Map);

      BeginFlight(pt);
      return true;
    }

    private void HitObject(Point3D ballLoc, int objZ, int objHeight)
    {
      DoAnim(GetWorldLocation(), ballLoc, Map);
      MoveToWorld(ballLoc);

      m_Path.Clear();
      m_PathIdx = 0;

      Timer.DelayCall(TimeSpan.FromSeconds(0.05), ContinueFlight).Start();
    }

    private bool CheckCatch(Mobile m, Point3D myLoc)
    {
      if (m == null || !m.Alive || !m.Player || m_Game == null)
        return false;

      if (m_Game.GetTeamInfo(m) == null)
        return false;

      int zdiff = myLoc.Z - m.Z;

      if (zdiff < 0)
        return false;
      if (zdiff < 12)
        return true;
      if (zdiff < 16)
        return Utility.RandomBool(); // 50% chance

      return false;
    }

    private void DoAnim(Point3D start, Point3D end, Map map)
    {
      Effects.SendMovingEffect(new Entity(Serial.Zero, start, map), new Entity(Serial.Zero, end, map),
        ItemID, 15, 0, false, false, Hue, 0);
    }

    private void DoCatch(Mobile m)
    {
      m_Flying = false;
      Visible = true;

      if (m == null || !m.Alive || !m.Player || m_Game == null)
        return;

      BRTeamInfo useTeam = m_Game.GetTeamInfo(m);

      if (useTeam == null)
        return;

      DoAnim(GetWorldLocation(), m.Location, m.Map);

      string verb = "caught";

      if (Thrower != null && m_Game.GetTeamInfo(Thrower) != useTeam)
        verb = "intercepted";

      if (!TakeBomb(m, useTeam, verb))
        MoveToWorld(m.Location, m.Map);
    }

    private void BeginFlight(Point3D dest)
    {
      Point3D org = GetWorldLocation();

      org.Z += 10; // always add 10 at the start cause we're coming from a mobile's eye level

      /*if ( org.X > dest.X || ( org.X == dest.X && org.Y > dest.Y ) || ( org.X == dest.X && org.Y == dest.Y && org.Z > dest.Z ) )
      {
        Point3D swap = org;
        org = dest;
        dest = swap;
      }*/

      ArrayList list = new ArrayList();
      double rise, run, zslp;
      double dist3d, dist2d;
      double x, y, z;
      int xd, yd, zd;
      Point3D p;

      xd = dest.X - org.X;
      yd = dest.Y - org.Y;
      zd = dest.Z - org.Z;
      dist2d = Math.Sqrt(xd * xd + yd * yd);
      if (zd != 0)
        dist3d = Math.Sqrt(dist2d * dist2d + zd * zd);
      else
        dist3d = dist2d;

      rise = yd / dist3d;
      run = xd / dist3d;
      zslp = zd / dist3d;

      x = org.X;
      y = org.Y;
      z = org.Z;
      while (Utility.NumberBetween(x, dest.X, org.X, 0.5) && Utility.NumberBetween(y, dest.Y, org.Y, 0.5) &&
             Utility.NumberBetween(z, dest.Z, org.Z, 0.5))
      {
        int ix = (int)Math.Round(x);
        int iy = (int)Math.Round(y);
        int iz = (int)Math.Round(z);

        if (list.Count > 0)
        {
          p = (Point3D)list[list.Count - 1];

          if (p.X != ix || p.Y != iy || p.Z != iz)
            list.Add(new Point3D(ix, iy, iz));
        }
        else
        {
          list.Add(new Point3D(ix, iy, iz));
        }

        x += run;
        y += rise;
        z += zslp;
      }

      if (list.Count > 0)
        if ((Point3D)list[list.Count - 1] != dest)
          list.Add(dest);

      /*if ( dist3d > 4 && ( dest.X != org.X || dest.Y != org.Y ) )
      {
        int count = list.Count;
        int i;
        int climb = count / 2;
        if ( climb > 3 )
          climb = 3;

        for ( i = 0; i < climb; i++ )
        {
          p = ((Point3D)list[i]);
          p.Z += (i+1) * 4;
          list[i] = p;
        }

        for ( ; i < count - climb; i++ )
        {
          p = ((Point3D)list[i]);
          p.Z += 16;
          list[i] = p;
        }

        for ( i = climb; i > 0; i-- )
        {
          p = ((Point3D)list[i]);
          p.Z += i * 4;
          list[i] = p;
        }
      }*/

      if (dist2d > 1)
      {
        int count = list.Count;
        double height = count * 2 * (Utility.RandomDouble() * 0.40 + 0.10); // 10 - 50%
        double coeff = -height / (count * count / 4.0);

        for (int i = 0; i < count; i++)
        {
          p = (Point3D)list[i];

          int xp = i - count / 2;

          p.Z += (int)Math.Ceiling(coeff * xp * xp + height);

          list[i] = p;
        }
      }

      m_Path.Clear();
      for (int i = 0; i < list.Count; i++)
        m_Path.Add((Point3D)list[i]);

      m_PathIdx = 0;

      ContinueFlight();
    }

    private void ContinueFlight()
    {
      int height;
      bool found = false;

      if (m_PathIdx < m_Path.Count && Map?.Tiles != null && Map != Map.Internal)
      {
        int pathCheckEnd = m_PathIdx + 5;

        if (m_Path.Count < pathCheckEnd)
          pathCheckEnd = m_Path.Count;

        Visible = false;

        if (m_PathIdx > 0) // move to the next location
          MoveToWorld(m_Path[m_PathIdx - 1]);

        Point3D pTop = new Point3D(GetWorldLocation()), pBottom = new Point3D(m_Path[pathCheckEnd - 1]);
        Utility.FixPoints(ref pTop, ref pBottom);

        for (int i = m_PathIdx; i < pathCheckEnd; i++)
        {
          Point3D point = m_Path[i];

          LandTile landTile = Map.Tiles.GetLandTile(point.X, point.Y);
          int landZ = 0, landAvg = 0, landTop = 0;
          Map.GetAverageZ(point.X, point.Y, ref landZ, ref landAvg, ref landTop);

          if (landZ <= point.Z && landTop >= point.Z && !landTile.Ignored)
          {
            HitObject(point, landTop, 0);
            return;
          }

          StaticTile[] statics = Map.Tiles.GetStaticTiles(point.X, point.Y, true);

          if (landTile.ID == 0x244 && statics.Length == 0) // 0x244 = invalid land tile
          {
            bool empty = true;
            IPooledEnumerable<Item> eable = Map.GetItemsInRange(point, 0);

            foreach (Item item in eable)
              if (item != this)
              {
                empty = false;
                break;
              }

            eable.Free();

            if (empty)
            {
              HitObject(point, landTop, 0);
              return;
            }
          }

          for (int j = 0; j < statics.Length; j++)
          {
            StaticTile t = statics[j];

            ItemData id = TileData.ItemTable[t.ID & TileData.MaxItemValue];
            height = id.CalcHeight;

            if (t.Z <= point.Z && t.Z + height >= point.Z &&
                (id.Flags & (TileFlag.Impassable | TileFlag.Wall | TileFlag.NoShoot)) != 0)
            {
              if (i > m_PathIdx)
                point = m_Path[i - 1];
              else
                point = GetWorldLocation();
              HitObject(point, t.Z, height);
              return;
            }
          }
        }

        Rectangle2D rect = new Rectangle2D(pTop.X, pTop.Y, pBottom.X - pTop.X + 1, pBottom.Y - pTop.Y + 1);

        IPooledEnumerable<Item> area = Map.GetItemsInBounds(rect);
        foreach (Item i in area)
        {
          if (i == this || i.ItemID >= 0x4000)
            continue;

          if (i is BRGoal)
          {
            height = 17;
          }
          else if (i is Blocker)
          {
            height = 20;
          }
          else
          {
            ItemData id = i.ItemData;
            if ((id.Flags & (TileFlag.Impassable | TileFlag.Wall | TileFlag.NoShoot)) == 0)
              continue;
            height = id.CalcHeight;
          }

          Point3D point = i.Location;
          Point3D loc = i.Location;
          for (int j = m_PathIdx; j < pathCheckEnd; j++)
          {
            point = m_Path[j];

            if (loc.X == point.X && loc.Y == point.Y &&
                (i is Blocker || loc.Z <= point.Z && loc.Z + height >= point.Z))
            {
              found = true;
              if (j > m_PathIdx)
                point = m_Path[j - 1];
              else
                point = GetWorldLocation();
              break;
            }
          }

          if (!found)
            continue;

          area.Free();
          if (i is BRGoal goal)
          {
            Point3D oldLoc = new Point3D(GetWorldLocation());
            if (CheckScore(goal, Thrower, 3))
              DoAnim(oldLoc, point, Map);
            else
              HitObject(point, loc.Z, height);
          }
          else
          {
            HitObject(point, loc.Z, height);
          }

          return;
        }

        area.Free();

        IPooledEnumerable<NetState> clients = Map.GetClientsInBounds(rect);
        foreach (NetState ns in clients)
        {
          Mobile m = ns.Mobile;

          if (m == null || m == Thrower)
            continue;

          Point3D point;
          Point3D loc = m.Location;

          for (int j = m_PathIdx; j < pathCheckEnd && !found; j++)
          {
            point = m_Path[j];

            if (loc.X == point.X && loc.Y == point.Y &&
                loc.Z <= point.Z && loc.Z + 16 >= point.Z)
              found = CheckCatch(m, point);
          }

          if (!found)
            continue;

          clients.Free();

          // TODO: probably need to change this a lot...
          DoCatch(m);

          return;
        }

        clients.Free();

        m_PathIdx = pathCheckEnd;

        if (m_PathIdx > 0 && m_PathIdx - 1 < m_Path.Count)
          DoAnim(GetWorldLocation(), m_Path[m_PathIdx - 1], Map);

        Timer.DelayCall(TimeSpan.FromSeconds(0.1), ContinueFlight).Start();
      }
      else
      {
        if (m_PathIdx > 0 && m_PathIdx - 1 < m_Path.Count)
          MoveToWorld(m_Path[m_PathIdx - 1]);
        else if (m_Path.Count > 0)
          MoveToWorld(m_Path.Last);

        int myZ = Map.GetAverageZ(X, Y);

        StaticTile[] statics = Map.Tiles.GetStaticTiles(X, Y, true);
        for (int j = 0; j < statics.Length; j++)
        {
          StaticTile t = statics[j];

          ItemData id = TileData.ItemTable[t.ID & TileData.MaxItemValue];
          height = id.CalcHeight;

          if (t.Z + height > myZ && t.Z + height <= Z)
            myZ = t.Z + height;
        }

        IPooledEnumerable<Item> eable = GetItemsInRange(0);
        foreach (Item item in eable)
          if (item.Visible && item != this)
          {
            height = item.ItemData.CalcHeight;
            if (item.Z + height > myZ && item.Z + height <= Z)
              myZ = item.Z + height;
          }

        eable.Free();

        Z = myZ;
        m_Flying = false;
        Visible = true;

        m_Path.Clear();
        m_PathIdx = 0;
      }
    }

    public bool CheckScore(BRGoal goal, Mobile m, int points)
    {
      if (m_Game == null || m == null || goal == null)
        return false;

      BRTeamInfo team = m_Game.GetTeamInfo(m);
      if (team == null || goal.Team == null || team == goal.Team)
        return false;

      if (points > 3)
        m_Game.Alert("Touchdown {0} ({1})!", team.Name, m.Name);
      else
        m_Game.Alert("Field goal {0} ({1})!", team.Name, m.Name);

      for (int i = m_Helpers.Count - 1; i >= 0; i--)
      {
        Mobile mob = (Mobile)m_Helpers[i];

        BRPlayerInfo pi = team[mob];
        if (pi != null)
        {
          if (mob == m)
            pi.Captures += points;

          pi.Score += points + 1;

          points /= 2;
        }
      }

      m_Game.ReturnBomb();

      m_Flying = false;
      Visible = true;
      m_Path.Clear();
      m_PathIdx = 0;

      Target.Cancel(m);

      return true;
    }

    private bool TakeBomb(Mobile m, BRTeamInfo team, string verb)
    {
      if (!m.Player || !m.Alive || m.NetState == null)
        return false;

      if (m.PlaceInBackpack(this))
      {
        m.RevealingAction();

        m.LocalOverheadMessage(MessageType.Regular, 0x59, false, "You got the bomb!");
        m_Game.Alert("{1} ({2}) {0} the bomb!", verb, m.Name, team.Name);

        m.Target = new BombTarget(this, m);

        if (m_Helpers.Contains(m))
          m_Helpers.Remove(m);

        if (m_Helpers.Count > 0)
        {
          Mobile last = (Mobile)m_Helpers[0];

          if (m_Game.GetTeamInfo(last) != team)
            m_Helpers.Clear();
        }

        m_Helpers.Add(m);

        return true;
      }

      return false;
    }

    private class EffectTimer : Timer
    {
      private BRBomb m_Bomb;
      private int m_Count;

      public EffectTimer(BRBomb bomb) : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0))
      {
        m_Bomb = bomb;
        m_Count = 0;
        Priority = TimerPriority.FiftyMS;
      }

      protected override void OnTick()
      {
        if (m_Bomb.Parent == null && m_Bomb.m_Game?.Controller != null)
        {
          if (!m_Bomb.m_Flying && m_Bomb.Map != Map.Internal)
            Effects.SendLocationEffect(m_Bomb.GetWorldLocation(), m_Bomb.Map, 0x377A, 16, 10, m_Bomb.Hue, 0);

          if (m_Bomb.Location != m_Bomb.m_Game.Controller.BombHome)
          {
            if (++m_Count >= 30)
            {
              m_Bomb.m_Game.ReturnBomb();
              m_Bomb.m_Game.Alert("The bomb has been returned to it's starting point.");

              m_Count = 0;
              m_Bomb.m_Helpers.Clear();
            }
          }
          else
          {
            m_Count = 0;
          }
        }
        else
        {
          m_Count = 0;
        }
      }
    }

    private class BombTarget : Target
    {
      private BRBomb m_Bomb;
      private Mobile m_Mob;
      private bool m_Resend = true;

      public BombTarget(BRBomb bomb, Mobile from) : base(10, true, TargetFlags.None)
      {
        CheckLOS = false;

        m_Bomb = bomb;
        m_Mob = from;

        m_Mob.SendMessage(0x26, "Where do you want to throw it?");
      }

      protected override void OnTarget(Mobile from, object targeted)
      {
        m_Resend = !m_Bomb.OnBombTarget(from, targeted);
      }

      protected override void OnTargetUntargetable(Mobile from, object targeted)
      {
        m_Resend = !m_Bomb.OnBombTarget(from, targeted);
      }

      protected override void OnTargetFinish(Mobile from)
      {
        base.OnTargetFinish(from);

        // has to be delayed in case some other target canceled us...
        if (m_Resend)
          Timer.DelayCall(TimeSpan.Zero, ResendBombTarget).Start();
      }

      private void ResendBombTarget()
      {
        // Make sure they still have the bomb, then give them the target back
        if (m_Bomb != null && !m_Bomb.Deleted && m_Mob != null && !m_Mob.Deleted && m_Mob.Alive)
          if (m_Bomb.IsChildOf(m_Mob))
            m_Mob.Target = new BombTarget(m_Bomb, m_Mob);
      }
    }
  }

  public class BRGoal : BaseAddon
  {
    private bool m_North;

    private BRTeamInfo m_Team;

    [Constructible]
    public BRGoal()
    {
      ItemID = 0x51D;
      Hue = 0x84C;
      Visible = true;

      Remake();
    }

    public BRGoal(Serial serial) : base(serial)
    {
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool North
    {
      get => m_North;
      set
      {
        m_North = value;
        Remake();
      }
    }

    public override string DefaultName => "Bombing Run Goal";

    public override bool ShareHue => false;

    public BRTeamInfo Team
    {
      get => m_Team;
      set
      {
        m_Team = value;
        if (m_Team != null && m_Team.Color != 0)
          Hue = m_Team.Color;
        else
          Hue = 0x84C;
      }
    }

    private static AddonComponent SetHue(AddonComponent ac, int hue)
    {
      ac.Hue = hue;
      return ac;
    }

    private void Remake()
    {
      foreach (AddonComponent ac in Components)
      {
        ac.Addon = null;
        ac.Delete();
      }

      Components.Clear();

      // stairs
      AddComponent(new AddonComponent(0x74D), -1, +1, -5);
      AddComponent(new AddonComponent(0x71F), 0, +1, -5);
      AddComponent(new AddonComponent(0x74B), +1, +1, -5);
      AddComponent(new AddonComponent(0x736), +1, 0, -5);
      AddComponent(new AddonComponent(0x74C), +1, -1, -5);
      AddComponent(new AddonComponent(0x737), 0, -1, -5);
      AddComponent(new AddonComponent(0x74A), -1, -1, -5);
      AddComponent(new AddonComponent(0x749), -1, 0, -5);

      // Center Sparkle
      AddComponent(new AddonComponent(0x375A), 0, 0, -1);

      if (!m_North)
      {
        // Pillars
        AddComponent(new AddonComponent(0x0CE), 0, +1, -2);
        AddComponent(new AddonComponent(0x0CC), 0, -1, -2);
        AddComponent(new AddonComponent(0x0D0), 0, 0, -2);

        // Yellow parts
        AddComponent(SetHue(new AddonComponent(0x0DF), 0x499), 0, +1, 7);
        AddComponent(SetHue(new AddonComponent(0x0DF), 0x499), 0, 0, 16);
        AddComponent(SetHue(new AddonComponent(0x0DF), 0x499), 0, -1, 7);

        // Blue Sparkles
        AddComponent(SetHue(new AddonComponent(0x377A), 0x84C), 0, +1, 12);
        AddComponent(SetHue(new AddonComponent(0x377A), 0x84C), 0, +1, -1);
        AddComponent(SetHue(new AddonComponent(0x377A), 0x84C), 0, -1, 12);
        AddComponent(SetHue(new AddonComponent(0x377A), 0x84C), 0, -1, -1);
      }
      else
      {
        // Pillars
        AddComponent(new AddonComponent(0x0CF), +1, 0, -2);
        AddComponent(new AddonComponent(0x0CC), -1, 0, -2);
        AddComponent(new AddonComponent(0x0D1), 0, 0, -2);

        // Yellow parts
        AddComponent(SetHue(new AddonComponent(0x0DF), 0x499), +1, 0, 7);
        AddComponent(SetHue(new AddonComponent(0x0DF), 0x499), 0, 0, 16);
        AddComponent(SetHue(new AddonComponent(0x0DF), 0x499), -1, 0, 7);

        // Blue Sparkles
        AddComponent(SetHue(new AddonComponent(0x377A), 0x84C), +1, 0, 12);
        AddComponent(SetHue(new AddonComponent(0x377A), 0x84C), +1, 0, -1);
        AddComponent(SetHue(new AddonComponent(0x377A), 0x84C), -1, 0, 12);
        AddComponent(SetHue(new AddonComponent(0x377A), 0x84C), -1, 0, -1);
      }
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(1); // version

      writer.Write(m_North);
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      switch (version)
      {
        case 1:
        {
          m_North = reader.ReadBool();
          goto case 0;
        }
        case 0:
        {
          break;
        }
      }

      Hue = 0x84C;
    }

    public override bool OnMoveOver(Mobile m)
    {
      if (!Visible)
        return true;

      if (m == null || !m.Player || !m.Alive || m.Backpack == null || m_Team?.Game == null)
        return true;

      if (!base.OnMoveOver(m))
        return false;

      if (m_Team != null && m_Team.Color != 0)
        Hue = m_Team.Color;
      else
        Hue = 0x84C;

      if (m.Backpack.FindItemByType(typeof(BRBomb), true) is BRBomb b)
        b.CheckScore(this, m, 7);

      return true;
    }
  }

  public sealed class BRBoard : Item
  {
    public BRTeamInfo m_TeamInfo;

    [Constructible]
    public BRBoard()
      : base(7774)
    {
      Movable = false;
    }

    public BRBoard(Serial serial)
      : base(serial)
    {
    }

    public override string DefaultName => "Scoreboard";

    public override void OnDoubleClick(Mobile from)
    {
      if (m_TeamInfo?.Game != null)
      {
        from.CloseGump(typeof(BRBoardGump));
        from.SendGump(new BRBoardGump(from, m_TeamInfo.Game));
      }
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }

  public class BRBoardGump : Gump
  {
    private const int LabelColor32 = 0xFFFFFF;
    private const int BlackColor32 = 0x000000;

    private BRGame m_Game;

    public BRBoardGump(Mobile mob, BRGame game)
      : this(mob, game, null)
    {
    }

    public BRBoardGump(Mobile mob, BRGame game, BRTeamInfo section)
      : base(60, 60)
    {
      m_Game = game;

      BRTeamInfo ourTeam = game.GetTeamInfo(mob);

      ArrayList entries = new ArrayList();

      if (section == null)
        for (int i = 0; i < game.Context.Participants.Count; ++i)
        {
          BRTeamInfo teamInfo = game.Controller.TeamInfo[i % game.Controller.TeamInfo.Length];

          if (teamInfo == null)
            continue;

          entries.Add(teamInfo);
        }
      else
        foreach (BRPlayerInfo player in section.Players.Values)
          if (player.Score > 0)
            entries.Add(player);

      entries.Sort();
      /*
      delegate( IRankedCTF a, IRankedCTF b )
    {
      return b.Score - a.Score;
    } );*/

      int height = 0;

      if (section == null)
        height = 73 + entries.Count * 75 + 28;

      Closable = false;

      AddPage(0);

      AddBackground(1, 1, 398, height, 3600);

      AddImageTiled(16, 15, 369, height - 29, 3604);

      for (int i = 0; i < entries.Count; i += 1)
        AddImageTiled(22, 58 + i * 75, 357, 70, 0x2430);

      AddAlphaRegion(16, 15, 369, height - 29);

      AddImage(215, -45, 0xEE40);
      //AddImage( 330, 141, 0x8BA );

      AddBorderedText(22, 22, 294, 20, Center("BR Scoreboard"), LabelColor32, BlackColor32);

      AddImageTiled(32, 50, 264, 1, 9107);
      AddImageTiled(42, 52, 264, 1, 9157);

      if (section == null)
        for (int i = 0; i < entries.Count; ++i)
        {
          BRTeamInfo teamInfo = entries[i] as BRTeamInfo;

          AddImage(30, 70 + i * 75, 10152);
          AddImage(30, 85 + i * 75, 10151);
          AddImage(30, 100 + i * 75, 10151);
          AddImage(30, 106 + i * 75, 10154);

          AddImage(24, 60 + i * 75, teamInfo == ourTeam ? 9730 : 9727, teamInfo.Color - 1);

          int nameColor = LabelColor32;
          int borderColor = BlackColor32;

          switch (teamInfo.Color)
          {
            case 0x47E:
              nameColor = 0xFFFFFF;
              break;

            case 0x4F2:
              nameColor = 0x3399FF;
              break;

            case 0x4F7:
              nameColor = 0x33FF33;
              break;

            case 0x4FC:
              nameColor = 0xFF00FF;
              break;

            case 0x021:
              nameColor = 0xFF3333;
              break;

            case 0x01A:
              nameColor = 0xFF66FF;
              break;

            case 0x455:
              nameColor = 0x333333;
              borderColor = 0xFFFFFF;
              break;
          }

          AddBorderedText(60, 65 + i * 75, 250, 20, $"{LadderGump.Rank(1 + i)}: {teamInfo.Name}", nameColor,
            borderColor);

          AddBorderedText(50 + 10, 85 + i * 75, 100, 20, "Score:", 0xFFC000, BlackColor32);
          AddBorderedText(50 + 15, 105 + i * 75, 100, 20, teamInfo.Score.ToString("N0"), 0xFFC000, BlackColor32);

          AddBorderedText(110 + 10, 85 + i * 75, 100, 20, "Kills:", 0xFFC000, BlackColor32);
          AddBorderedText(110 + 15, 105 + i * 75, 100, 20, teamInfo.Kills.ToString("N0"), 0xFFC000, BlackColor32);

          AddBorderedText(160 + 10, 85 + i * 75, 100, 20, "Points:", 0xFFC000, BlackColor32);
          AddBorderedText(160 + 15, 105 + i * 75, 100, 20, teamInfo.Captures.ToString("N0"), 0xFFC000,
            BlackColor32);

          BRPlayerInfo pl = teamInfo.Leader;

          AddBorderedText(235 + 10, 85 + i * 75, 250, 20, "Leader:", 0xFFC000, BlackColor32);

          if (pl != null)
            AddBorderedText(235 + 15, 105 + i * 75, 250, 20, pl.Player.Name, 0xFFC000, BlackColor32);
        }

      AddButton(314, height - 42, 247, 248, 1, GumpButtonType.Reply, 0);
    }

    public string Center(string text)
    {
      return $"<CENTER>{text}</CENTER>";
    }

    public string Color(string text, int color)
    {
      return $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";
    }

    private void AddBorderedText(int x, int y, int width, int height, string text, int color, int borderColor)
    {
      AddColoredText(x - 1, y - 1, width, height, text, borderColor);
      AddColoredText(x - 1, y + 1, width, height, text, borderColor);
      AddColoredText(x + 1, y - 1, width, height, text, borderColor);
      AddColoredText(x + 1, y + 1, width, height, text, borderColor);
      AddColoredText(x, y, width, height, text, color);
    }

    private void AddColoredText(int x, int y, int width, int height, string text, int color)
    {
      if (color == 0)
        AddHtml(x, y, width, height, text, false, false);
      else
        AddHtml(x, y, width, height, Color(text, color), false, false);
    }
  }

  public sealed class BRPlayerInfo : IRankedCTF, IComparable
  {
    private int m_Captures;

    private int m_Kills;

    private int m_Score;
    private BRTeamInfo m_TeamInfo;

    public BRPlayerInfo(BRTeamInfo teamInfo, Mobile player)
    {
      m_TeamInfo = teamInfo;
      Player = player;
    }

    public Mobile Player{ get; }

    public int CompareTo(object obj)
    {
      BRPlayerInfo pi = (BRPlayerInfo)obj;
      int res = pi.Captures.CompareTo(Captures);
      if (res == 0)
      {
        res = pi.Score.CompareTo(Score);

        if (res == 0)
          res = pi.Kills.CompareTo(Kills);
      }

      return res;
    }

    public string Name => Player.Name;

    public int Kills
    {
      get => m_Kills;
      set
      {
        m_TeamInfo.Kills += value - m_Kills;
        m_Kills = value;
      }
    }

    public int Captures
    {
      get => m_Captures;
      set
      {
        m_TeamInfo.Captures += value - m_Captures;
        m_Captures = value;
      }
    }

    public int Score
    {
      get => m_Score;
      set
      {
        m_TeamInfo.Score += value - m_Score;
        m_Score = value;

        if (m_TeamInfo.Leader == null || m_Score > m_TeamInfo.Leader.Score)
          m_TeamInfo.Leader = this;
      }
    }
  }

  [PropertyObject]
  public sealed class BRTeamInfo : IRankedCTF, IComparable
  {
    private BRGoal m_Goal;

    public BRTeamInfo(int teamID)
    {
      TeamID = teamID;
      Players = new Hashtable();
    }

    public BRTeamInfo(int teamID, GenericReader ip)
    {
      TeamID = teamID;
      Players = new Hashtable();

      int version = ip.ReadEncodedInt();

      switch (version)
      {
        case 0:
        {
          Board = ip.ReadItem() as BRBoard;
          TeamName = ip.ReadString();
          Color = ip.ReadEncodedInt();
          m_Goal = ip.ReadItem() as BRGoal;
          break;
        }
      }
    }

    public BRGame Game{ get; set; }

    public int TeamID{ get; }

    public BRPlayerInfo Leader{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public BRBoard Board{ get; set; }

    public Hashtable Players{ get; }

    public BRPlayerInfo this[Mobile mob]
    {
      get
      {
        if (mob == null)
          return null;

        if (!(Players[mob] is BRPlayerInfo val))
          Players[mob] = val = new BRPlayerInfo(this, mob);

        return val;
      }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Color{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public string TeamName{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public BRGoal Goal
    {
      get => m_Goal;
      set
      {
        m_Goal = value;
        if (m_Goal != null)
          m_Goal.Team = this;
      }
    }

    public int CompareTo(object obj)
    {
      BRTeamInfo ti = (BRTeamInfo)obj;
      int res = ti.Captures.CompareTo(Captures);
      if (res == 0)
      {
        res = ti.Score.CompareTo(Score);

        if (res == 0)
          res = ti.Kills.CompareTo(Kills);
      }

      return res;
    }

    public string Name => $"{TeamName} Team";

    public int Kills{ get; set; }

    public int Captures{ get; set; }

    public int Score{ get; set; }

    public void Reset()
    {
      Kills = 0;
      Captures = 0;
      Score = 0;

      Leader = null;

      Players.Clear();

      if (Board != null)
        Board.m_TeamInfo = this;
      if (m_Goal != null)
        m_Goal.Team = this;
    }

    public void Serialize(GenericWriter op)
    {
      op.WriteEncodedInt(0); // version

      op.Write(Board);

      op.Write(TeamName);

      op.WriteEncodedInt(Color);

      op.Write(m_Goal);
    }

    public override string ToString()
    {
      if (TeamName != null)
        return $"({Name}) ...";
      return "...";
    }
  }

  public sealed class BRController : EventController
  {
    [Constructible]
    public BRController()
    {
      Visible = false;
      Movable = false;

      Duration = TimeSpan.FromMinutes(30.0);

      BombHome = Point3D.Zero;

      TeamInfo = new BRTeamInfo[4];

      for (int i = 0; i < TeamInfo.Length; ++i)
        TeamInfo[i] = new BRTeamInfo(i);
    }

    public BRController(Serial serial)
      : base(serial)
    {
    }

    public BRTeamInfo[] TeamInfo{ get; private set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public BRTeamInfo Team1
    {
      get => TeamInfo[0];
      set { }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public BRTeamInfo Team2
    {
      get => TeamInfo[1];
      set { }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public BRTeamInfo Team3
    {
      get => TeamInfo[2];
      set { }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public BRTeamInfo Team4
    {
      get => TeamInfo[3];
      set { }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public TimeSpan Duration{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Point3D BombHome{ get; set; }

    public override string Title => "Bombing Run";
    public override string DefaultName => "Bombing Run Controller";

    public override string GetTeamName(int teamID)
    {
      return TeamInfo[teamID % TeamInfo.Length].Name;
    }

    public override EventGame Construct(DuelContext context)
    {
      return new BRGame(this, context);
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);

      writer.Write(BombHome);

      writer.Write(Duration);

      writer.WriteEncodedInt(TeamInfo.Length);

      for (int i = 0; i < TeamInfo.Length; ++i)
        TeamInfo[i].Serialize(writer);
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      switch (version)
      {
        case 0:
        {
          BombHome = reader.ReadPoint3D();

          Duration = reader.ReadTimeSpan();

          TeamInfo = new BRTeamInfo[reader.ReadEncodedInt()];

          for (int i = 0; i < TeamInfo.Length; ++i)
            TeamInfo[i] = new BRTeamInfo(i, reader);

          break;
        }
      }
    }
  }

  public sealed class BRGame : EventGame
  {
    private BRBomb m_Bomb;

    private Timer m_FinishTimer;

    private TimerCallback m_UnhideCallback;

    public BRGame(BRController controller, DuelContext context) : base(context)
    {
      Controller = controller;
    }

    public BRController Controller{ get; }

    public Map Facet
    {
      get
      {
        if (m_Context.Arena != null)
          return m_Context.Arena.Facet;

        return Controller.Map;
      }
    }

    public override bool CantDoAnything(Mobile mob)
    {
      if (mob?.Backpack == null || GetTeamInfo(mob) == null)
        return false;

      Item bomb = mob.Backpack.FindItemByType(typeof(BRBomb), true);

      if (bomb != null)
        return true;

      return false;
    }

    public void ReturnBomb()
    {
      if (m_Bomb != null && Controller != null)
      {
        if (m_UnhideCallback == null)
          m_UnhideCallback = UnhideBomb;
        m_Bomb.Visible = false;
        m_Bomb.MoveToWorld(Controller.BombHome, Controller.Map);
        Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomMinMax(5, 15)), m_UnhideCallback);
      }
    }

    private void UnhideBomb()
    {
      if (m_Bomb != null)
      {
        m_Bomb.Visible = true;
        m_Bomb.OnLocationChange(m_Bomb.Location);
      }
    }

    public void Alert(string text)
    {
      m_Context.m_Tournament?.Alert(text);

      for (int i = 0; i < m_Context.Participants.Count; ++i)
      {
        Participant p = m_Context.Participants[i] as Participant;

        for (int j = 0; j < p.Players.Length; ++j)
          if (p.Players[j] != null)
            p.Players[j].Mobile.SendMessage(0x35, text);
      }
    }

    public void Alert(string format, params object[] args)
    {
      Alert(string.Format(format, args));
    }

    public BRTeamInfo GetTeamInfo(Mobile mob)
    {
      int teamID = GetTeamID(mob);

      if (teamID >= 0)
        return Controller.TeamInfo[teamID % Controller.TeamInfo.Length];

      return null;
    }

    public int GetTeamID(Mobile mob)
    {
      if (!(mob is PlayerMobile pm))
        return mob is BaseCreature creature ? creature.Team - 1 : -1;

      if (pm.DuelContext == null || pm.DuelContext != m_Context)
        return -1;

      if (pm.DuelPlayer == null || pm.DuelPlayer.Eliminated)
        return -1;

      return pm.DuelContext.Participants.IndexOf(pm.DuelPlayer.Participant);
    }

    public int GetColor(Mobile mob)
    {
      return GetTeamInfo(mob)?.Color ?? -1;
    }

    private void ApplyHues(Participant p, int hueOverride)
    {
      for (int i = 0; i < p.Players.Length; ++i)
        if (p.Players[i] != null)
          p.Players[i].Mobile.SolidHueOverride = hueOverride;
    }

    public void DelayBounce(TimeSpan ts, Mobile mob, Container corpse)
    {
      Timer.DelayCall(ts, new TimerStateCallback(DelayBounce_Callback), new object[] { mob, corpse });
    }

    private void DelayBounce_Callback(object state)
    {
      object[] states = (object[])state;
      Mobile mob = (Mobile)states[0];
      Container corpse = (Container)states[1];

      DuelPlayer dp = null;

      if (mob is PlayerMobile mobile)
        dp = mobile.DuelPlayer;

      m_Context.RemoveAggressions(mob);

      if (dp != null && !dp.Eliminated)
        mob.MoveToWorld(m_Context.Arena.GetBaseStartPoint(GetTeamID(mob)), Facet);
      else
        m_Context.SendOutside(mob);

      m_Context.Refresh(mob, corpse);
      DuelContext.Debuff(mob);
      DuelContext.CancelSpell(mob);
      mob.Frozen = false;
    }

    public override bool OnDeath(Mobile mob, Container corpse)
    {
      Mobile killer = mob.FindMostRecentDamager(false);

      bool hadBomb = false;

      Item[] bombs = corpse.FindItemsByType(typeof(BRBomb), false);

      for (int i = 0; i < bombs.Length; ++i)
        (bombs[i] as BRBomb).DropTo(mob, killer);

      hadBomb = hadBomb || bombs.Length > 0;

      if (mob.Backpack != null)
      {
        bombs = mob.Backpack.FindItemsByType(typeof(BRBomb), false);

        for (int i = 0; i < bombs.Length; ++i)
          (bombs[i] as BRBomb).DropTo(mob, killer);

        hadBomb = hadBomb || bombs.Length > 0;
      }

      if (killer != null && killer.Player)
      {
        BRTeamInfo teamInfo = GetTeamInfo(killer);
        BRTeamInfo victInfo = GetTeamInfo(mob);

        if (teamInfo != null && teamInfo != victInfo)
        {
          BRPlayerInfo playerInfo = teamInfo[killer];

          if (playerInfo != null)
          {
            playerInfo.Kills += 1;
            playerInfo.Score += 1; // base frag

            if (hadBomb)
              playerInfo.Score += 4; // fragged bomb carrier
          }
        }
      }

      mob.CloseGump(typeof(BRBoardGump));
      mob.SendGump(new BRBoardGump(mob, this));

      m_Context.Requip(mob, corpse);
      DelayBounce(TimeSpan.FromSeconds(30.0), mob, corpse);

      return false;
    }

    public override void OnStart()
    {
      for (int i = 0; i < Controller.TeamInfo.Length; ++i)
      {
        BRTeamInfo teamInfo = Controller.TeamInfo[i];

        teamInfo.Game = this;
        teamInfo.Reset();
      }

      for (int i = 0; i < m_Context.Participants.Count; ++i)
        ApplyHues(m_Context.Participants[i] as Participant,
          Controller.TeamInfo[i % Controller.TeamInfo.Length].Color);

      m_FinishTimer?.Stop();

      m_Bomb = new BRBomb(this);
      ReturnBomb();

      m_FinishTimer = Timer.DelayCall(Controller.Duration, Finish_Callback);
    }

    private void Finish_Callback()
    {
      ArrayList teams = new ArrayList();

      for (int i = 0; i < m_Context.Participants.Count; ++i)
      {
        BRTeamInfo teamInfo = Controller.TeamInfo[i % Controller.TeamInfo.Length];

        if (teamInfo == null)
          continue;

        teams.Add(teamInfo);
      }

      teams.Sort();

      Tournament tourny = m_Context.m_Tournament;

      StringBuilder sb = new StringBuilder();

      if (tourny != null && tourny.TournyType == TournyType.FreeForAll)
      {
        sb.Append(m_Context.Participants.Count * tourny.PlayersPerParticipant);
        sb.Append("-man FFA");
      }
      else if (tourny != null && tourny.TournyType == TournyType.RandomTeam)
      {
        sb.Append(tourny.ParticipantsPerMatch);
        sb.Append("-team");
      }
      else if (tourny != null && tourny.TournyType == TournyType.RedVsBlue)
      {
        sb.Append("Red v Blue");
      }
      else if (tourny != null && tourny.TournyType == TournyType.Faction)
      {
        sb.Append(tourny.ParticipantsPerMatch);
        sb.Append("-team Faction");
      }
      else if (tourny != null)
      {
        for (int i = 0; i < tourny.ParticipantsPerMatch; ++i)
        {
          if (sb.Length > 0)
            sb.Append('v');

          sb.Append(tourny.PlayersPerParticipant);
        }
      }

      if (Controller != null)
        sb.Append(' ').Append(Controller.Title);

      string title = sb.ToString();

      BRTeamInfo winner = (BRTeamInfo)(teams.Count > 0 ? teams[0] : null);

      for (int i = 0; i < teams.Count; ++i)
      {
        TrophyRank rank = TrophyRank.Bronze;

        if (i == 0)
          rank = TrophyRank.Gold;
        else if (i == 1)
          rank = TrophyRank.Silver;

        BRPlayerInfo leader = ((BRTeamInfo)teams[i]).Leader;

        foreach (BRPlayerInfo pl in ((BRTeamInfo)teams[i]).Players.Values)
        {
          Mobile mob = pl.Player;

          if (mob == null)
            continue;

          sb = new StringBuilder();

          sb.Append(title);

          if (pl == leader)
            sb.Append(" Leader");

          if (pl.Score > 0)
          {
            sb.Append(": ");

            //sb.Append( pl.Score.ToString( "N0" ) );
            //sb.Append( pl.Score == 1 ? " point" : " points" );

            sb.Append(pl.Kills.ToString("N0"));
            sb.Append(pl.Kills == 1 ? " kill" : " kills");

            if (pl.Captures > 0)
            {
              sb.Append(", ");
              sb.Append(pl.Captures.ToString("N0"));
              sb.Append(pl.Captures == 1 ? " point" : " points");
            }
          }

          Item item = new Trophy(sb.ToString(), rank);

          if (pl == leader)
            item.ItemID = 4810;

          item.Name = $"{item.Name}, {((BRTeamInfo)teams[i]).Name.ToLower()} team";

          if (!mob.PlaceInBackpack(item))
            mob.BankBox.DropItem(item);

          int cash = pl.Score * 250;

          if (cash > 0)
          {
            item = new BankCheck(cash);

            if (!mob.PlaceInBackpack(item))
              mob.BankBox.DropItem(item);

            mob.SendMessage(
              "You have been awarded a {0} trophy and {1:N0}gp for your participation in this tournament.",
              rank.ToString().ToLower(), cash);
          }
          else
          {
            mob.SendMessage("You have been awarded a {0} trophy for your participation in this tournament.",
              rank.ToString().ToLower());
          }
        }
      }

      for (int i = 0; i < m_Context.Participants.Count; ++i)
      {
        if (!(m_Context.Participants[i] is Participant p) || p.Players == null)
          continue;

        for (int j = 0; j < p.Players.Length; ++j)
        {
          DuelPlayer dp = p.Players[j];

          if (dp?.Mobile != null)
          {
            dp.Mobile.CloseGump(typeof(BRBoardGump));
            dp.Mobile.SendGump(new BRBoardGump(dp.Mobile, this));
          }
        }

        if (i == winner.TeamID)
          continue;

        if (p.Players != null)
          for (int j = 0; j < p.Players.Length; ++j)
            if (p.Players[j] != null)
              p.Players[j].Eliminated = true;
      }

      m_Context.Finish(m_Context.Participants[winner.TeamID] as Participant);
    }

    public override void OnStop()
    {
      for (int i = 0; i < Controller.TeamInfo.Length; ++i)
      {
        BRTeamInfo teamInfo = Controller.TeamInfo[i];

        if (teamInfo.Board != null)
          teamInfo.Board.m_TeamInfo = null;

        teamInfo.Game = null;
      }

      ReturnBomb();

      m_Bomb?.Delete();

      for (int i = 0; i < m_Context.Participants.Count; ++i)
        ApplyHues(m_Context.Participants[i] as Participant, -1);

      m_FinishTimer?.Stop();
      m_FinishTimer = null;
    }
  }
}