using System;

namespace Server.Items;

public partial class Corpse
{
    // Decay timer and TimeOfDeath moved from delta time to anchored time
    private void MigrateFrom(V18Content content)
    {
        _restoreEquip = content.RestoreEquip;
        _flags = content.Flags;
        _timeOfDeath = content.TimeOfDeath;
        _restoreTable = content.RestoreTable;
        _looters = content.Looters;
        _killer = content.Killer;
        _aggressors = content.Aggressors;
        _owner = content.Owner;
        _corpseName = content.CorpseName;
        _accessLevel = content.AccessLevel;
        _guild = content.Guild;
        _equipItems = content.EquipItems;
        _hairItemId = content.HairItemId;
        _hairHue = content.HairHue;
        _facialHairItemId = content.FacialHairItemId;
        _facialHairHue = content.FacialHairHue;

        if (content.DecayTimerDelay != TimeSpan.MinValue)
        {
            DeserializeDecayTimer(content.DecayTimerDelay);
        }
    }

    // Decay timer moved from [TimerDrift]/[DeserializeTimerField] to [DeserializeTimer]
    private void MigrateFrom(V17Content content)
    {
        _restoreEquip = content.RestoreEquip;
        _flags = content.Flags;
        _timeOfDeath = content.TimeOfDeath;
        _restoreTable = content.RestoreTable;
        _looters = content.Looters;
        _killer = content.Killer;
        _aggressors = content.Aggressors;
        _owner = content.Owner;
        _corpseName = content.CorpseName;
        _accessLevel = content.AccessLevel;
        _guild = content.Guild;
        _equipItems = content.EquipItems;
        _hairItemId = content.HairItemId;
        _hairHue = content.HairHue;
        _facialHairItemId = content.FacialHairItemId;
        _facialHairHue = content.FacialHairHue;

        if (content.DecayTimerDelay != TimeSpan.MinValue)
        {
            DeserializeDecayTimer(content.DecayTimerDelay);
        }
    }

    // Decomposed VirtualHairInfo into discrete int fields (hair/facial hair item id + hue)
    private void MigrateFrom(V16Content content)
    {
        _restoreEquip = content.RestoreEquip;
        _flags = content.Flags;
        _timeOfDeath = content.TimeOfDeath;
        _restoreTable = content.RestoreTable;
        _decayTimer = new InternalTimer(this, content.DecayTimerDelay);
        _decayTimer.Start();
        _looters = content.Looters;
        _killer = content.Killer;
        _aggressors = content.Aggressors;
        _owner = content.Owner;
        _corpseName = content.CorpseName;
        _accessLevel = content.AccessLevel;
        _guild = content.Guild;
        _equipItems = content.EquipItems;
        if (content.Hair != null)
        {
            _hairItemId = content.Hair.ItemId;
            _hairHue = content.Hair.Hue;
        }

        if (content.FacialHair != null)
        {
            _facialHairItemId = content.FacialHair.ItemId;
            _facialHairHue = content.FacialHair.Hue;
        }
    }

    // Folded Murderer bool field into CorpseFlag.Murderer
    private void MigrateFrom(V15Content content)
    {
        _restoreEquip = content.RestoreEquip;
        _flags = content.Flags;
        if (content.Murderer)
        {
            _flags |= CorpseFlag.Murderer;
        }
        _timeOfDeath = content.TimeOfDeath;
        _restoreTable = content.RestoreTable;
        _decayTimer = new InternalTimer(this, content.DecayTimerDelay);
        _decayTimer.Start();
        _looters = content.Looters;
        _killer = content.Killer;
        _aggressors = content.Aggressors;
        _owner = content.Owner;
        _corpseName = content.CorpseName;
        _accessLevel = content.AccessLevel;
        _guild = content.Guild;
        _equipItems = content.EquipItems;
        if (content.Hair != null)
        {
            _hairItemId = content.Hair.ItemId;
            _hairHue = content.Hair.Hue;
        }

        if (content.FacialHair != null)
        {
            _facialHairItemId = content.FacialHair.ItemId;
            _facialHairHue = content.FacialHair.Hue;
        }
    }

    // Replaced int Kills snapshot with bool Murderer snapshot
    private void MigrateFrom(V14Content content)
    {
        _restoreEquip = content.RestoreEquip;
        _flags = content.Flags;
        if (content.Kills >= 5)
        {
            _flags |= CorpseFlag.Murderer;
        }
        _timeOfDeath = content.TimeOfDeath;
        _restoreTable = content.RestoreTable;
        _decayTimer = new InternalTimer(this, content.DecayTimerDelay);
        _decayTimer.Start();
        _looters = content.Looters;
        _killer = content.Killer;
        _aggressors = content.Aggressors;
        _owner = content.Owner;
        _corpseName = content.CorpseName;
        _accessLevel = content.AccessLevel;
        _guild = content.Guild;
        _equipItems = content.EquipItems;
        if (content.Hair != null)
        {
            _hairItemId = content.Hair.ItemId;
            _hairHue = content.Hair.Hue;
        }

        if (content.FacialHair != null)
        {
            _facialHairItemId = content.FacialHair.ItemId;
            _facialHairHue = content.FacialHair.Hue;
        }
    }

    // Added corpse hair and corpse facial hair
    private void MigrateFrom(V13Content content)
    {
        _restoreEquip = content.RestoreEquip;
        _flags = content.Flags;
        if (content.Kills >= 5)
        {
            _flags |= CorpseFlag.Murderer;
        }
        _timeOfDeath = content.TimeOfDeath;
        _restoreTable = content.RestoreTable;
        _decayTimer = new InternalTimer(this, content.DecayTimerDelay);
        _decayTimer.Start();
        _looters = content.Looters;
        _killer = content.Killer;
        _aggressors = content.Aggressors;
        _owner = content.Owner;
        _corpseName = content.CorpseName;
        _accessLevel = content.AccessLevel;
        _guild = content.Guild;
        _equipItems = content.EquipItems;
    }
}
