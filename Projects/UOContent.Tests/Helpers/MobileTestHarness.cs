using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Tests;

/// <summary>
/// Owns the mobiles and items a test creates and deletes them on dispose, so a failed assertion
/// cannot leave entities registered in the shared world. Hold one per test class instance and
/// dispose it from the class's <see cref="IDisposable.Dispose"/>.
/// </summary>
internal sealed class MobileTestHarness : IDisposable
{
    private readonly List<Mobile> _mobiles = [];
    private readonly List<Item> _items = [];

    public T Track<T>(T mobile) where T : Mobile
    {
        _mobiles.Add(mobile);
        return mobile;
    }

    public T TrackItem<T>(T item) where T : Item
    {
        _items.Add(item);
        return item;
    }

    /// <summary>
    /// A player registered in the world table, so serialized references to it resolve on load.
    /// </summary>
    public PlayerMobile NewPlayer()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();
        World.AddEntity(pm);
        return Track(pm);
    }

    public TestCreature NewCreature() => Track(new TestCreature());

    public void Dispose()
    {
        for (var i = 0; i < _mobiles.Count; i++)
        {
            _mobiles[i].Delete();
        }

        for (var i = 0; i < _items.Count; i++)
        {
            _items[i].Delete();
        }
    }
}

/// <summary>
/// A plain melee creature. Its speeds are pinned so tests do not depend on the npc-speeds table.
/// </summary>
internal class TestCreature : BaseCreature
{
    public TestCreature() : base(AIType.AI_Melee) => Body = 0xC9;

    public TestCreature(Serial serial) : base(serial) => Body = 0xC9;

    public DateTime SummonEndValue => SummonEnd;

    public override void GetSpeeds(out double activeSpeed, out double passiveSpeed)
    {
        activeSpeed = 0.3;
        passiveSpeed = 0.6;
    }

    public override void GetMoveSpeeds(out double activeMoveSpeed, out double passiveMoveSpeed)
    {
        activeMoveSpeed = 0.6;
        passiveMoveSpeed = 1.2;
    }
}

/// <summary>
/// A vendor with no outfit and pinned speeds. Subclasses stock it by overriding
/// <see cref="AddSBInfos"/>, which runs from the base constructor.
/// </summary>
internal class TestVendor : BaseVendor
{
    // Initialized before the base constructor calls InitSBInfo.
    private readonly List<SBInfo> _sbInfos = [];

    public TestVendor() : base("the stub")
    {
    }

    public TestVendor(Serial serial) : base(serial)
    {
    }

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo() => AddSBInfos(_sbInfos);

    protected virtual void AddSBInfos(List<SBInfo> sbInfos)
    {
    }

    public override void InitOutfit()
    {
    }

    public override void GetSpeeds(out double activeSpeed, out double passiveSpeed)
    {
        activeSpeed = 0.3;
        passiveSpeed = 0.6;
    }
}
