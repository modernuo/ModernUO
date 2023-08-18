/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ChampionSkullBrazier.cs                                         *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using ModernUO.Serialization;
using Server.Items;
using Server.Targeting;
using Server.Mobiles;

namespace Server.Engines.CannedEvil;

[SerializationGenerator(1, false)]
public partial class ChampionSkullBrazier : AddonComponent
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private ChampionSkullType _type;

    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private ChampionSkullPlatform _platform;

    [SerializableProperty(2)]
    [CommandProperty(AccessLevel.GameMaster)]
    public Item Skull
    {
        get => _skull;
        set
        {
            _skull = value;
            this.MarkDirty();
            _platform?.Validate();
        }
    }

    public override int LabelNumber => 1049489 + (int)_type;

    public ChampionSkullBrazier(ChampionSkullPlatform platform, ChampionSkullType type) : base(0x19BB)
    {
        Hue = 0x455;
        Light = LightType.Circle300;

        _platform = platform;
        _type = type;
    }

    public override void OnDoubleClick(Mobile from)
    {
        _platform?.Validate();
        BeginSacrifice(from);
    }

    public void BeginSacrifice(Mobile from)
    {
        if (Deleted)
        {
            return;
        }

        if (_skull is { Deleted: true })
        {
            Skull = null;
        }

        if (from.Map != Map || !from.InRange(GetWorldLocation(), 3))
        {
            from.SendLocalizedMessage(500446); // That is too far away.
        }
        else if (!Harrower.CanSpawn)
        {
            from.SendMessage("The harrower has already been spawned.");
        }
        else if (_skull == null)
        {
            from.SendLocalizedMessage(1049485); // What would you like to sacrifice?
            from.Target = new SacrificeTarget(this);
        }
        else
        {
            SendLocalizedMessageTo(from, 1049487); // I already have my champions awakening skull!
        }
    }

    public void EndSacrifice(Mobile from, ChampionSkull skull)
    {
        if (Deleted)
        {
            return;
        }

        if (_skull is { Deleted: true })
        {
            Skull = null;
        }

        if (from.Map != Map || !from.InRange(GetWorldLocation(), 3))
        {
            from.SendLocalizedMessage(500446); // That is too far away.
        }
        else if (!Harrower.CanSpawn)
        {
            from.SendMessage("The harrower has already been spawned.");
        }
        else if (skull == null)
        {
            SendLocalizedMessageTo(from, 1049488); // That is not my champions awakening skull!
        }
        else if (_skull != null)
        {
            SendLocalizedMessageTo(from, 1049487); // I already have my champions awakening skull!
        }
        else if (!skull.IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1049486); // You can only sacrifice items that are in your backpack!
        }
        else if (skull.Type == Type)
        {
            skull.Movable = false;
            skull.MoveToWorld(GetWorldTop(), Map);

            Skull = skull;
        }
        else
        {
            SendLocalizedMessageTo(from, 1049488); // That is not my champions awakening skull!
        }
    }

    private class SacrificeTarget : Target
    {
        private readonly ChampionSkullBrazier _brazier;

        public SacrificeTarget(ChampionSkullBrazier brazier) : base(12, false, TargetFlags.None) =>
            _brazier = brazier;

        protected override void OnTarget(Mobile from, object targeted) =>
            _brazier.EndSacrifice(from, targeted as ChampionSkull);
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _type = (ChampionSkullType)reader.ReadInt();
        _platform = reader.ReadEntity<ChampionSkullPlatform>();
        _skull = reader.ReadEntity<Item>();
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        if (_platform == null)
        {
            Delete();
        }
    }
}
