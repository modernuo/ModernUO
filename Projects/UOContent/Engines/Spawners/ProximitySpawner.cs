/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ProximitySpawner.cs                                             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Text.Json;
using ModernUO.Serialization;
using Server.Json;
using Server.Mobiles;

namespace Server.Engines.Spawners;

[SerializationGenerator(0)]
public partial class ProximitySpawner : Spawner
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private int _triggerRange;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private TextDefinition _spawnMessage;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private bool _instantFlag;

    [Constructible(AccessLevel.Developer)]
    public ProximitySpawner()
    {
    }

    [Constructible(AccessLevel.Developer)]
    public ProximitySpawner(string spawnName) : base(spawnName)
    {
    }

    [Constructible(AccessLevel.Developer)]
    public ProximitySpawner(int amount, int minDelay, int maxDelay, int team, int homeRange, string spawnName)
        : base(amount, minDelay, maxDelay, team, homeRange, spawnName)
    {
    }

    [Constructible(AccessLevel.Developer)]
    public ProximitySpawner(
        int amount, int minDelay, int maxDelay, int team, int homeRange, int triggerRange,
        string spawnMessage, bool instantFlag, string spawnName
    ) : base(amount, minDelay, maxDelay, team, homeRange, spawnName)
    {
        TriggerRange = triggerRange;
        SpawnMessage = TextDefinition.Parse(spawnMessage);
        InstantFlag = instantFlag;
    }

    [Constructible(AccessLevel.Developer)]
    public ProximitySpawner(
        int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int homeRange,
        params ReadOnlySpan<string> spawnedNames
    ) : base(amount, minDelay, maxDelay, team, homeRange, spawnedNames)
    {
    }

    [Constructible(AccessLevel.Developer)]
    public ProximitySpawner(
        int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int homeRange, int triggerRange,
        TextDefinition spawnMessage, bool instantFlag, params ReadOnlySpan<string> spawnedNames
    ) : base(amount, minDelay, maxDelay, team, homeRange, spawnedNames)
    {
        TriggerRange = triggerRange;
        SpawnMessage = spawnMessage;
        InstantFlag = instantFlag;
    }

    public ProximitySpawner(DynamicJson json, JsonSerializerOptions options) : base(json, options)
    {
        json.GetProperty("triggerRange", options, out int triggerRange);
        json.GetProperty("spawnMessage", options, out TextDefinition spawnMessage);
        json.GetProperty("instant", options, out bool instant);

        TriggerRange = triggerRange;
        SpawnMessage = spawnMessage;
        InstantFlag = instant;
    }

    public override string DefaultName => "Proximity Spawner";

    public override bool HandlesOnMovement => Running;

    public override void ToJson(DynamicJson json, JsonSerializerOptions options)
    {
        base.ToJson(json, options);
        json.SetProperty("triggerRange", options, TriggerRange);
        json.SetProperty("spawnMessage", options, SpawnMessage);
        json.SetProperty("instant", options, InstantFlag);
    }

    public override void DoTimer(TimeSpan delay)
    {
        if (!Running)
        {
            return;
        }

        End = Core.Now + delay;
    }

    public override void Respawn()
    {
        RemoveSpawns();

        End = Core.Now;
    }

    public virtual bool ValidTrigger(Mobile m)
    {
        if (m is BaseCreature bc && (bc.IsDeadBondedPet || !(bc.Controlled || bc.Summoned)))
        {
            return false;
        }

        return m.AccessLevel == AccessLevel.Player && (m.Player || m.Alive && !m.Hidden && m.CanBeDamaged());
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (!Running)
        {
            return;
        }

        if (IsEmpty && End <= Core.Now && m.InRange(GetWorldLocation(), TriggerRange) &&
            m.Location != oldLocation && ValidTrigger(m))
        {
            SpawnMessage.SendMessageTo(m);

            DoTimer();
            Spawn();

            if (InstantFlag)
            {
                foreach (var spawned in Spawned.Keys)
                {
                    if (spawned is Mobile mobile)
                    {
                        mobile.Combatant = m;
                    }
                }
            }
        }
    }
}
