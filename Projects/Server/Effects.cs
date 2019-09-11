/***************************************************************************
 *                                Effects.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using Server.Network;

namespace Server
{
  public enum EffectLayer
  {
    Head = 0,
    RightHand = 1,
    LeftHand = 2,
    Waist = 3,
    LeftFoot = 4,
    RightFoot = 5,
    CenterFeet = 7
  }

  public enum ParticleSupportType
  {
    Full,
    Detect,
    None
  }

  public static class Effects
  {
    public static ParticleSupportType ParticleSupportType{ get; set; } = ParticleSupportType.Detect;

    public static bool SendParticlesTo(NetState state)
    {
      return ParticleSupportType == ParticleSupportType.Full ||
             ParticleSupportType == ParticleSupportType.Detect && state.IsUOTDClient;
    }

    public static void PlaySound(IPoint3D p, Map map, int soundId)
    {
      if (soundId <= -1)
        return;

      if (map != null)
      {
        IPooledEnumerable<NetState> eable = map.GetClientsInRange(new Point3D(p));

        foreach (NetState state in eable)
        {
          state.Mobile.ProcessDelta();
          Packets.SendPlaySound(state, soundId, p);
        }

        eable.Free();
      }
    }

    public static void SendBoltEffect(IEntity e, bool sound = true)
    {
      Map map = e.Map;

      if (map == null)
        return;

      e.ProcessDelta();

      IPooledEnumerable<NetState> eable = map.GetClientsInRange(e.Location);

      foreach (NetState state in eable)
        if (state.Mobile.CanSee(e))
        {
          if (SendParticlesTo(state))
            Packets.SendTargetParticleEffect(state, e, 0, 10, 5, 0, 0, 5031, 3, 0);

          Packets.SendBoltEffect(state, e);

          if (sound)
            Packets.SendPlaySound(state, 0x29, e);
        }

      eable.Free();
    }

    public static void SendLocationEffect(IPoint3D p, Map map, int itemID, int duration, int speed = 10, int hue = 0,
      int renderMode = 0)
    {
      IPooledEnumerable<NetState> eable = map.GetClientsInRange(p);

      foreach (NetState state in eable)
      {
        state.Mobile.ProcessDelta();
        Packets.SendLocationHuedEffect(state, p, itemID, speed, duration, hue, renderMode);
      }

      eable.Free();
    }

    public static void SendLocationParticles(IEntity e, int itemID, int speed, int duration, int effect)
    {
      SendLocationParticles(e, itemID, speed, duration, 0, 0, effect, 0);
    }

    public static void SendLocationParticles(IEntity e, int itemID, int speed, int duration, int effect, int unknown)
    {
      SendLocationParticles(e, itemID, speed, duration, 0, 0, effect, unknown);
    }

    public static void SendLocationParticles(IEntity e, int itemID, int speed, int duration, int hue, int renderMode,
      int effect, int unknown)
    {
      Map map = e.Map;

      if (map == null)
        return;

      IPooledEnumerable<NetState> eable = map.GetClientsInRange(e.Location);

      foreach (NetState state in eable)
      {
        state.Mobile.ProcessDelta();

        if (SendParticlesTo(state))
          Packets.SendLocationParticleEffect(state, e, itemID, speed, duration, hue,
            renderMode, effect, unknown);
        else if (itemID != 0)
          Packets.SendLocationHuedEffect(state, e, itemID, speed, duration, hue, renderMode);
      }

      eable.Free();
    }

    public static void SendTargetEffect(IEntity target, int itemID, int speed, int duration, int hue = 0, int renderMode = 0)
    {
      if (target is Mobile mobile)
        mobile.ProcessDelta();

      IPooledEnumerable<NetState> eable = target.Map.GetClientsInRange(target.Location);

      foreach (NetState state in eable)
      {
        state.Mobile.ProcessDelta();
        Packets.SendTargetHuedEffect(state, target, itemID, speed, duration, hue, renderMode);
      }

      eable.Free();
    }

    public static void SendTargetParticles(IEntity target, int itemID, int speed, int duration, int effect,
      EffectLayer layer)
    {
      SendTargetParticles(target, itemID, speed, duration, 0, 0, effect, layer, 0);
    }

    public static void SendTargetParticles(IEntity target, int itemID, int speed, int duration, int effect,
      EffectLayer layer, int unknown)
    {
      SendTargetParticles(target, itemID, speed, duration, 0, 0, effect, layer, unknown);
    }

    public static void SendTargetParticles(IEntity target, int itemID, int speed, int duration, int hue, int renderMode,
      int effect, EffectLayer layer, int unknown)
    {
      if (target is Mobile mobile)
        mobile.ProcessDelta();

      Map map = target.Map;

      if (map == null)
        return;

      IPooledEnumerable<NetState> eable = map.GetClientsInRange(target.Location);

      foreach (NetState state in eable)
      {
        state.Mobile.ProcessDelta();

        if (SendParticlesTo(state))
          Packets.SendTargetParticleEffect(state, target, itemID, speed, duration, hue,
          renderMode, effect, (int)layer, unknown);
        else if (itemID != 0)
          Packets.SendTargetHuedEffect(state, target, itemID, speed, duration, hue, renderMode);
      }

      eable.Free();
    }

    public static void SendMovingEffect(IEntity from, IEntity to, int itemID, int speed, int duration,
      bool fixedDirection, bool explodes, int hue = 0, int renderMode = 0)
    {
      if (from is Mobile mobile)
        mobile.ProcessDelta();

      if (to is Mobile mobile1)
        mobile1.ProcessDelta();

      IPooledEnumerable<NetState> eable = from.Map.GetClientsInRange(from.Location);

      foreach (NetState state in eable)
      {
        state.Mobile.ProcessDelta();
        Packets.SendMovingHuedEffect(state, from, to, itemID, speed, duration, fixedDirection, explodes, hue, renderMode);
      }

      eable.Free();
    }

    public static void SendMovingParticles(IEntity from, IEntity to, int itemID, int speed, int duration,
      bool fixedDirection, bool explodes, int effect, int explodeEffect, int explodeSound)
    {
      SendMovingParticles(from, to, itemID, speed, duration, fixedDirection, explodes, 0, 0, effect, explodeEffect,
        explodeSound, 0);
    }

    public static void SendMovingParticles(IEntity from, IEntity to, int itemID, int speed, int duration,
      bool fixedDirection, bool explodes, int effect, int explodeEffect, int explodeSound, int unknown)
    {
      SendMovingParticles(from, to, itemID, speed, duration, fixedDirection, explodes, 0, 0, effect, explodeEffect,
        explodeSound, unknown);
    }

    public static void SendMovingParticles(IEntity from, IEntity to, int itemID, int speed, int duration,
      bool fixedDirection, bool explodes, int hue, int renderMode, int effect, int explodeEffect, int explodeSound,
      int unknown)
    {
      SendMovingParticles(from, to, itemID, speed, duration, fixedDirection, explodes, hue, renderMode, effect,
        explodeEffect, explodeSound, (EffectLayer)255, unknown);
    }

    public static void SendMovingParticles(IEntity from, IEntity to, int itemID, int speed, int duration,
      bool fixedDirection, bool explodes, int hue, int renderMode, int effect, int explodeEffect, int explodeSound,
      EffectLayer layer, int unknown)
    {
      if (from is Mobile fromMob)
        fromMob.ProcessDelta();

      if (to is Mobile toMob)
        toMob.ProcessDelta();

      Map map = from.Map;

      if (map == null)
        return;

      IPooledEnumerable<NetState> eable = map.GetClientsInRange(from.Location);

      foreach (NetState state in eable)
      {
        state.Mobile.ProcessDelta();

        if (SendParticlesTo(state))
          Packets.SendMovingParticleEffect(state, from, to, itemID, speed, duration,
            fixedDirection, explodes, hue, renderMode, effect, explodeEffect, explodeSound, layer,
            unknown);
        else if (itemID > 1)
          Packets.SendMovingHuedEffect(state, from, to, itemID, speed, duration, fixedDirection,
            explodes, hue, renderMode);
      }

      eable.Free();
    }
  }
}
