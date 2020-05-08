/***************************************************************************
 *                                Packets.cs
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

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Server.Accounting;
using Server.Compression;
using Server.ContextMenus;
using Server.Gumps;
using Server.HuePickers;
using Server.Items;
using Server.Menus;
using Server.Menus.ItemLists;
using Server.Menus.Questions;
using Server.Prompts;
using Server.Targeting;

namespace Server.Network
{
  public enum PMMessage : byte
  {
    CharNoExist = 1,
    CharExists = 2,
    CharInWorld = 5,
    LoginSyncError = 6,
    IdleWarning = 7
  }

  public enum LRReason : byte
  {
    CannotLift = 0,
    OutOfRange = 1,
    OutOfSight = 2,
    TryToSteal = 3,
    AreHolding = 4,
    Inspecific = 5
  }

  public sealed class ChangeUpdateRange : Packet
  {
    private static readonly ChangeUpdateRange[] m_Cache = new ChangeUpdateRange[0x100];

    public ChangeUpdateRange(int range) : base(0xC8, 2)
    {
      Stream.Write((byte)range);
    }

    public static ChangeUpdateRange Instantiate(int range)
    {
      var idx = (byte)range;
      var p = m_Cache[idx];

      if (p == null)
      {
        m_Cache[idx] = p = new ChangeUpdateRange(range);
        p.SetStatic();
      }

      return p;
    }
  }

  public sealed class ChangeCombatant : Packet
  {
    public ChangeCombatant(Mobile combatant) : base(0xAA, 5)
    {
      Stream.Write(combatant?.Serial ?? Serial.Zero);
    }
  }

  public sealed class DisplayHuePicker : Packet
  {
    public DisplayHuePicker(HuePicker huePicker) : base(0x95, 9)
    {
      Stream.Write(huePicker.Serial);
      Stream.Write((short)0);
      Stream.Write((short)huePicker.ItemID);
    }
  }

  public sealed class UnicodePrompt : Packet
  {
    public UnicodePrompt(Prompt prompt) : base(0xC2)
    {
      EnsureCapacity(21);

      Stream.Write(prompt.Serial);
      Stream.Write(prompt.Serial);
      Stream.Write(0);
      Stream.Write(0);
      Stream.Write((short)0);
    }
  }

  public sealed class ChangeCharacter : Packet
  {
    public ChangeCharacter(IAccount a) : base(0x81)
    {
      EnsureCapacity(305);

      var count = 0;

      for (var i = 0; i < a.Length; ++i)
        if (a[i] != null)
          ++count;

      Stream.Write((byte)count);
      Stream.Write((byte)0);

      for (var i = 0; i < a.Length; ++i)
        if (a[i] != null)
        {
          var name = a[i].Name;

          if (name == null)
            name = "-null-";
          else if ((name = name.Trim()).Length == 0)
            name = "-empty-";

          Stream.WriteAsciiFixed(name, 30);
          Stream.Fill(30); // password
        }
        else
        {
          Stream.Fill(60);
        }
    }
  }

  public sealed class DeathStatus : Packet
  {
    public static readonly Packet Dead = SetStatic(new DeathStatus(true));
    public static readonly Packet Alive = SetStatic(new DeathStatus(false));

    public DeathStatus(bool dead) : base(0x2C, 2)
    {
      Stream.Write((byte)(dead ? 0 : 2));
    }

    public static Packet Instantiate(bool dead) => dead ? Dead : Alive;
  }

  public sealed class SpeedControl : Packet
  {
    public static readonly Packet WalkSpeed = SetStatic(new SpeedControl(2));
    public static readonly Packet MountSpeed = SetStatic(new SpeedControl(1));
    public static readonly Packet Disable = SetStatic(new SpeedControl(0));

    public SpeedControl(int speedControl)
      : base(0xBF)
    {
      EnsureCapacity(3);

      Stream.Write((short)0x26);
      Stream.Write((byte)speedControl);
    }
  }

  public sealed class InvalidMapEnable : Packet
  {
    public InvalidMapEnable() : base(0xC6, 1)
    {
    }
  }

  public sealed class BondedStatus : Packet
  {
    public BondedStatus(int val1, Serial serial, int val2) : base(0xBF)
    {
      EnsureCapacity(11);

      Stream.Write((short)0x19);
      Stream.Write((byte)val1);
      Stream.Write(serial);
      Stream.Write((byte)val2);
    }
  }

  public sealed class ToggleSpecialAbility : Packet
  {
    public ToggleSpecialAbility(int abilityID, bool active)
      : base(0xBF)
    {
      EnsureCapacity(7);

      Stream.Write((short)0x25);

      Stream.Write((short)abilityID);
      Stream.Write(active);
    }
  }

  public sealed class DisplayItemListMenu : Packet
  {
    public DisplayItemListMenu(ItemListMenu menu) : base(0x7C)
    {
      EnsureCapacity(256);

      Stream.Write(((IMenu)menu).Serial);
      Stream.Write((short)0);

      var question = menu.Question;

      if (question == null)
      {
        Stream.Write((byte)0);
      }
      else
      {
        var questionLength = question.Length;
        Stream.Write((byte)questionLength);
        Stream.WriteAsciiFixed(question, questionLength);
      }

      var entries = menu.Entries;

      int entriesLength = (byte)entries.Length;

      Stream.Write((byte)entriesLength);

      for (var i = 0; i < entriesLength; ++i)
      {
        var e = entries[i];

        Stream.Write((ushort)e.ItemID);
        Stream.Write((short)e.Hue);

        var name = e.Name;

        if (name == null)
        {
          Stream.Write((byte)0);
        }
        else
        {
          var nameLength = name.Length;
          Stream.Write((byte)nameLength);
          Stream.WriteAsciiFixed(name, nameLength);
        }
      }
    }
  }

  public sealed class DisplayQuestionMenu : Packet
  {
    public DisplayQuestionMenu(QuestionMenu menu) : base(0x7C)
    {
      EnsureCapacity(256);

      Stream.Write(((IMenu)menu).Serial);
      Stream.Write((short)0);

      var question = menu.Question;

      if (question == null)
      {
        Stream.Write((byte)0);
      }
      else
      {
        var questionLength = question.Length;
        Stream.Write((byte)questionLength);
        Stream.WriteAsciiFixed(question, questionLength);
      }

      var answers = menu.Answers;

      int answersLength = (byte)answers.Length;

      Stream.Write((byte)answersLength);

      for (var i = 0; i < answersLength; ++i)
      {
        Stream.Write(0);

        var answer = answers[i];

        if (answer == null)
        {
          Stream.Write((byte)0);
        }
        else
        {
          var answerLength = answer.Length;
          Stream.Write((byte)answerLength);
          Stream.WriteAsciiFixed(answer, answerLength);
        }
      }
    }
  }

  public sealed class GlobalLightLevel : Packet
  {
    private static readonly GlobalLightLevel[] m_Cache = new GlobalLightLevel[0x100];

    public GlobalLightLevel(int level) : base(0x4F, 2)
    {
      Stream.Write((sbyte)level);
    }

    public static GlobalLightLevel Instantiate(int level)
    {
      var lvl = (byte)level;
      var p = m_Cache[lvl];

      if (p == null)
      {
        m_Cache[lvl] = p = new GlobalLightLevel(level);
        p.SetStatic();
      }

      return p;
    }
  }

  public sealed class PersonalLightLevel : Packet
  {
    public PersonalLightLevel(Mobile m) : this(m, m.LightLevel)
    {
    }

    public PersonalLightLevel(Mobile m, int level) : base(0x4E, 6)
    {
      Stream.Write(m.Serial);
      Stream.Write((sbyte)level);
    }
  }

  public sealed class PersonalLightLevelZero : Packet
  {
    public PersonalLightLevelZero(Mobile m) : base(0x4E, 6)
    {
      Stream.Write(m.Serial);
      Stream.Write((sbyte)0);
    }
  }

  [Flags]
  public enum CMEFlags
  {
    None = 0x00,
    Disabled = 0x01,
    Arrow = 0x02,
    Highlighted = 0x04,
    Colored = 0x20
  }

  public sealed class DisplayContextMenu : Packet
  {
    public DisplayContextMenu(ContextMenu menu) : base(0xBF)
    {
      var entries = menu.Entries;

      int length = (byte)entries.Length;

      EnsureCapacity(12 + length * 8);

      Stream.Write((short)0x14);
      Stream.Write((short)0x02);

      var target = menu.Target as IEntity;

      Stream.Write(target?.Serial ?? Serial.MinusOne);

      Stream.Write((byte)length);

      Point3D p;

      if (target is Mobile)
        p = target.Location;
      else if (target is Item item)
        p = item.GetWorldLocation();
      else
        p = Point3D.Zero;

      for (var i = 0; i < length; ++i)
      {
        var e = entries[i];

        Stream.Write(e.Number);
        Stream.Write((short)i);

        var range = e.Range;

        if (range == -1)
          range = 18;

        var flags = e.Enabled && menu.From.InRange(p, range) ? CMEFlags.None : CMEFlags.Disabled;

        flags |= e.Flags;

        Stream.Write((short)flags);
      }
    }
  }

  public sealed class DisplayContextMenuOld : Packet
  {
    public DisplayContextMenuOld(ContextMenu menu) : base(0xBF)
    {
      var entries = menu.Entries;

      int length = (byte)entries.Length;

      EnsureCapacity(12 + length * 8);

      Stream.Write((short)0x14);
      Stream.Write((short)0x01);

      var target = menu.Target as IEntity;

      Stream.Write(target?.Serial ?? Serial.MinusOne);

      Stream.Write((byte)length);

      Point3D p;

      if (target is Mobile)
        p = target.Location;
      else if (target is Item item)
        p = item.GetWorldLocation();
      else
        p = Point3D.Zero;

      for (var i = 0; i < length; ++i)
      {
        var e = entries[i];

        Stream.Write((short)i);
        Stream.Write((ushort)(e.Number - 3000000));

        var range = e.Range;

        if (range == -1)
          range = 18;

        var flags = e.Enabled && menu.From.InRange(p, range) ? CMEFlags.None : CMEFlags.Disabled;

        var color = e.Color & 0xFFFF;

        if (color != 0xFFFF)
          flags |= CMEFlags.Colored;

        flags |= e.Flags;

        Stream.Write((short)flags);

        if ((flags & CMEFlags.Colored) != 0)
          Stream.Write((short)color);
      }
    }
  }

  public sealed class DisplayProfile : Packet
  {
    public DisplayProfile(bool realSerial, Mobile m, string header, string body, string footer) : base(0xB8)
    {
      header ??= "";
      body ??= "";
      footer ??= "";

      EnsureCapacity(12 + header.Length + footer.Length * 2 + body.Length * 2);

      Stream.Write(realSerial ? m.Serial : Serial.Zero);
      Stream.WriteAsciiNull(header);
      Stream.WriteBigUniNull(footer);
      Stream.WriteBigUniNull(body);
    }
  }

  public sealed class CloseGump : Packet
  {
    public CloseGump(int typeID, int buttonID) : base(0xBF)
    {
      EnsureCapacity(13);

      Stream.Write((short)0x04);
      Stream.Write(typeID);
      Stream.Write(buttonID);
    }
  }

  public sealed class WorldItem : Packet
  {
    public WorldItem(Item item) : base(0x1A)
    {
      EnsureCapacity(20);

      // 14 base length
      // +2 - Amount
      // +2 - Hue
      // +1 - Flags

      var serial = item.Serial.Value;
      var itemID = item.ItemID & 0x3FFF;
      var amount = item.Amount;
      var loc = item.Location;
      var x = loc.m_X;
      var y = loc.m_Y;
      var hue = item.Hue;
      var flags = item.GetPacketFlags();
      var direction = (int)item.Direction;

      if (amount != 0)
        serial |= 0x80000000;
      else
        serial &= 0x7FFFFFFF;

      Stream.Write(serial);

      if (item is BaseMulti)
        Stream.Write((short)(itemID | 0x4000));
      else
        Stream.Write((short)itemID);

      if (amount != 0) Stream.Write((short)amount);

      x &= 0x7FFF;

      if (direction != 0) x |= 0x8000;

      Stream.Write((short)x);

      y &= 0x3FFF;

      if (hue != 0) y |= 0x8000;

      if (flags != 0) y |= 0x4000;

      Stream.Write((short)y);

      if (direction != 0)
        Stream.Write((byte)direction);

      Stream.Write((sbyte)loc.m_Z);

      if (hue != 0)
        Stream.Write((ushort)hue);

      if (flags != 0)
        Stream.Write((byte)flags);
    }
  }

  public sealed class WorldItemSA : Packet
  {
    public WorldItemSA(Item item) : base(0xF3, 24)
    {
      Stream.Write((short)0x1);

      var itemID = item.ItemID;

      if (item is BaseMulti)
      {
        Stream.Write((byte)0x02);

        Stream.Write(item.Serial);

        itemID &= 0x3FFF;

        Stream.Write((short)itemID);

        Stream.Write((byte)0);
        /*} else if ( ) {
          m_Stream.Write( (byte) 0x01 );

          m_Stream.Write( (int) item.Serial );

          m_Stream.Write( (short) itemID );

          m_Stream.Write( (byte) item.Direction );*/
      }
      else
      {
        Stream.Write((byte)0x00);

        Stream.Write(item.Serial);

        itemID &= 0x7FFF;

        Stream.Write((short)itemID);

        Stream.Write((byte)0);
      }

      var amount = item.Amount;
      Stream.Write((short)amount);
      Stream.Write((short)amount);

      var loc = item.Location;
      var x = loc.m_X & 0x7FFF;
      var y = loc.m_Y & 0x3FFF;
      Stream.Write((short)x);
      Stream.Write((short)y);
      Stream.Write((sbyte)loc.m_Z);

      Stream.Write((byte)item.Light);
      Stream.Write((short)item.Hue);
      Stream.Write((byte)item.GetPacketFlags());
    }
  }

  public sealed class WorldItemHS : Packet
  {
    public WorldItemHS(Item item) : base(0xF3, 26)
    {
      Stream.Write((short)0x1);

      var itemID = item.ItemID;

      if (item is BaseMulti)
      {
        Stream.Write((byte)0x02);

        Stream.Write(item.Serial);

        itemID &= 0x3FFF;

        Stream.Write((ushort)itemID);

        Stream.Write((byte)0);
        /*} else if ( ) {
          m_Stream.Write( (byte) 0x01 );

          m_Stream.Write( (int) item.Serial );

          m_Stream.Write( (ushort) itemID );

          m_Stream.Write( (byte) item.Direction );*/
      }
      else
      {
        Stream.Write((byte)0x00);

        Stream.Write(item.Serial);

        itemID &= 0xFFFF;

        Stream.Write((ushort)itemID);

        Stream.Write((byte)0);
      }

      var amount = item.Amount;
      Stream.Write((short)amount);
      Stream.Write((short)amount);

      var loc = item.Location;
      var x = loc.m_X & 0x7FFF;
      var y = loc.m_Y & 0x3FFF;
      Stream.Write((short)x);
      Stream.Write((short)y);
      Stream.Write((sbyte)loc.m_Z);

      Stream.Write((byte)item.Light);
      Stream.Write((short)item.Hue);
      Stream.Write((byte)item.GetPacketFlags());

      Stream.Write((short)0x00); // ??
    }
  }

  public sealed class LiftRej : Packet
  {
    public LiftRej(LRReason reason) : base(0x27, 2)
    {
      Stream.Write((byte)reason);
    }
  }

  public sealed class LogoutAck : Packet
  {
    public LogoutAck() : base(0xD1, 2)
    {
      Stream.Write((byte)0x01);
    }
  }

  public sealed class Weather : Packet
  {
    public Weather(int v1, int v2, int v3) : base(0x65, 4)
    {
      Stream.Write((byte)v1);
      Stream.Write((byte)v2);
      Stream.Write((byte)v3);
    }
  }

  /// <summary>
  ///   Causes the client to walk in a given direction. It does not send a movement request.
  /// </summary>
  public sealed class PlayerMove : Packet
  {
    public PlayerMove(Direction d) : base(0x97, 2)
    {
      Stream.Write((byte)d);

      // @4C63B0
    }
  }

  /// <summary>
  ///   Asks the client for it's version
  /// </summary>
  public sealed class ClientVersionReq : Packet
  {
    public ClientVersionReq() : base(0xBD)
    {
      EnsureCapacity(3);
    }
  }

  public enum EffectType
  {
    Moving = 0x00,
    Lightning = 0x01,
    FixedXYZ = 0x02,
    FixedFrom = 0x03
  }

  public class ParticleEffect : Packet
  {
    public ParticleEffect(EffectType type, Serial from, Serial to, int itemID, Point3D fromPoint, Point3D toPoint,
      int speed, int duration, bool fixedDirection, bool explode, int hue, int renderMode, int effect,
      int explodeEffect, int explodeSound, Serial serial, int layer, int unknown) : base(0xC7, 49)
    {
      Stream.Write((byte)type);
      Stream.Write(from);
      Stream.Write(to);
      Stream.Write((short)itemID);
      Stream.Write((short)fromPoint.m_X);
      Stream.Write((short)fromPoint.m_Y);
      Stream.Write((sbyte)fromPoint.m_Z);
      Stream.Write((short)toPoint.m_X);
      Stream.Write((short)toPoint.m_Y);
      Stream.Write((sbyte)toPoint.m_Z);
      Stream.Write((byte)speed);
      Stream.Write((byte)duration);
      Stream.Write((byte)0);
      Stream.Write((byte)0);
      Stream.Write(fixedDirection);
      Stream.Write(explode);
      Stream.Write(hue);
      Stream.Write(renderMode);
      Stream.Write((short)effect);
      Stream.Write((short)explodeEffect);
      Stream.Write((short)explodeSound);
      Stream.Write(serial);
      Stream.Write((byte)layer);
      Stream.Write((short)unknown);
    }

    public ParticleEffect(EffectType type, Serial from, Serial to, int itemID, IPoint3D fromPoint, IPoint3D toPoint,
      int speed, int duration, bool fixedDirection, bool explode, int hue, int renderMode, int effect,
      int explodeEffect, int explodeSound, Serial serial, int layer, int unknown) : base(0xC7, 49)
    {
      Stream.Write((byte)type);
      Stream.Write(from);
      Stream.Write(to);
      Stream.Write((short)itemID);
      Stream.Write((short)fromPoint.X);
      Stream.Write((short)fromPoint.Y);
      Stream.Write((sbyte)fromPoint.Z);
      Stream.Write((short)toPoint.X);
      Stream.Write((short)toPoint.Y);
      Stream.Write((sbyte)toPoint.Z);
      Stream.Write((byte)speed);
      Stream.Write((byte)duration);
      Stream.Write((byte)0);
      Stream.Write((byte)0);
      Stream.Write(fixedDirection);
      Stream.Write(explode);
      Stream.Write(hue);
      Stream.Write(renderMode);
      Stream.Write((short)effect);
      Stream.Write((short)explodeEffect);
      Stream.Write((short)explodeSound);
      Stream.Write(serial);
      Stream.Write((byte)layer);
      Stream.Write((short)unknown);
    }
  }

  public class HuedEffect : Packet
  {
    public HuedEffect(EffectType type, Serial from, Serial to, int itemID, Point3D fromPoint, Point3D toPoint, int speed,
      int duration, bool fixedDirection, bool explode, int hue, int renderMode) : base(0xC0, 36)
    {
      Stream.Write((byte)type);
      Stream.Write(from);
      Stream.Write(to);
      Stream.Write((short)itemID);
      Stream.Write((short)fromPoint.m_X);
      Stream.Write((short)fromPoint.m_Y);
      Stream.Write((sbyte)fromPoint.m_Z);
      Stream.Write((short)toPoint.m_X);
      Stream.Write((short)toPoint.m_Y);
      Stream.Write((sbyte)toPoint.m_Z);
      Stream.Write((byte)speed);
      Stream.Write((byte)duration);
      Stream.Write((byte)0);
      Stream.Write((byte)0);
      Stream.Write(fixedDirection);
      Stream.Write(explode);
      Stream.Write(hue);
      Stream.Write(renderMode);
    }

    public HuedEffect(EffectType type, Serial from, Serial to, int itemID, IPoint3D fromPoint, IPoint3D toPoint,
      int speed, int duration, bool fixedDirection, bool explode, int hue, int renderMode) : base(0xC0, 36)
    {
      Stream.Write((byte)type);
      Stream.Write(from);
      Stream.Write(to);
      Stream.Write((short)itemID);
      Stream.Write((short)fromPoint.X);
      Stream.Write((short)fromPoint.Y);
      Stream.Write((sbyte)fromPoint.Z);
      Stream.Write((short)toPoint.X);
      Stream.Write((short)toPoint.Y);
      Stream.Write((sbyte)toPoint.Z);
      Stream.Write((byte)speed);
      Stream.Write((byte)duration);
      Stream.Write((byte)0);
      Stream.Write((byte)0);
      Stream.Write(fixedDirection);
      Stream.Write(explode);
      Stream.Write(hue);
      Stream.Write(renderMode);
    }
  }

  public sealed class TargetParticleEffect : ParticleEffect
  {
    public TargetParticleEffect(IEntity e, int itemID, int speed, int duration, int hue, int renderMode, int effect,
      int layer, int unknown) : base(EffectType.FixedFrom, e.Serial, Serial.Zero, itemID, e.Location, e.Location,
      speed, duration, true, false, hue, renderMode, effect, 1, 0, e.Serial, layer, unknown)
    {
    }
  }

  public sealed class TargetEffect : HuedEffect
  {
    public TargetEffect(IEntity e, int itemID, int speed, int duration, int hue, int renderMode) : base(
      EffectType.FixedFrom, e.Serial, Serial.Zero, itemID, e.Location, e.Location, speed, duration, true, false, hue,
      renderMode)
    {
    }
  }

  public sealed class LocationParticleEffect : ParticleEffect
  {
    public LocationParticleEffect(IEntity e, int itemID, int speed, int duration, int hue, int renderMode, int effect,
      int unknown) : base(EffectType.FixedXYZ, e.Serial, Serial.Zero, itemID, e.Location, e.Location, speed, duration,
      true, false, hue, renderMode, effect, 1, 0, e.Serial, 255, unknown)
    {
    }
  }

  public sealed class LocationEffect : HuedEffect
  {
    public LocationEffect(IPoint3D p, int itemID, int speed, int duration, int hue, int renderMode) : base(
      EffectType.FixedXYZ, Serial.Zero, Serial.Zero, itemID, p, p, speed, duration, true, false, hue, renderMode)
    {
    }
  }

  public sealed class MovingParticleEffect : ParticleEffect
  {
    public MovingParticleEffect(IEntity from, IEntity to, int itemID, int speed, int duration, bool fixedDirection,
      bool explodes, int hue, int renderMode, int effect, int explodeEffect, int explodeSound, EffectLayer layer,
      int unknown) : base(EffectType.Moving, from.Serial, to.Serial, itemID, from.Location, to.Location, speed,
      duration, fixedDirection, explodes, hue, renderMode, effect, explodeEffect, explodeSound, Serial.Zero,
      (int)layer, unknown)
    {
    }
  }

  public sealed class MovingEffect : HuedEffect
  {
    public MovingEffect(IEntity from, IEntity to, int itemID, int speed, int duration, bool fixedDirection,
      bool explodes, int hue, int renderMode) : base(EffectType.Moving, from.Serial, to.Serial, itemID, from.Location,
      to.Location, speed, duration, fixedDirection, explodes, hue, renderMode)
    {
    }
  }

  public enum ScreenEffectType
  {
    FadeOut = 0x00,
    FadeIn = 0x01,
    LightFlash = 0x02,
    FadeInOut = 0x03,
    DarkFlash = 0x04
  }

  public class ScreenEffect : Packet
  {
    public ScreenEffect(ScreenEffectType type)
      : base(0x70, 28)
    {
      Stream.Write((byte)0x04);
      Stream.Fill(8);
      Stream.Write((short)type);
      Stream.Fill(16);
    }
  }

  public sealed class ScreenFadeOut : ScreenEffect
  {
    public static readonly Packet Instance = SetStatic(new ScreenFadeOut());

    public ScreenFadeOut()
      : base(ScreenEffectType.FadeOut)
    {
    }
  }

  public sealed class ScreenFadeIn : ScreenEffect
  {
    public static readonly Packet Instance = SetStatic(new ScreenFadeIn());

    public ScreenFadeIn()
      : base(ScreenEffectType.FadeIn)
    {
    }
  }

  public sealed class ScreenFadeInOut : ScreenEffect
  {
    public static readonly Packet Instance = SetStatic(new ScreenFadeInOut());

    public ScreenFadeInOut()
      : base(ScreenEffectType.FadeInOut)
    {
    }
  }

  public sealed class ScreenLightFlash : ScreenEffect
  {
    public static readonly Packet Instance = SetStatic(new ScreenLightFlash());

    public ScreenLightFlash()
      : base(ScreenEffectType.LightFlash)
    {
    }
  }

  public sealed class ScreenDarkFlash : ScreenEffect
  {
    public static readonly Packet Instance = SetStatic(new ScreenDarkFlash());

    public ScreenDarkFlash()
      : base(ScreenEffectType.DarkFlash)
    {
    }
  }

  public enum DeleteResultType
  {
    PasswordInvalid,
    CharNotExist,
    CharBeingPlayed,
    CharTooYoung,
    CharQueued,
    BadRequest
  }

  public sealed class DeleteResult : Packet
  {
    public DeleteResult(DeleteResultType res) : base(0x85, 2)
    {
      Stream.Write((byte)res);
    }
  }

  /*public sealed class MovingEffect : Packet
  {
    public MovingEffect( IEntity from, IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool turn, int hue, int renderMode ) : base( 0xC0, 36 )
    {
      m_Stream.Write( (byte) 0x00 );
      m_Stream.Write( (int) from.Serial );
      m_Stream.Write( (int) to.Serial );
      m_Stream.Write( (short) itemID );
      m_Stream.Write( (short) from.Location.m_X );
      m_Stream.Write( (short) from.Location.m_Y );
      m_Stream.Write( (sbyte) from.Location.m_Z );
      m_Stream.Write( (short) to.Location.m_X );
      m_Stream.Write( (short) to.Location.m_Y );
      m_Stream.Write( (sbyte) to.Location.m_Z );
      m_Stream.Write( (byte) speed );
      m_Stream.Write( (byte) duration );
      m_Stream.Write( (byte) 0 );
      m_Stream.Write( (byte) 0 );
      m_Stream.Write( (bool) fixedDirection );
      m_Stream.Write( (bool) turn );
      m_Stream.Write( (int) hue );
      m_Stream.Write( (int) renderMode );
    }
  }*/

  /*public sealed class LocationEffect : Packet
  {
    public LocationEffect( IPoint3D p, int itemID, int duration, int hue, int renderMode ) : base( 0xC0, 36 )
    {
      m_Stream.Write( (byte) 0x02 );
      m_Stream.Write( (int) Serial.Zero );
      m_Stream.Write( (int) Serial.Zero );
      m_Stream.Write( (short) itemID );
      m_Stream.Write( (short) p.X );
      m_Stream.Write( (short) p.Y );
      m_Stream.Write( (sbyte) p.Z );
      m_Stream.Write( (short) p.X );
      m_Stream.Write( (short) p.Y );
      m_Stream.Write( (sbyte) p.Z );
      m_Stream.Write( (byte) 10 );
      m_Stream.Write( (byte) duration );
      m_Stream.Write( (byte) 0 );
      m_Stream.Write( (byte) 0 );
      m_Stream.Write( (byte) 1 );
      m_Stream.Write( (byte) 0 );
      m_Stream.Write( (int) hue );
      m_Stream.Write( (int) renderMode );
    }
  }*/

  public sealed class BoltEffect : Packet
  {
    public BoltEffect(IEntity target, int hue) : base(0xC0, 36)
    {
      Stream.Write((byte)0x01); // type
      Stream.Write(target.Serial);
      Stream.Write(Serial.Zero);
      Stream.Write((short)0); // itemID
      Stream.Write((short)target.X);
      Stream.Write((short)target.Y);
      Stream.Write((sbyte)target.Z);
      Stream.Write((short)target.X);
      Stream.Write((short)target.Y);
      Stream.Write((sbyte)target.Z);
      Stream.Write((byte)0); // speed
      Stream.Write((byte)0); // duration
      Stream.Write((short)0); // unk
      Stream.Write(false); // fixed direction
      Stream.Write(false); // explode
      Stream.Write(hue);
      Stream.Write(0); // render mode
    }
  }

  public sealed class DisplaySpellbook : Packet
  {
    public DisplaySpellbook(Item book) : base(0x24, 7)
    {
      Stream.Write(book.Serial);
      Stream.Write((short)-1);
    }
  }

  public sealed class DisplaySpellbookHS : Packet
  {
    public DisplaySpellbookHS(Item book) : base(0x24, 9)
    {
      Stream.Write(book.Serial);
      Stream.Write((short)-1);
      Stream.Write((short)0x7D);
    }
  }

  public sealed class NewSpellbookContent : Packet
  {
    public NewSpellbookContent(Item item, int graphic, int offset, ulong content) : base(0xBF)
    {
      EnsureCapacity(23);

      Stream.Write((short)0x1B);
      Stream.Write((short)0x01);

      Stream.Write(item.Serial);
      Stream.Write((short)graphic);
      Stream.Write((short)offset);

      for (var i = 0; i < 8; ++i)
        Stream.Write((byte)(content >> (i * 8)));
    }
  }

  public sealed class SpellbookContent : Packet
  {
    public SpellbookContent(int count, int offset, ulong content, Item item) : base(0x3C)
    {
      EnsureCapacity(5 + count * 19);

      var written = 0;

      Stream.Write((ushort)0);

      ulong mask = 1;

      for (var i = 0; i < 64; ++i, mask <<= 1)
        if ((content & mask) != 0)
        {
          Stream.Write(0x7FFFFFFF - i);
          Stream.Write((ushort)0);
          Stream.Write((byte)0);
          Stream.Write((ushort)(i + offset));
          Stream.Write((short)0);
          Stream.Write((short)0);
          Stream.Write(item.Serial);
          Stream.Write((short)0);

          ++written;
        }

      Stream.Seek(3, SeekOrigin.Begin);
      Stream.Write((ushort)written);
    }
  }

  public sealed class SpellbookContent6017 : Packet
  {
    public SpellbookContent6017(int count, int offset, ulong content, Item item) : base(0x3C)
    {
      EnsureCapacity(5 + count * 20);

      var written = 0;

      Stream.Write((ushort)0);

      ulong mask = 1;

      for (var i = 0; i < 64; ++i, mask <<= 1)
        if ((content & mask) != 0)
        {
          Stream.Write(0x7FFFFFFF - i);
          Stream.Write((ushort)0);
          Stream.Write((byte)0);
          Stream.Write((ushort)(i + offset));
          Stream.Write((short)0);
          Stream.Write((short)0);
          Stream.Write((byte)0); // Grid Location?
          Stream.Write(item.Serial);
          Stream.Write((short)0);

          ++written;
        }

      Stream.Seek(3, SeekOrigin.Begin);
      Stream.Write((ushort)written);
    }
  }

  public sealed class ContainerDisplay : Packet
  {
    public ContainerDisplay(Container c) : base(0x24, 7)
    {
      Stream.Write(c.Serial);
      Stream.Write((short)c.GumpID);
    }
  }

  public sealed class ContainerDisplayHS : Packet
  {
    public ContainerDisplayHS(Container c) : base(0x24, 9)
    {
      Stream.Write(c.Serial);
      Stream.Write((short)c.GumpID);
      Stream.Write((short)0x7D);
    }
  }

  public sealed class ContainerContentUpdate : Packet
  {
    public ContainerContentUpdate(Item item) : base(0x25, 20)
    {
      Serial parentSerial;

      if (item.Parent is Item parentItem)
      {
        parentSerial = parentItem.Serial;
      }
      else
      {
        Console.WriteLine("Warning: ContainerContentUpdate on item with !(parent is Item)");
        parentSerial = Serial.Zero;
      }

      Stream.Write(item.Serial);
      Stream.Write((ushort)item.ItemID);
      Stream.Write((byte)0); // signed, itemID offset
      Stream.Write((ushort)Math.Min(item.Amount, ushort.MaxValue));
      Stream.Write((short)item.X);
      Stream.Write((short)item.Y);
      Stream.Write(parentSerial);
      Stream.Write((ushort)(item.QuestItem ? Item.QuestItemHue : item.Hue));
    }
  }

  public sealed class ContainerContentUpdate6017 : Packet
  {
    public ContainerContentUpdate6017(Item item) : base(0x25, 21)
    {
      Serial parentSerial;

      if (item.Parent is Item parentItem)
      {
        parentSerial = parentItem.Serial;
      }
      else
      {
        Console.WriteLine("Warning: ContainerContentUpdate on item with !(parent is Item)");
        parentSerial = Serial.Zero;
      }

      Stream.Write(item.Serial);
      Stream.Write((ushort)item.ItemID);
      Stream.Write((byte)0); // signed, itemID offset
      Stream.Write((ushort)Math.Min(item.Amount, ushort.MaxValue));
      Stream.Write((short)item.X);
      Stream.Write((short)item.Y);
      Stream.Write((byte)0); // Grid Location?
      Stream.Write(parentSerial);
      Stream.Write((ushort)(item.QuestItem ? Item.QuestItemHue : item.Hue));
    }
  }

  public sealed class ContainerContent : Packet
  {
    public ContainerContent(Mobile beholder, Item beheld) : base(0x3C)
    {
      var items = beheld.Items;
      var count = items.Count;

      EnsureCapacity(5 + count * 19);

      var pos = Stream.Position;

      var written = 0;

      Stream.Write((ushort)0);

      for (var i = 0; i < count; ++i)
      {
        var child = items[i];

        if (!child.Deleted && beholder.CanSee(child))
        {
          var loc = child.Location;

          Stream.Write(child.Serial);
          Stream.Write((ushort)child.ItemID);
          Stream.Write((byte)0); // signed, itemID offset
          Stream.Write((ushort)Math.Min(child.Amount, ushort.MaxValue));
          Stream.Write((short)loc.m_X);
          Stream.Write((short)loc.m_Y);
          Stream.Write(beheld.Serial);
          Stream.Write((ushort)(child.QuestItem ? Item.QuestItemHue : child.Hue));

          ++written;
        }
      }

      Stream.Seek(pos, SeekOrigin.Begin);
      Stream.Write((ushort)written);
    }
  }

  public sealed class ContainerContent6017 : Packet
  {
    public ContainerContent6017(Mobile beholder, Item beheld) : base(0x3C)
    {
      var items = beheld.Items;
      var count = items.Count;

      EnsureCapacity(5 + count * 20);

      var pos = Stream.Position;

      var written = 0;

      Stream.Write((ushort)0);

      for (var i = 0; i < count; ++i)
      {
        var child = items[i];

        if (!child.Deleted && beholder.CanSee(child))
        {
          var loc = child.Location;

          Stream.Write(child.Serial);
          Stream.Write((ushort)child.ItemID);
          Stream.Write((byte)0); // signed, itemID offset
          Stream.Write((ushort)Math.Min(child.Amount, ushort.MaxValue));
          Stream.Write((short)loc.m_X);
          Stream.Write((short)loc.m_Y);
          Stream.Write((byte)0); // Grid Location?
          Stream.Write(beheld.Serial);
          Stream.Write((ushort)(child.QuestItem ? Item.QuestItemHue : child.Hue));

          ++written;
        }
      }

      Stream.Seek(pos, SeekOrigin.Begin);
      Stream.Write((ushort)written);
    }
  }

  public sealed class SetWarMode : Packet
  {
    public static readonly Packet InWarMode = SetStatic(new SetWarMode(true));
    public static readonly Packet InPeaceMode = SetStatic(new SetWarMode(false));

    public SetWarMode(bool mode) : base(0x72, 5)
    {
      Stream.Write(mode);
      Stream.Write((byte)0x00);
      Stream.Write((byte)0x32);
      Stream.Write((byte)0x00);
      // m_Stream.Fill();
    }

    public static Packet Instantiate(bool mode) => mode ? InWarMode : InPeaceMode;
  }

  public sealed class Swing : Packet
  {
    public Swing(int flag, Mobile attacker, Mobile defender) : base(0x2F, 10)
    {
      Stream.Write((byte)flag);
      Stream.Write(attacker.Serial);
      Stream.Write(defender.Serial);
    }
  }

  public sealed class NullFastwalkStack : Packet
  {
    public NullFastwalkStack() : base(0xBF)
    {
      EnsureCapacity(256);
      Stream.Write((short)0x1);
      Stream.Write(0x0);
      Stream.Write(0x0);
      Stream.Write(0x0);
      Stream.Write(0x0);
      Stream.Write(0x0);
      Stream.Write(0x0);
    }
  }

  public sealed class RemoveEntity : Packet
  {
    public RemoveEntity(IEntity entity) : base(0x1D, 5)
    {
      Stream.Write(entity.Serial);
    }
  }

  public sealed class ServerChange : Packet
  {
    public ServerChange(Mobile m, Map map) : base(0x76, 16)
    {
      Stream.Write((short)m.X);
      Stream.Write((short)m.Y);
      Stream.Write((short)m.Z);
      Stream.Write((byte)0);
      Stream.Write((short)0);
      Stream.Write((short)0);
      Stream.Write((short)map.Width);
      Stream.Write((short)map.Height);
    }
  }

  public sealed class SkillUpdate : Packet
  {
    public SkillUpdate(Skills skills) : base(0x3A)
    {
      EnsureCapacity(6 + skills.Length * 9);

      Stream.Write((byte)0x02); // type: absolute, capped

      for (var i = 0; i < skills.Length; ++i)
      {
        var s = skills[i];

        var v = s.NonRacialValue;
        var uv = (int)(v * 10);

        if (uv < 0)
          uv = 0;
        else if (uv >= 0x10000)
          uv = 0xFFFF;

        Stream.Write((ushort)(s.Info.SkillID + 1));
        Stream.Write((ushort)uv);
        Stream.Write((ushort)s.BaseFixedPoint);
        Stream.Write((byte)s.Lock);
        Stream.Write((ushort)s.CapFixedPoint);
      }

      Stream.Write((short)0); // terminate
    }
  }

  public sealed class Sequence : Packet
  {
    public Sequence(int num) : base(0x7B, 2)
    {
      Stream.Write((byte)num);
    }
  }

  public sealed class SkillChange : Packet
  {
    public SkillChange(Skill skill) : base(0x3A)
    {
      EnsureCapacity(13);

      var v = skill.NonRacialValue;
      var uv = (int)(v * 10);

      if (uv < 0)
        uv = 0;
      else if (uv >= 0x10000)
        uv = 0xFFFF;

      Stream.Write((byte)0xDF); // type: delta, capped
      Stream.Write((ushort)skill.Info.SkillID);
      Stream.Write((ushort)uv);
      Stream.Write((ushort)skill.BaseFixedPoint);
      Stream.Write((byte)skill.Lock);
      Stream.Write((ushort)skill.CapFixedPoint);

      /*m_Stream.Write( (short) skill.Info.SkillID );
      m_Stream.Write( (short) (skill.Value * 10.0) );
      m_Stream.Write( (short) (skill.Base * 10.0) );
      m_Stream.Write( (byte) skill.Lock );
      m_Stream.Write( (short) skill.CapFixedPoint );*/
    }
  }

  public sealed class LaunchBrowser : Packet
  {
    public LaunchBrowser(Uri uri) : this(uri.AbsoluteUri)
    {
    }

    public LaunchBrowser(string url) : base(0xA5)
    {
      url ??= "";

      EnsureCapacity(4 + url.Length);

      Stream.WriteAsciiNull(url);
    }
  }

  public sealed class MessageLocalized : Packet
  {
    private static readonly MessageLocalized[] m_Cache_IntLoc = new MessageLocalized[15000];
    private static readonly MessageLocalized[] m_Cache_CliLoc = new MessageLocalized[100000];
    private static readonly MessageLocalized[] m_Cache_CliLocCmp = new MessageLocalized[5000];

    public MessageLocalized(Serial serial, int graphic, MessageType type, int hue, int font, int number, string name,
      string args) : base(0xC1)
    {
      name ??= "";
      args ??= "";

      if (hue == 0)
        hue = 0x3B2;

      EnsureCapacity(50 + args.Length * 2);

      Stream.Write(serial);
      Stream.Write((short)graphic);
      Stream.Write((byte)type);
      Stream.Write((short)hue);
      Stream.Write((short)font);
      Stream.Write(number);
      Stream.WriteAsciiFixed(name, 30);
      Stream.WriteLittleUniNull(args);
    }

    public static MessageLocalized InstantiateGeneric(int number)
    {
      MessageLocalized[] cache = null;
      var index = 0;

      if (number >= 3000000)
      {
        cache = m_Cache_IntLoc;
        index = number - 3000000;
      }
      else if (number >= 1000000)
      {
        cache = m_Cache_CliLoc;
        index = number - 1000000;
      }
      else if (number >= 500000)
      {
        cache = m_Cache_CliLocCmp;
        index = number - 500000;
      }

      MessageLocalized p;

      if (cache != null && index >= 0 && index < cache.Length)
      {
        p = cache[index];

        if (p == null)
        {
          cache[index] = p = new MessageLocalized(Serial.MinusOne, -1, MessageType.Regular, 0x3B2, 3, number,
            "System", "");
          p.SetStatic();
        }
      }
      else
      {
        p = new MessageLocalized(Serial.MinusOne, -1, MessageType.Regular, 0x3B2, 3, number, "System", "");
      }

      return p;
    }
  }

  public sealed class MobileMoving : Packet
  {
    public MobileMoving(Mobile m, int noto) : base(0x77, 17)
    {
      var loc = m.Location;

      var hue = m.Hue;

      if (m.SolidHueOverride >= 0)
        hue = m.SolidHueOverride;

      Stream.Write(m.Serial);
      Stream.Write((short)m.Body);
      Stream.Write((short)loc.m_X);
      Stream.Write((short)loc.m_Y);
      Stream.Write((sbyte)loc.m_Z);
      Stream.Write((byte)m.Direction);
      Stream.Write((short)hue);
      Stream.Write((byte)m.GetPacketFlags());
      Stream.Write((byte)noto);
    }
  }

  // Pre-7.0.0.0 Mobile Moving
  public sealed class MobileMovingOld : Packet
  {
    public MobileMovingOld(Mobile m, int noto) : base(0x77, 17)
    {
      var loc = m.Location;

      var hue = m.Hue;

      if (m.SolidHueOverride >= 0)
        hue = m.SolidHueOverride;

      Stream.Write(m.Serial);
      Stream.Write((short)m.Body);
      Stream.Write((short)loc.m_X);
      Stream.Write((short)loc.m_Y);
      Stream.Write((sbyte)loc.m_Z);
      Stream.Write((byte)m.Direction);
      Stream.Write((short)hue);
      Stream.Write((byte)m.GetOldPacketFlags());
      Stream.Write((byte)noto);
    }
  }

  public sealed class MultiTargetReqHS : Packet
  {
    public MultiTargetReqHS(MultiTarget t) : base(0x99, 30)
    {
      Stream.Write(t.AllowGround);
      Stream.Write(t.TargetID);
      Stream.Write((byte)t.Flags);

      Stream.Fill();

      Stream.Seek(18, SeekOrigin.Begin);
      Stream.Write((short)t.MultiID);
      Stream.Write((short)t.Offset.X);
      Stream.Write((short)t.Offset.Y);
      Stream.Write((short)t.Offset.Z);

      // DWORD Hue
    }
  }

  public sealed class MultiTargetReq : Packet
  {
    public MultiTargetReq(MultiTarget t) : base(0x99, 26)
    {
      Stream.Write(t.AllowGround);
      Stream.Write(t.TargetID);
      Stream.Write((byte)t.Flags);

      Stream.Fill();

      Stream.Seek(18, SeekOrigin.Begin);
      Stream.Write((short)t.MultiID);
      Stream.Write((short)t.Offset.X);
      Stream.Write((short)t.Offset.Y);
      Stream.Write((short)t.Offset.Z);
    }
  }

  public sealed class CancelTarget : Packet
  {
    public static readonly Packet Instance = SetStatic(new CancelTarget());

    public CancelTarget() : base(0x6C, 19)
    {
      Stream.Write((byte)0);
      Stream.Write(0);
      Stream.Write((byte)3);
      Stream.Fill();
    }
  }

  public sealed class TargetReq : Packet
  {
    public TargetReq(Target t) : base(0x6C, 19)
    {
      Stream.Write(t.AllowGround);
      Stream.Write(t.TargetID);
      Stream.Write((byte)t.Flags);
      Stream.Fill();
    }
  }

  public sealed class DragEffect : Packet
  {
    public DragEffect(IEntity src, IEntity trg, int itemID, int hue, int amount) : base(0x23, 26)
    {
      Stream.Write((short)itemID);
      Stream.Write((byte)0);
      Stream.Write((short)hue);
      Stream.Write((short)amount);
      Stream.Write(src.Serial);
      Stream.Write((short)src.X);
      Stream.Write((short)src.Y);
      Stream.Write((sbyte)src.Z);
      Stream.Write(trg.Serial);
      Stream.Write((short)trg.X);
      Stream.Write((short)trg.Y);
      Stream.Write((sbyte)trg.Z);
    }
  }

  public interface IGumpWriter
  {
    int TextEntries { get; set; }
    int Switches { get; set; }

    void AppendLayout(bool val);
    void AppendLayout(int val);
    void AppendLayout(uint val);
    void AppendLayoutNS(int val);
    void AppendLayout(string text);
    void AppendLayout(byte[] buffer);
    void WriteStrings(List<string> strings);
    void Flush();
  }

  public sealed class DisplayGumpPacked : Packet, IGumpWriter
  {
    private static readonly byte[] m_True = Gump.StringToBuffer(" 1");
    private static readonly byte[] m_False = Gump.StringToBuffer(" 0");

    private static readonly byte[] m_BeginTextSeparator = Gump.StringToBuffer(" @");
    private static readonly byte[] m_EndTextSeparator = Gump.StringToBuffer("@");

    private static readonly byte[] m_Buffer = new byte[48];

    private readonly Gump m_Gump;

    private readonly PacketWriter m_Layout;

    private int m_StringCount;
    private readonly PacketWriter m_Strings;

    static DisplayGumpPacked() => m_Buffer[0] = (byte)' ';

    public DisplayGumpPacked(Gump gump)
      : base(0xDD)
    {
      m_Gump = gump;

      m_Layout = PacketWriter.CreateInstance(8192);
      m_Strings = PacketWriter.CreateInstance(8192);
    }

    public int TextEntries { get; set; }

    public int Switches { get; set; }

    public void AppendLayout(bool val)
    {
      AppendLayout(val ? m_True : m_False);
    }

    public void AppendLayout(int val)
    {
      var toString = val.ToString();
      var bytes = Encoding.ASCII.GetBytes(toString, 0, toString.Length, m_Buffer, 1) + 1;

      m_Layout.Write(m_Buffer, 0, bytes);
    }

    public void AppendLayout(uint val)
    {
      var toString = val.ToString();
      var bytes = Encoding.ASCII.GetBytes(toString, 0, toString.Length, m_Buffer, 1) + 1;

      m_Layout.Write(m_Buffer, 0, bytes);
    }

    public void AppendLayoutNS(int val)
    {
      var toString = val.ToString();
      var bytes = Encoding.ASCII.GetBytes(toString, 0, toString.Length, m_Buffer, 1);

      m_Layout.Write(m_Buffer, 1, bytes);
    }

    public void AppendLayout(string text)
    {
      AppendLayout(m_BeginTextSeparator);

      m_Layout.WriteAsciiFixed(text, text.Length);

      AppendLayout(m_EndTextSeparator);
    }

    public void AppendLayout(byte[] buffer)
    {
      m_Layout.Write(buffer, 0, buffer.Length);
    }

    public void WriteStrings(List<string> strings)
    {
      m_StringCount = strings.Count;

      for (var i = 0; i < strings.Count; ++i)
      {
        var v = strings[i] ?? "";

        m_Strings.Write((ushort)v.Length);
        m_Strings.WriteBigUniFixed(v, v.Length);
      }
    }

    public void Flush()
    {
      EnsureCapacity(28 + (int)m_Layout.Length + (int)m_Strings.Length);

      Stream.Write(m_Gump.Serial);
      Stream.Write(m_Gump.TypeID);
      Stream.Write(m_Gump.X);
      Stream.Write(m_Gump.Y);

      // Note: layout MUST be null terminated (don't listen to krrios)
      m_Layout.Write((byte)0);
      WritePacked(m_Layout);

      Stream.Write(m_StringCount);

      WritePacked(m_Strings);

      PacketWriter.ReleaseInstance(m_Layout);
      PacketWriter.ReleaseInstance(m_Strings);
    }

    private void WritePacked(PacketWriter src)
    {
      var buffer = src.UnderlyingStream.GetBuffer();
      var length = (int)src.Length;

      if (length == 0)
      {
        Stream.Write(0);
        return;
      }

      var wantLength = 1 + buffer.Length * 1024 / 1000;

      wantLength += 4095;
      wantLength &= ~4095;

      var packBuffer = ArrayPool<byte>.Shared.Rent(wantLength);

      var packLength = wantLength;

      ZLib.Pack(packBuffer, ref packLength, buffer, length, ZLibQuality.Default);

      Stream.Write(4 + packLength);
      Stream.Write(length);
      Stream.Write(packBuffer, 0, packLength);

      ArrayPool<byte>.Shared.Return(packBuffer);
    }
  }

  public sealed class DisplayGumpFast : Packet, IGumpWriter
  {
    private static readonly byte[] m_True = Gump.StringToBuffer(" 1");
    private static readonly byte[] m_False = Gump.StringToBuffer(" 0");

    private static readonly byte[] m_BeginTextSeparator = Gump.StringToBuffer(" @");
    private static readonly byte[] m_EndTextSeparator = Gump.StringToBuffer("@");

    private readonly byte[] m_Buffer = new byte[48];
    private int m_LayoutLength;

    public DisplayGumpFast(Gump g) : base(0xB0)
    {
      m_Buffer[0] = (byte)' ';

      EnsureCapacity(4096);

      Stream.Write(g.Serial);
      Stream.Write(g.TypeID);
      Stream.Write(g.X);
      Stream.Write(g.Y);
      Stream.Write((ushort)0xFFFF);
    }

    public int TextEntries { get; set; }

    public int Switches { get; set; }

    public void AppendLayout(bool val)
    {
      AppendLayout(val ? m_True : m_False);
    }

    public void AppendLayout(int val)
    {
      var toString = val.ToString();
      var bytes = Encoding.ASCII.GetBytes(toString, 0, toString.Length, m_Buffer, 1) + 1;

      Stream.Write(m_Buffer, 0, bytes);
      m_LayoutLength += bytes;
    }

    public void AppendLayout(uint val)
    {
      var toString = val.ToString();
      var bytes = Encoding.ASCII.GetBytes(toString, 0, toString.Length, m_Buffer, 1) + 1;

      Stream.Write(m_Buffer, 0, bytes);
      m_LayoutLength += bytes;
    }

    public void AppendLayoutNS(int val)
    {
      var toString = val.ToString();
      var bytes = Encoding.ASCII.GetBytes(toString, 0, toString.Length, m_Buffer, 1);

      Stream.Write(m_Buffer, 1, bytes);
      m_LayoutLength += bytes;
    }

    public void AppendLayout(string text)
    {
      AppendLayout(m_BeginTextSeparator);

      var length = text.Length;
      Stream.WriteAsciiFixed(text, length);
      m_LayoutLength += length;

      AppendLayout(m_EndTextSeparator);
    }

    public void AppendLayout(byte[] buffer)
    {
      var length = buffer.Length;
      Stream.Write(buffer, 0, length);
      m_LayoutLength += length;
    }

    public void WriteStrings(List<string> text)
    {
      Stream.Seek(19, SeekOrigin.Begin);
      Stream.Write((ushort)m_LayoutLength);
      Stream.Seek(0, SeekOrigin.End);

      Stream.Write((ushort)text.Count);

      for (var i = 0; i < text.Count; ++i)
      {
        var v = text[i] ?? "";

        int length = (ushort)v.Length;

        Stream.Write((ushort)length);
        Stream.WriteBigUniFixed(v, length);
      }
    }

    public void Flush()
    {
    }
  }

  public sealed class DisplayGump : Packet
  {
    public DisplayGump(Gump g, string layout, string[] text) : base(0xB0)
    {
      layout ??= "";

      EnsureCapacity(256);

      Stream.Write(g.Serial);
      Stream.Write(g.TypeID);
      Stream.Write(g.X);
      Stream.Write(g.Y);
      Stream.Write((ushort)(layout.Length + 1));
      Stream.WriteAsciiNull(layout);

      Stream.Write((ushort)text.Length);

      for (var i = 0; i < text.Length; ++i)
      {
        var v = text[i] ?? "";

        var length = (ushort)v.Length;

        Stream.Write(length);
        Stream.WriteBigUniFixed(v, length);
      }
    }
  }

  public sealed class DisplayPaperdoll : Packet
  {
    public DisplayPaperdoll(Mobile m, string text, bool canLift) : base(0x88, 66)
    {
      byte flags = 0x00;

      if (m.Warmode)
        flags |= 0x01;

      if (canLift)
        flags |= 0x02;

      Stream.Write(m.Serial);
      Stream.WriteAsciiFixed(text, 60);
      Stream.Write(flags);
    }
  }

  public sealed class PopupMessage : Packet
  {
    public PopupMessage(PMMessage msg) : base(0x53, 2)
    {
      Stream.Write((byte)msg);
    }
  }

  public sealed class PlaySound : Packet
  {
    public PlaySound(int soundID, IPoint3D target) : base(0x54, 12)
    {
      Stream.Write((byte)1); // flags
      Stream.Write((short)soundID);
      Stream.Write((short)0); // volume
      Stream.Write((short)target.X);
      Stream.Write((short)target.Y);
      Stream.Write((short)target.Z);
    }
  }

  public sealed class PlayMusic : Packet
  {
    public static readonly Packet InvalidInstance = SetStatic(new PlayMusic(MusicName.Invalid));

    private static readonly Packet[] m_Instances = new Packet[60];

    public PlayMusic(MusicName name) : base(0x6D, 3)
    {
      Stream.Write((short)name);
    }

    public static Packet GetInstance(MusicName name)
    {
      if (name == MusicName.Invalid)
        return InvalidInstance;

      var v = (int)name;
      Packet p;

      if (v >= 0 && v < m_Instances.Length)
      {
        p = m_Instances[v];

        if (p == null)
          m_Instances[v] = p = SetStatic(new PlayMusic(name));
      }
      else
      {
        p = new PlayMusic(name);
      }

      return p;
    }
  }

  public sealed class ScrollMessage : Packet
  {
    public ScrollMessage(int type, int tip, string text) : base(0xA6)
    {
      text ??= "";

      EnsureCapacity(10 + text.Length);

      Stream.Write((byte)type);
      Stream.Write(tip);
      Stream.Write((ushort)text.Length);
      Stream.WriteAsciiFixed(text, text.Length);
    }
  }

  public sealed class CurrentTime : Packet
  {
    public CurrentTime() : base(0x5B, 4)
    {
      var now = DateTime.UtcNow;

      Stream.Write((byte)now.Hour);
      Stream.Write((byte)now.Minute);
      Stream.Write((byte)now.Second);
    }
  }

  public sealed class MapChange : Packet
  {
    public MapChange(Mobile m) : base(0xBF)
    {
      EnsureCapacity(6);

      Stream.Write((short)0x08);
      Stream.Write((byte)(m.Map?.MapID ?? 0));
    }
  }

  public sealed class SeasonChange : Packet
  {
    private static readonly SeasonChange[][] m_Cache = new SeasonChange[][]
    {
      new SeasonChange[2],
      new SeasonChange[2],
      new SeasonChange[2],
      new SeasonChange[2],
      new SeasonChange[2]
    };

    public SeasonChange(int season, bool playSound = true) : base(0xBC, 3)
    {
      Stream.Write((byte)season);
      Stream.Write(playSound);
    }

    public static SeasonChange Instantiate(int season) => Instantiate(season, true);

    public static SeasonChange Instantiate(int season, bool playSound)
    {
      if (season >= 0 && season < m_Cache.Length)
      {
        var idx = playSound ? 1 : 0;

        var p = m_Cache[season][idx];

        if (p == null)
        {
          m_Cache[season][idx] = p = new SeasonChange(season, playSound);
          p.SetStatic();
        }

        return p;
      }

      return new SeasonChange(season, playSound);
    }
  }

  public sealed class SupportedFeatures : Packet
  {
    public SupportedFeatures(NetState ns) : base(0xB9, ns.ExtendedSupportedFeatures ? 5 : 3)
    {
      var flags = ExpansionInfo.CoreExpansion.SupportedFeatures;

      flags |= Value;

      if (ns.Account.Limit >= 6)
      {
        flags |= FeatureFlags.LiveAccount;
        flags &= ~FeatureFlags.UOTD;

        if (ns.Account.Limit > 6)
          flags |= FeatureFlags.SeventhCharacterSlot;
        else
          flags |= FeatureFlags.SixthCharacterSlot;
      }

      if (ns.ExtendedSupportedFeatures)
        Stream.Write((uint)flags);
      else
        Stream.Write((ushort)flags);
    }

    public static FeatureFlags Value { get; set; }

    public static SupportedFeatures Instantiate(NetState ns) => new SupportedFeatures(ns);
  }

  public static class AttributeNormalizer
  {
    public static int Maximum { get; set; } = 25;

    public static bool Enabled { get; set; } = true;

    public static void Write(PacketWriter stream, int cur, int max)
    {
      if (Enabled && max != 0)
      {
        stream.Write((short)Maximum);
        stream.Write((short)(cur * Maximum / max));
      }
      else
      {
        stream.Write((short)max);
        stream.Write((short)cur);
      }
    }

    public static void WriteReverse(PacketWriter stream, int cur, int max)
    {
      if (Enabled && max != 0)
      {
        stream.Write((short)(cur * Maximum / max));
        stream.Write((short)Maximum);
      }
      else
      {
        stream.Write((short)cur);
        stream.Write((short)max);
      }
    }
  }

  public sealed class MobileHits : Packet
  {
    public MobileHits(Mobile m) : base(0xA1, 9)
    {
      Stream.Write(m.Serial);
      Stream.Write((short)m.HitsMax);
      Stream.Write((short)m.Hits);
    }
  }

  public sealed class MobileHitsN : Packet
  {
    public MobileHitsN(Mobile m) : base(0xA1, 9)
    {
      Stream.Write(m.Serial);
      AttributeNormalizer.Write(Stream, m.Hits, m.HitsMax);
    }
  }

  public sealed class MobileMana : Packet
  {
    public MobileMana(Mobile m) : base(0xA2, 9)
    {
      Stream.Write(m.Serial);
      Stream.Write((short)m.ManaMax);
      Stream.Write((short)m.Mana);
    }
  }

  public sealed class MobileManaN : Packet
  {
    public MobileManaN(Mobile m) : base(0xA2, 9)
    {
      Stream.Write(m.Serial);
      AttributeNormalizer.Write(Stream, m.Mana, m.ManaMax);
    }
  }

  public sealed class MobileStam : Packet
  {
    public MobileStam(Mobile m) : base(0xA3, 9)
    {
      Stream.Write(m.Serial);
      Stream.Write((short)m.StamMax);
      Stream.Write((short)m.Stam);
    }
  }

  public sealed class MobileStamN : Packet
  {
    public MobileStamN(Mobile m) : base(0xA3, 9)
    {
      Stream.Write(m.Serial);
      AttributeNormalizer.Write(Stream, m.Stam, m.StamMax);
    }
  }

  public sealed class MobileAttributes : Packet
  {
    public MobileAttributes(Mobile m) : base(0x2D, 17)
    {
      Stream.Write(m.Serial);

      Stream.Write((short)m.HitsMax);
      Stream.Write((short)m.Hits);

      Stream.Write((short)m.ManaMax);
      Stream.Write((short)m.Mana);

      Stream.Write((short)m.StamMax);
      Stream.Write((short)m.Stam);
    }
  }

  public sealed class MobileAttributesN : Packet
  {
    public MobileAttributesN(Mobile m) : base(0x2D, 17)
    {
      Stream.Write(m.Serial);

      AttributeNormalizer.Write(Stream, m.Hits, m.HitsMax);
      AttributeNormalizer.Write(Stream, m.Mana, m.ManaMax);
      AttributeNormalizer.Write(Stream, m.Stam, m.StamMax);
    }
  }

  public sealed class PathfindMessage : Packet
  {
    public PathfindMessage(IPoint3D p) : base(0x38, 7)
    {
      Stream.Write((short)p.X);
      Stream.Write((short)p.Y);
      Stream.Write((short)p.Z);
    }
  }

  // unsure of proper format, client crashes
  public sealed class MobileName : Packet
  {
    public MobileName(Mobile m) : base(0x98)
    {
      EnsureCapacity(37);

      Stream.Write(m.Serial);
      Stream.WriteAsciiFixed(m.Name ?? "", 30);
    }
  }

  public sealed class MobileAnimation : Packet
  {
    public MobileAnimation(Mobile m, int action, int frameCount, int repeatCount, bool forward, bool repeat, int delay) : base(0x6E, 14)
    {
      Stream.Write(m.Serial);
      Stream.Write((short)action);
      Stream.Write((short)frameCount);
      Stream.Write((short)repeatCount);
      Stream.Write(!forward); // protocol has really "reverse" but I find this more intuitive
      Stream.Write(repeat);
      Stream.Write((byte)delay);
    }
  }

  public sealed class NewMobileAnimation : Packet
  {
    public NewMobileAnimation(Mobile m, int action, int frameCount, int delay) : base(0xE2, 10)
    {
      Stream.Write(m.Serial);
      Stream.Write((short)action);
      Stream.Write((short)frameCount);
      Stream.Write((byte)delay);
    }
  }

  public sealed class MobileStatusCompact : Packet
  {
    public MobileStatusCompact(bool canBeRenamed, Mobile m) : base(0x11)
    {
      EnsureCapacity(43);

      Stream.Write(m.Serial);
      Stream.WriteAsciiFixed(m.Name ?? "", 30);

      AttributeNormalizer.WriteReverse(Stream, m.Hits, m.HitsMax);

      Stream.Write(canBeRenamed);

      Stream.Write((byte)0); // type
    }
  }

  public sealed class MobileStatusExtended : Packet
  {
    public MobileStatusExtended(Mobile m) : this(m, m.NetState)
    {
    }

    public MobileStatusExtended(Mobile m, NetState ns) : base(0x11)
    {
      var name = m.Name ?? "";

      int type;

      if (Core.HS && ns?.ExtendedStatus == true)
      {
        type = 6;
        EnsureCapacity(121);
      }
      else if (Core.ML && ns?.SupportsExpansion(Expansion.ML) == true)
      {
        type = 5;
        EnsureCapacity(91);
      }
      else
      {
        type = Core.AOS ? 4 : 3;
        EnsureCapacity(88);
      }

      Stream.Write(m.Serial);
      Stream.WriteAsciiFixed(name, 30);

      Stream.Write((short)m.Hits);
      Stream.Write((short)m.HitsMax);

      Stream.Write(m.CanBeRenamedBy(m));

      Stream.Write((byte)type);

      Stream.Write(m.Female);

      Stream.Write((short)m.Str);
      Stream.Write((short)m.Dex);
      Stream.Write((short)m.Int);

      Stream.Write((short)m.Stam);
      Stream.Write((short)m.StamMax);

      Stream.Write((short)m.Mana);
      Stream.Write((short)m.ManaMax);

      Stream.Write(m.TotalGold);
      Stream.Write((short)(Core.AOS ? m.PhysicalResistance : (int)(m.ArmorRating + 0.5)));
      Stream.Write((short)(Mobile.BodyWeight + m.TotalWeight));

      if (type >= 5)
      {
        Stream.Write((short)m.MaxWeight);
        Stream.Write((byte)(m.Race.RaceID + 1)); // Would be 0x00 if it's a non-ML enabled account but...
      }

      Stream.Write((short)m.StatCap);

      Stream.Write((byte)m.Followers);
      Stream.Write((byte)m.FollowersMax);

      if (type >= 4)
      {
        Stream.Write((short)m.FireResistance); // Fire
        Stream.Write((short)m.ColdResistance); // Cold
        Stream.Write((short)m.PoisonResistance); // Poison
        Stream.Write((short)m.EnergyResistance); // Energy
        Stream.Write((short)m.Luck); // Luck

        var weapon = m.Weapon;

        if (weapon != null)
        {
          weapon.GetStatusDamage(m, out var min, out var max);
          Stream.Write((short)min); // Damage min
          Stream.Write((short)max); // Damage max
        }
        else
        {
          Stream.Write((short)0); // Damage min
          Stream.Write((short)0); // Damage max
        }

        Stream.Write(m.TithingPoints);
      }

      if (type >= 6)
        for (var i = 0; i < 15; ++i)
          Stream.Write((short)m.GetAOSStatus(i));
    }
  }

  public sealed class MobileStatus : Packet
  {
    public MobileStatus(Mobile beholder, Mobile beheld) : this(beholder, beheld, beheld.NetState)
    {
    }

    public MobileStatus(Mobile beholder, Mobile beheld, NetState ns) : base(0x11)
    {
      var name = beheld.Name ?? "";

      int type;

      if (beholder != beheld)
      {
        type = 0;
        EnsureCapacity(43);
      }
      else if (Core.HS && ns?.ExtendedStatus == true)
      {
        type = 6;
        EnsureCapacity(121);
      }
      else if (Core.ML && ns?.SupportsExpansion(Expansion.ML) == true)
      {
        type = 5;
        EnsureCapacity(91);
      }
      else
      {
        type = Core.AOS ? 4 : 3;
        EnsureCapacity(88);
      }

      Stream.Write(beheld.Serial);

      Stream.WriteAsciiFixed(name, 30);

      if (beholder == beheld)
        WriteAttr(beheld.Hits, beheld.HitsMax);
      else
        WriteAttrNorm(beheld.Hits, beheld.HitsMax);

      Stream.Write(beheld.CanBeRenamedBy(beholder));

      Stream.Write((byte)type);

      if (type <= 0)
        return;

      Stream.Write(beheld.Female);

      Stream.Write((short)beheld.Str);
      Stream.Write((short)beheld.Dex);
      Stream.Write((short)beheld.Int);

      WriteAttr(beheld.Stam, beheld.StamMax);
      WriteAttr(beheld.Mana, beheld.ManaMax);

      Stream.Write(beheld.TotalGold);
      Stream.Write((short)(Core.AOS ? beheld.PhysicalResistance : (int)(beheld.ArmorRating + 0.5)));
      Stream.Write((short)(Mobile.BodyWeight + beheld.TotalWeight));

      if (type >= 5)
      {
        Stream.Write((short)beheld.MaxWeight);
        Stream.Write((byte)(beheld.Race.RaceID + 1)); // Would be 0x00 if it's a non-ML enabled account but...
      }

      Stream.Write((short)beheld.StatCap);

      Stream.Write((byte)beheld.Followers);
      Stream.Write((byte)beheld.FollowersMax);

      if (type >= 4)
      {
        Stream.Write((short)beheld.FireResistance); // Fire
        Stream.Write((short)beheld.ColdResistance); // Cold
        Stream.Write((short)beheld.PoisonResistance); // Poison
        Stream.Write((short)beheld.EnergyResistance); // Energy
        Stream.Write((short)beheld.Luck); // Luck

        var weapon = beheld.Weapon;

        if (weapon != null)
        {
          weapon.GetStatusDamage(beheld, out var min, out var max);
          Stream.Write((short)min); // Damage min
          Stream.Write((short)max); // Damage max
        }
        else
        {
          Stream.Write((short)0); // Damage min
          Stream.Write((short)0); // Damage max
        }

        Stream.Write(beheld.TithingPoints);
      }

      if (type >= 6)
        for (var i = 0; i < 15; ++i)
          Stream.Write((short)beheld.GetAOSStatus(i));
    }

    private void WriteAttr(int current, int maximum)
    {
      Stream.Write((short)current);
      Stream.Write((short)maximum);
    }

    private void WriteAttrNorm(int current, int maximum)
    {
      AttributeNormalizer.WriteReverse(Stream, current, maximum);
    }
  }

  public sealed class HealthbarPoison : Packet
  {
    public HealthbarPoison(Mobile m) : base(0x17)
    {
      EnsureCapacity(12);

      Stream.Write(m.Serial);
      Stream.Write((short)1);

      Stream.Write((short)1);

      var p = m.Poison;

      if (p != null)
        Stream.Write((byte)(p.Level + 1));
      else
        Stream.Write((byte)0);
    }
  }

  public sealed class HealthbarYellow : Packet
  {
    public HealthbarYellow(Mobile m) : base(0x17)
    {
      EnsureCapacity(12);

      Stream.Write(m.Serial);
      Stream.Write((short)1);

      Stream.Write((short)2);

      if (m.Blessed || m.YellowHealthbar)
        Stream.Write((byte)1);
      else
        Stream.Write((byte)0);
    }
  }

  public sealed class MobileUpdate : Packet
  {
    public MobileUpdate(Mobile m) : base(0x20, 19)
    {
      var hue = m.Hue;

      if (m.SolidHueOverride >= 0)
        hue = m.SolidHueOverride;

      Stream.Write(m.Serial);
      Stream.Write((short)m.Body);
      Stream.Write((byte)0);
      Stream.Write((short)hue);
      Stream.Write((byte)m.GetPacketFlags());
      Stream.Write((short)m.X);
      Stream.Write((short)m.Y);
      Stream.Write((short)0);
      Stream.Write((byte)m.Direction);
      Stream.Write((sbyte)m.Z);
    }
  }

  // Pre-7.0.0.0 Mobile Update
  public sealed class MobileUpdateOld : Packet
  {
    public MobileUpdateOld(Mobile m) : base(0x20, 19)
    {
      var hue = m.Hue;

      if (m.SolidHueOverride >= 0)
        hue = m.SolidHueOverride;

      Stream.Write(m.Serial);
      Stream.Write((short)m.Body);
      Stream.Write((byte)0);
      Stream.Write((short)hue);
      Stream.Write((byte)m.GetOldPacketFlags());
      Stream.Write((short)m.X);
      Stream.Write((short)m.Y);
      Stream.Write((short)0);
      Stream.Write((byte)m.Direction);
      Stream.Write((sbyte)m.Z);
    }
  }

  public sealed class MobileIncoming : Packet
  {
    private static readonly ThreadLocal<int[]> m_DupedLayersTL = new ThreadLocal<int[]>(() => { return new int[256]; });
    private static readonly ThreadLocal<int> m_VersionTL = new ThreadLocal<int>();

    public MobileIncoming(Mobile beholder, Mobile beheld) : base(0x78)
    {
      var m_Version = ++m_VersionTL.Value;
      var m_DupedLayers = m_DupedLayersTL.Value;

      var eq = beheld.Items;
      var count = eq.Count;

      if (beheld.HairItemID > 0)
        count++;
      if (beheld.FacialHairItemID > 0)
        count++;

      EnsureCapacity(23 + count * 9);

      var hue = beheld.Hue;

      if (beheld.SolidHueOverride >= 0)
        hue = beheld.SolidHueOverride;

      Stream.Write(beheld.Serial);
      Stream.Write((short)beheld.Body);
      Stream.Write((short)beheld.X);
      Stream.Write((short)beheld.Y);
      Stream.Write((sbyte)beheld.Z);
      Stream.Write((byte)beheld.Direction);
      Stream.Write((short)hue);
      Stream.Write((byte)beheld.GetPacketFlags());
      Stream.Write((byte)Notoriety.Compute(beholder, beheld));

      for (var i = 0; i < eq.Count; ++i)
      {
        var item = eq[i];

        var layer = (byte)item.Layer;

        if (!item.Deleted && beholder.CanSee(item) && m_DupedLayers[layer] != m_Version)
        {
          m_DupedLayers[layer] = m_Version;

          hue = item.Hue;

          if (beheld.SolidHueOverride >= 0)
            hue = beheld.SolidHueOverride;

          var itemID = item.ItemID & 0xFFFF;

          Stream.Write(item.Serial);
          Stream.Write((ushort)itemID);
          Stream.Write(layer);

          Stream.Write((short)hue);
        }
      }

      if (beheld.HairItemID > 0)
        if (m_DupedLayers[(int)Layer.Hair] != m_Version)
        {
          m_DupedLayers[(int)Layer.Hair] = m_Version;
          hue = beheld.HairHue;

          if (beheld.SolidHueOverride >= 0)
            hue = beheld.SolidHueOverride;

          var itemID = beheld.HairItemID & 0xFFFF;

          Stream.Write(HairInfo.FakeSerial(beheld));
          Stream.Write((ushort)itemID);
          Stream.Write((byte)Layer.Hair);

          Stream.Write((short)hue);
        }

      if (beheld.FacialHairItemID > 0)
        if (m_DupedLayers[(int)Layer.FacialHair] != m_Version)
        {
          m_DupedLayers[(int)Layer.FacialHair] = m_Version;
          hue = beheld.FacialHairHue;

          if (beheld.SolidHueOverride >= 0)
            hue = beheld.SolidHueOverride;

          var itemID = beheld.FacialHairItemID & 0xFFFF;

          Stream.Write(FacialHairInfo.FakeSerial(beheld));
          Stream.Write((ushort)itemID);
          Stream.Write((byte)Layer.FacialHair);

          Stream.Write((short)hue);
        }

      Stream.Write(0); // terminate
    }

    public static Packet Create(NetState ns, Mobile beholder, Mobile beheld)
    {
      if (ns.NewMobileIncoming)
        return new MobileIncoming(beholder, beheld);
      if (ns.StygianAbyss)
        return new MobileIncomingSA(beholder, beheld);
      return new MobileIncomingOld(beholder, beheld);
    }
  }

  public sealed class MobileIncomingSA : Packet
  {
    private static readonly ThreadLocal<int[]> m_DupedLayersTL = new ThreadLocal<int[]>(() => { return new int[256]; });
    private static readonly ThreadLocal<int> m_VersionTL = new ThreadLocal<int>();

    public MobileIncomingSA(Mobile beholder, Mobile beheld) : base(0x78)
    {
      var m_Version = ++m_VersionTL.Value;
      var m_DupedLayers = m_DupedLayersTL.Value;

      var eq = beheld.Items;
      var count = eq.Count;

      if (beheld.HairItemID > 0)
        count++;
      if (beheld.FacialHairItemID > 0)
        count++;

      EnsureCapacity(23 + count * 9);

      var hue = beheld.Hue;

      if (beheld.SolidHueOverride >= 0)
        hue = beheld.SolidHueOverride;

      Stream.Write(beheld.Serial);
      Stream.Write((short)beheld.Body);
      Stream.Write((short)beheld.X);
      Stream.Write((short)beheld.Y);
      Stream.Write((sbyte)beheld.Z);
      Stream.Write((byte)beheld.Direction);
      Stream.Write((short)hue);
      Stream.Write((byte)beheld.GetPacketFlags());
      Stream.Write((byte)Notoriety.Compute(beholder, beheld));

      for (var i = 0; i < eq.Count; ++i)
      {
        var item = eq[i];

        var layer = (byte)item.Layer;

        if (!item.Deleted && beholder.CanSee(item) && m_DupedLayers[layer] != m_Version)
        {
          m_DupedLayers[layer] = m_Version;

          hue = item.Hue;

          if (beheld.SolidHueOverride >= 0)
            hue = beheld.SolidHueOverride;

          var itemID = item.ItemID & 0x7FFF;
          var writeHue = hue != 0;

          if (writeHue)
            itemID |= 0x8000;

          Stream.Write(item.Serial);
          Stream.Write((ushort)itemID);
          Stream.Write(layer);

          if (writeHue)
            Stream.Write((short)hue);
        }
      }

      if (beheld.HairItemID > 0)
        if (m_DupedLayers[(int)Layer.Hair] != m_Version)
        {
          m_DupedLayers[(int)Layer.Hair] = m_Version;
          hue = beheld.HairHue;

          if (beheld.SolidHueOverride >= 0)
            hue = beheld.SolidHueOverride;

          var itemID = beheld.HairItemID & 0x7FFF;

          var writeHue = hue != 0;

          if (writeHue)
            itemID |= 0x8000;

          Stream.Write(HairInfo.FakeSerial(beheld));
          Stream.Write((ushort)itemID);
          Stream.Write((byte)Layer.Hair);

          if (writeHue)
            Stream.Write((short)hue);
        }

      if (beheld.FacialHairItemID > 0)
        if (m_DupedLayers[(int)Layer.FacialHair] != m_Version)
        {
          m_DupedLayers[(int)Layer.FacialHair] = m_Version;
          hue = beheld.FacialHairHue;

          if (beheld.SolidHueOverride >= 0)
            hue = beheld.SolidHueOverride;

          var itemID = beheld.FacialHairItemID & 0x7FFF;

          var writeHue = hue != 0;

          if (writeHue)
            itemID |= 0x8000;

          Stream.Write(FacialHairInfo.FakeSerial(beheld));
          Stream.Write((ushort)itemID);
          Stream.Write((byte)Layer.FacialHair);

          if (writeHue)
            Stream.Write((short)hue);
        }

      Stream.Write(0); // terminate
    }
  }

  // Pre-7.0.0.0 Mobile Incoming
  public sealed class MobileIncomingOld : Packet
  {
    private static readonly ThreadLocal<int[]> m_DupedLayersTL = new ThreadLocal<int[]>(() => { return new int[256]; });
    private static readonly ThreadLocal<int> m_VersionTL = new ThreadLocal<int>();

    public MobileIncomingOld(Mobile beholder, Mobile beheld) : base(0x78)
    {
      var m_Version = ++m_VersionTL.Value;
      var m_DupedLayers = m_DupedLayersTL.Value;

      var eq = beheld.Items;
      var count = eq.Count;

      if (beheld.HairItemID > 0)
        count++;
      if (beheld.FacialHairItemID > 0)
        count++;

      EnsureCapacity(23 + count * 9);

      var hue = beheld.Hue;

      if (beheld.SolidHueOverride >= 0)
        hue = beheld.SolidHueOverride;

      Stream.Write(beheld.Serial);
      Stream.Write((short)beheld.Body);
      Stream.Write((short)beheld.X);
      Stream.Write((short)beheld.Y);
      Stream.Write((sbyte)beheld.Z);
      Stream.Write((byte)beheld.Direction);
      Stream.Write((short)hue);
      Stream.Write((byte)beheld.GetOldPacketFlags());
      Stream.Write((byte)Notoriety.Compute(beholder, beheld));

      for (var i = 0; i < eq.Count; ++i)
      {
        var item = eq[i];

        var layer = (byte)item.Layer;

        if (!item.Deleted && beholder.CanSee(item) && m_DupedLayers[layer] != m_Version)
        {
          m_DupedLayers[layer] = m_Version;

          hue = item.Hue;

          if (beheld.SolidHueOverride >= 0)
            hue = beheld.SolidHueOverride;

          var itemID = item.ItemID & 0x7FFF;
          var writeHue = hue != 0;

          if (writeHue)
            itemID |= 0x8000;

          Stream.Write(item.Serial);
          Stream.Write((ushort)itemID);
          Stream.Write(layer);

          if (writeHue)
            Stream.Write((short)hue);
        }
      }

      if (beheld.HairItemID > 0)
        if (m_DupedLayers[(int)Layer.Hair] != m_Version)
        {
          m_DupedLayers[(int)Layer.Hair] = m_Version;
          hue = beheld.HairHue;

          if (beheld.SolidHueOverride >= 0)
            hue = beheld.SolidHueOverride;

          var itemID = beheld.HairItemID & 0x7FFF;

          var writeHue = hue != 0;

          if (writeHue)
            itemID |= 0x8000;

          Stream.Write(HairInfo.FakeSerial(beheld));
          Stream.Write((ushort)itemID);
          Stream.Write((byte)Layer.Hair);

          if (writeHue)
            Stream.Write((short)hue);
        }

      if (beheld.FacialHairItemID > 0)
        if (m_DupedLayers[(int)Layer.FacialHair] != m_Version)
        {
          m_DupedLayers[(int)Layer.FacialHair] = m_Version;
          hue = beheld.FacialHairHue;

          if (beheld.SolidHueOverride >= 0)
            hue = beheld.SolidHueOverride;

          var itemID = beheld.FacialHairItemID & 0x7FFF;

          var writeHue = hue != 0;

          if (writeHue)
            itemID |= 0x8000;

          Stream.Write(FacialHairInfo.FakeSerial(beheld));
          Stream.Write((ushort)itemID);
          Stream.Write((byte)Layer.FacialHair);

          if (writeHue)
            Stream.Write((short)hue);
        }

      Stream.Write(0); // terminate
    }
  }

  public sealed class AsciiMessage : Packet
  {
    public AsciiMessage(Serial serial, int graphic, MessageType type, int hue, int font, string name, string text) : base(0x1C)
    {
      name ??= "";
      text ??= "";

      if (hue == 0)
        hue = 0x3B2;

      EnsureCapacity(45 + text.Length);

      Stream.Write(serial);
      Stream.Write((short)graphic);
      Stream.Write((byte)type);
      Stream.Write((short)hue);
      Stream.Write((short)font);
      Stream.WriteAsciiFixed(name, 30);
      Stream.WriteAsciiNull(text);
    }
  }

  public sealed class UnicodeMessage : Packet
  {
    public UnicodeMessage(Serial serial, int graphic, MessageType type, int hue, int font, string lang, string name,
      string text) : base(0xAE)
    {
      if (string.IsNullOrEmpty(lang)) lang = "ENU";
      name ??= "";
      text ??= "";

      if (hue == 0)
        hue = 0x3B2;

      EnsureCapacity(50 + text.Length * 2);

      Stream.Write(serial);
      Stream.Write((short)graphic);
      Stream.Write((byte)type);
      Stream.Write((short)hue);
      Stream.Write((short)font);
      Stream.WriteAsciiFixed(lang, 4);
      Stream.WriteAsciiFixed(name, 30);
      Stream.WriteBigUniNull(text);
    }
  }

  public sealed class PingAck : Packet
  {
    private static readonly PingAck[] m_Cache = new PingAck[0x100];

    public PingAck(byte ping) : base(0x73, 2)
    {
      Stream.Write(ping);
    }

    public static PingAck Instantiate(byte ping)
    {
      var p = m_Cache[ping];

      if (p == null)
      {
        m_Cache[ping] = p = new PingAck(ping);
        p.SetStatic();
      }

      return p;
    }
  }

  public sealed class MovementRej : Packet
  {
    public MovementRej(int seq, Mobile m) : base(0x21, 8)
    {
      Stream.Write((byte)seq);
      Stream.Write((short)m.X);
      Stream.Write((short)m.Y);
      Stream.Write((byte)m.Direction);
      Stream.Write((sbyte)m.Z);
    }
  }

  public sealed class MovementAck : Packet
  {
    private static readonly MovementAck[] m_Cache = new MovementAck[8 * 256];

    private MovementAck(int seq, int noto) : base(0x22, 3)
    {
      Stream.Write((byte)seq);
      Stream.Write((byte)noto);
    }

    public static MovementAck Instantiate(int seq, Mobile m)
    {
      var noto = Notoriety.Compute(m, m);

      var p = m_Cache[noto * seq];

      if (p == null)
      {
        m_Cache[noto * seq] = p = new MovementAck(seq, noto);
        p.SetStatic();
      }

      return p;
    }
  }

  public sealed class LoginConfirm : Packet
  {
    public LoginConfirm(Mobile m) : base(0x1B, 37)
    {
      Stream.Write(m.Serial);
      Stream.Write(0);
      Stream.Write((short)m.Body);
      Stream.Write((short)m.X);
      Stream.Write((short)m.Y);
      Stream.Write((short)m.Z);
      Stream.Write((byte)m.Direction);
      Stream.Write((byte)0);
      Stream.Write(-1);

      var map = m.Map;

      if (map == null || map == Map.Internal)
        map = m.LogoutMap;

      Stream.Write((short)0);
      Stream.Write((short)0);
      Stream.Write((short)(map?.Width ?? 6144));
      Stream.Write((short)(map?.Height ?? 4096));

      Stream.Fill();
    }
  }

  public sealed class LoginComplete : Packet
  {
    public static readonly Packet Instance = SetStatic(new LoginComplete());

    public LoginComplete() : base(0x55, 1)
    {
    }
  }

  public sealed class CharacterListUpdate : Packet
  {
    public CharacterListUpdate(IAccount a) : base(0x86)
    {
      EnsureCapacity(4 + a.Length * 60);

      var highSlot = -1;

      for (var i = 0; i < a.Length; ++i)
        if (a[i] != null)
          highSlot = i;

      var count = Math.Max(Math.Max(highSlot + 1, a.Limit), 5);

      Stream.Write((byte)count);

      for (var i = 0; i < count; ++i)
      {
        var m = a[i];

        if (m != null)
        {
          Stream.WriteAsciiFixed(m.Name, 30);
          Stream.Fill(30); // password
        }
        else
        {
          Stream.Fill(60);
        }
      }
    }
  }

  public sealed class CharacterList : Packet
  {
    // private static MD5CryptoServiceProvider m_MD5Provider;

    public CharacterList(IAccount a, CityInfo[] info) : base(0xA9)
    {
      EnsureCapacity(11 + a.Length * 60 + info.Length * 89);

      var highSlot = -1;

      for (var i = 0; i < a.Length; ++i)
        if (a[i] != null)
          highSlot = i;

      var count = Math.Max(Math.Max(highSlot + 1, a.Limit), 5);

      Stream.Write((byte)count);

      for (var i = 0; i < count; ++i)
        if (a[i] != null)
        {
          Stream.WriteAsciiFixed(a[i].Name, 30);
          Stream.Fill(30); // password
        }
        else
        {
          Stream.Fill(60);
        }

      Stream.Write((byte)info.Length);

      for (var i = 0; i < info.Length; ++i)
      {
        var ci = info[i];

        Stream.Write((byte)i);
        Stream.WriteAsciiFixed(ci.City, 32);
        Stream.WriteAsciiFixed(ci.Building, 32);
        Stream.Write(ci.X);
        Stream.Write(ci.Y);
        Stream.Write(ci.Z);
        Stream.Write(ci.Map.MapID);
        Stream.Write(ci.Description);
        Stream.Write(0);
      }

      var flags = ExpansionInfo.CoreExpansion.CharacterListFlags;

      if (count > 6)
        flags |= CharacterListFlags.SeventhCharacterSlot |
                 CharacterListFlags.SixthCharacterSlot; // 7th Character Slot - TODO: Is SixthCharacterSlot Required?
      else if (count == 6)
        flags |= CharacterListFlags.SixthCharacterSlot; // 6th Character Slot
      else if (a.Limit == 1)
        flags |= CharacterListFlags.SlotLimit &
                 CharacterListFlags.OneCharacterSlot; // Limit Characters & One Character

      Stream.Write((int)(flags | AdditionalFlags)); // Additional Flags

      Stream.Write((short)-1);
    }

    public static CharacterListFlags AdditionalFlags { get; set; }
  }

  public sealed class CharacterListOld : Packet
  {
    // private static MD5CryptoServiceProvider m_MD5Provider;

    public CharacterListOld(IAccount a, CityInfo[] info) : base(0xA9)
    {
      EnsureCapacity(9 + a.Length * 60 + info.Length * 63);

      var highSlot = -1;

      for (var i = 0; i < a.Length; ++i)
        if (a[i] != null)
          highSlot = i;

      var count = Math.Max(Math.Max(highSlot + 1, a.Limit), 5);

      Stream.Write((byte)count);

      for (var i = 0; i < count; ++i)
        if (a[i] != null)
        {
          Stream.WriteAsciiFixed(a[i].Name, 30);
          Stream.Fill(30); // password
        }
        else
        {
          Stream.Fill(60);
        }

      Stream.Write((byte)info.Length);

      for (var i = 0; i < info.Length; ++i)
      {
        var ci = info[i];

        Stream.Write((byte)i);
        Stream.WriteAsciiFixed(ci.City, 31);
        Stream.WriteAsciiFixed(ci.Building, 31);
      }

      var flags = ExpansionInfo.CoreExpansion.CharacterListFlags;

      if (count > 6)
        flags |= CharacterListFlags.SeventhCharacterSlot |
                 CharacterListFlags.SixthCharacterSlot; // 7th Character Slot - TODO: Is SixthCharacterSlot Required?
      else if (count == 6)
        flags |= CharacterListFlags.SixthCharacterSlot; // 6th Character Slot
      else if (a.Limit == 1)
        flags |= CharacterListFlags.SlotLimit &
                 CharacterListFlags.OneCharacterSlot; // Limit Characters & One Character

      Stream.Write((int)(flags | CharacterList.AdditionalFlags)); // Additional Flags
    }
  }

  public sealed class ClearWeaponAbility : Packet
  {
    public static readonly Packet Instance = SetStatic(new ClearWeaponAbility());

    public ClearWeaponAbility() : base(0xBF)
    {
      EnsureCapacity(5);

      Stream.Write((short)0x21);
    }
  }

  public enum ALRReason : byte
  {
    Invalid = 0x00,
    InUse = 0x01,
    Blocked = 0x02,
    BadPass = 0x03,
    Idle = 0xFE,
    BadComm = 0xFF
  }

  public sealed class AccountLoginRej : Packet
  {
    public AccountLoginRej(ALRReason reason) : base(0x82, 2)
    {
      Stream.Write((byte)reason);
    }
  }

  [Flags]
  public enum AffixType : byte
  {
    Append = 0x00,
    Prepend = 0x01,
    System = 0x02
  }

  public sealed class MessageLocalizedAffix : Packet
  {
    public MessageLocalizedAffix(Serial serial, int graphic, MessageType messageType, int hue, int font, int number,
      string name, AffixType affixType, string affix, string args) : base(0xCC)
    {
      name ??= "";
      affix ??= "";
      args ??= "";

      if (hue == 0)
        hue = 0x3B2;

      EnsureCapacity(52 + affix.Length + args.Length * 2);

      Stream.Write(serial);
      Stream.Write((short)graphic);
      Stream.Write((byte)messageType);
      Stream.Write((short)hue);
      Stream.Write((short)font);
      Stream.Write(number);
      Stream.Write((byte)affixType);
      Stream.WriteAsciiFixed(name, 30);
      Stream.WriteAsciiNull(affix);
      Stream.WriteBigUniNull(args);
    }
  }

  public sealed class ServerInfo
  {
    public ServerInfo(string name, int fullPercent, TimeZoneInfo tz, IPEndPoint address)
    {
      Name = name;
      FullPercent = fullPercent;
      TimeZone = tz.GetUtcOffset(DateTime.Now).Hours;
      Address = address;
    }

    public string Name { get; set; }

    public int FullPercent { get; set; }

    public int TimeZone { get; set; }

    public IPEndPoint Address { get; set; }
  }

  public sealed class FollowMessage : Packet
  {
    public FollowMessage(Serial serial1, Serial serial2) : base(0x15, 9)
    {
      Stream.Write(serial1);
      Stream.Write(serial2);
    }
  }

  public sealed class AccountLoginAck : Packet
  {
    public AccountLoginAck(ServerInfo[] info) : base(0xA8)
    {
      EnsureCapacity(6 + info.Length * 40);

      Stream.Write((byte)0x5D); // Unknown

      Stream.Write((ushort)info.Length);

      for (var i = 0; i < info.Length; ++i)
      {
        var si = info[i];

        Stream.Write((ushort)i);
        Stream.WriteAsciiFixed(si.Name, 32);
        Stream.Write((byte)si.FullPercent);
        Stream.Write((sbyte)si.TimeZone);
        Stream.Write(Utility.GetAddressValue(si.Address.Address));
      }
    }
  }

  public sealed class DisplaySignGump : Packet
  {
    public DisplaySignGump(Serial serial, int gumpID, string unknown, string caption) : base(0x8B)
    {
      unknown ??= "";
      caption ??= "";

      EnsureCapacity(16 + unknown.Length + caption.Length);

      Stream.Write(serial);
      Stream.Write((short)gumpID);
      Stream.Write((short)unknown.Length);
      Stream.WriteAsciiFixed(unknown, unknown.Length);
      Stream.Write((short)(caption.Length + 1));
      Stream.WriteAsciiFixed(caption, caption.Length + 1);
    }
  }

  public sealed class PlayServerAck : Packet
  {
    internal static int m_AuthID = -1;

    public PlayServerAck(ServerInfo si) : base(0x8C, 11)
    {
      var addr = Utility.GetAddressValue(si.Address.Address);

      Stream.Write((byte)addr);
      Stream.Write((byte)(addr >> 8));
      Stream.Write((byte)(addr >> 16));
      Stream.Write((byte)(addr >> 24));

      Stream.Write((short)si.Address.Port);
      Stream.Write(m_AuthID);
    }
  }
}
