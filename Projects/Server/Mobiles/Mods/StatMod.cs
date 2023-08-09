/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: StatMod.cs                                                      *
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
using ModernUO.Serialization;

namespace Server;

[SerializationGenerator(0)]
public partial class StatMod : MobileMod
{
    [SerializableField(0, getter: "private", setter: "private")]
    private DateTime _added;

    [SerializableField(1, getter: "private", setter: "private")]
    private TimeSpan _duration;

    [SerializableField(2, setter: "private")]
    private StatType _type;

    [SerializableField(3, setter: "private")]
    private int _offset;

    // Added a timer and removed the processing of expirations in GetStatOffset which caused recursions in DeltaQueue
    // ProcessDeltaQueue -> Dequeue -> ProcessDelta -> SendStats packet -> get_HitsMax ->
    // - GetStatOffset -> Delta -> Queue to DeltaQueue
    private TimerExecutionToken _timerToken;

    public StatMod(Mobile owner) : base(owner)
    {
    }

    public StatMod(StatType type, string name, int offset, TimeSpan duration, Mobile owner = null) : base(owner, name)
    {
        _type = type;
        _offset = offset;
        _duration = duration;
        _added = Core.Now;

        if (_duration > TimeSpan.Zero)
        {
            Timer.StartTimer(duration, RemoveFromOwner, out _timerToken);
        }
    }

    public bool HasElapsed() => _duration > TimeSpan.Zero && Core.Now - _added >= _duration;

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_duration > TimeSpan.Zero && Core.Now - _added < _duration)
        {
            Timer.StartTimer(_duration, RemoveFromOwner, out _timerToken);
        }
    }

    private void RemoveFromOwner() => Owner?.RemoveStatMod(this);

    // Called by Mobile.RemoveStatMod()
    public void Remove() => _timerToken.Cancel();
}
