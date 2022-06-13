using System;

namespace Server.Factions;

public static class FactionSystem
{
    public static bool Enabled { get; private set; }

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetSetting("factions.enabled", false);

        if (Enabled)
        {
            GenericPersistence.Register("Factions", Serialize, Deserialize);
        }
    }

    // This does not do the actual work of removing faction stuff, only turns off the persistence.
    public static void Disable()
    {
        if (!Enabled)
        {
            return;
        }

        Persistence.Unregister("Factions");
        Enabled = false;
        ServerConfiguration.SetSetting("factions.enabled", false);
    }

    // This does not do the actual work of creating faction stuff, only turns on the persistence.
    public static void Enable()
    {
        if (Enabled)
        {
            return;
        }

        GenericPersistence.Register("Factions", Serialize, Deserialize);
        Enabled = true;
        ServerConfiguration.SetSetting("factions.enabled", true);
    }

    private static void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        var factions = Faction.Factions;
        for (var i = 0; i < factions.Count; i++)
        {
            factions[i].State.Serialize(writer);
        }

        var towns = Town.Towns;
        for (var i = 0; i < towns.Count; i++)
        {
            towns[i].State.Serialize(writer);
        }
    }

    private static void Deserialize(IGenericReader reader)
    {
        var version = reader.ReadEncodedInt();

        var count = Faction.Factions.Count;
        for (var i = 0; i < count; i++)
        {
            new FactionState(reader);
        }

        count = Town.Towns.Count;
        for (var i = 0; i < count; i++)
        {
            new TownState(reader);
        }
    }
}

[ManualDirtyChecking]
[TypeAlias("Server.Factions.FactionPersistance")]
[Obsolete("Deprecated in favor of the static system. Only used for legacy deserialization")]
public class FactionPersistence : Item
{
    public FactionPersistence()
    {
        Delete();
    }

    public FactionPersistence(Serial serial) : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);

        var version = reader.ReadInt();

        int type;

        while ((type = reader.ReadEncodedInt()) != 0)
        {
            if (type == 1)
            {
                new FactionState(reader);
            }
            else if (type == 2)
            {
                new TownState(reader);
            }
        }
    }
}
