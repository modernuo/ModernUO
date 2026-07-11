using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Text;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class AntiquePropertyTests
{
    [Fact]
    public void NegativeAttribute_ReservesTheNextNonCollidingBitForAntique()
    {
        Assert.True(Enum.TryParse<NegativeAttribute>("Antique", out var antique));
        Assert.Equal(0x00000008, (int)antique);
    }

    [Fact]
    public void Antique_IsStoredAndDisplayedOnlyFromHighSeasOnEverySupportedHost()
    {
        var previousExpansion = Core.Expansion;
        Item[] items = [new Katana(), new LeatherChest(), new Buckler(), new GoldRing()];

        try
        {
            foreach (var item in items)
            {
                GetNegativeAttributes(item).Antique = 1;

                Core.Expansion = Expansion.SA;
                var beforeHighSeas = new RecordingPropertyList();
                item.GetProperties(beforeHighSeas);
                Assert.False(NegativeAttributes.IsAntique(item));
                Assert.DoesNotContain(beforeHighSeas.Entries, entry => entry.Number == 1076187);

                Core.Expansion = Expansion.HS;
                var highSeas = new RecordingPropertyList();
                item.GetProperties(highSeas);
                Assert.True(NegativeAttributes.IsAntique(item));
                Assert.Single(highSeas.Entries, entry => entry.Number == 1076187 && entry.Argument == string.Empty);
            }
        }
        finally
        {
            Core.Expansion = previousExpansion;

            foreach (var item in items)
            {
                item.Delete();
            }
        }
    }

    [Fact]
    public void Antique_EquippedHostsLoseOneDurabilityOnAnAcceptedMissButBackpackItemsDoNot()
    {
        var previousExpansion = Core.Expansion;
        using var random = new Server.Tests.PredictableRandom(0);
        var attacker = CreateMobile(new Point3D(6200, 500, 0));
        var defender = CreateMobile(new Point3D(6201, 500, 0));
        var weapon = new TestKatana { MaxHitPoints = 10, HitPoints = 5 };
        var armor = new LeatherChest { MaxHitPoints = 10, HitPoints = 5 };
        var shield = new Buckler { Layer = Layer.TwoHanded, MaxHitPoints = 10, HitPoints = 5 };
        var jewel = new GoldRing { MaxHitPoints = 10, HitPoints = 5 };
        var backpackWeapon = new Katana { MaxHitPoints = 10, HitPoints = 5 };

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.NegativeAttributes.Antique = 1;
            armor.NegativeAttributes.Antique = 1;
            shield.NegativeAttributes.Antique = 1;
            jewel.NegativeAttributes.Antique = 1;
            backpackWeapon.NegativeAttributes.Antique = 1;
            attacker.AddItem(weapon);
            attacker.AddItem(armor);
            attacker.AddItem(shield);
            attacker.AddItem(jewel);
            attacker.AddItem(new Backpack());
            attacker.Backpack.AddItem(backpackWeapon);

            weapon.OnSwing(attacker, defender);

            Assert.Equal(4, weapon.HitPoints);
            Assert.Equal(4, armor.HitPoints);
            Assert.Equal(4, shield.HitPoints);
            Assert.Equal(4, jewel.HitPoints);
            Assert.Equal(5, backpackWeapon.HitPoints);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            armor.Delete();
            shield.Delete();
            jewel.Delete();
            backpackWeapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void PowderOfTemperament_AntiqueWeaponAppliesTheFirstCapWithoutFreeRepair()
    {
        var previousExpansion = Core.Expansion;
        var player = new PlayerMobile(World.NewMobile);
        player.DefaultMobileInit();
        player.MoveToWorld(new Point3D(6200, 500, 0), Map.Felucca);
        player.AddItem(new Backpack());
        var weapon = new Katana { MaxHitPoints = 245, HitPoints = 200 };
        var powder = new PowderOfTemperament();

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.NegativeAttributes.Antique = 1;
            Assert.True(NegativeAttributes.IsAntique(weapon));
            player.Backpack.AddItem(weapon);
            player.Backpack.AddItem(powder);

            powder.OnDoubleClick(player);
            player.Target.Invoke(player, weapon);

            Assert.Equal(250, weapon.MaxHitPoints);
            Assert.Equal(200, weapon.HitPoints);
            Assert.Equal(2, weapon.NegativeAttributes.Antique);
            Assert.Equal(9, powder.UsesRemaining);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            powder.Delete();
            player.Delete();
        }
    }

    private static Mobile CreateMobile(Point3D location)
    {
        var mobile = new Mobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.MoveToWorld(location, Map.Felucca);
        return mobile;
    }

    private sealed class TestKatana : Katana
    {
        public override bool CheckHit(Mobile attacker, Mobile defender) => false;
        public override int ComputeDamage(Mobile attacker, Mobile defender) => 1;
        public override void AddBlood(Mobile attacker, Mobile defender, int damage)
        {
        }
    }

    private static NegativeAttributes GetNegativeAttributes(Item item) => item switch
    {
        BaseWeapon weapon => weapon.NegativeAttributes,
        BaseArmor armor   => armor.NegativeAttributes,
        BaseJewel jewel   => jewel.NegativeAttributes,
        _                 => throw new ArgumentException($"Unsupported Antique host: {item.GetType().FullName}", nameof(item))
    };

    private sealed record PropertyEntry(int Number, string Argument);

    private sealed class RecordingPropertyList : IPropertyList
    {
        private string _interpolated = string.Empty;

        public List<PropertyEntry> Entries { get; } = [];

        public void Reset()
        {
        }

        public void Terminate()
        {
        }

        public void Add(int number) => Entries.Add(new PropertyEntry(number, string.Empty));
        public void Add(int number, string argument) => Entries.Add(new PropertyEntry(number, argument));
        public void Add(ReadOnlySpan<char> argument) => Entries.Add(new PropertyEntry(0, argument.ToString()));
        public void Add(int number, ReadOnlySpan<char> argument) => Entries.Add(new PropertyEntry(number, argument.ToString()));
        public void AddChunked(ReadOnlySpan<char> text) => Entries.Add(new PropertyEntry(0, text.ToString()));
        public OplTextBlock TextBlock() => new(this);
        public void Add(int number, int value) => Entries.Add(new PropertyEntry(number, value.ToString()));
        public void AddLocalized(int value) => Entries.Add(new PropertyEntry(0, value.ToString()));
        public void AddLocalized(int number, int value) => Entries.Add(new PropertyEntry(number, value.ToString()));
        public void Add(ref IPropertyList.InterpolatedStringHandler handler) => Entries.Add(new PropertyEntry(0, _interpolated));
        public void Add(int number, ref IPropertyList.InterpolatedStringHandler handler) =>
            Entries.Add(new PropertyEntry(number, _interpolated));
        public void InitializeInterpolation(int literalLength, int formattedCount) => _interpolated = string.Empty;
        public void AppendLiteral(string value) => _interpolated += value;
        public void AppendFormatted<T>(T value) => _interpolated += value;
        public void AppendFormatted<T>(T value, string format) =>
            _interpolated += value is IFormattable formattable ? formattable.ToString(format, null) : value;
        public void AppendFormatted<T>(T value, int alignment) => _interpolated += value;
        public void AppendFormatted<T>(T value, int alignment, string format) =>
            _interpolated += value is IFormattable formattable ? formattable.ToString(format, null) : value;
        public void AppendFormatted(ReadOnlySpan<char> value) => _interpolated += value.ToString();
        public void AppendFormatted(ReadOnlySpan<char> value, int alignment, string format = null) =>
            _interpolated += value.ToString();
        public void AppendFormatted(object value, int alignment = 0, string format = null) => _interpolated += value;
        public void AppendFormatted(string value) => _interpolated += value;
        public void AppendFormatted(string value, int alignment, string format = null) => _interpolated += value;
    }
}
