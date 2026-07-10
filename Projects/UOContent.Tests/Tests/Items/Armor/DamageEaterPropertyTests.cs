using System;
using System.Collections.Generic;
using System.Reflection;
using Server;
using Server.Items;
using Server.Tests;
using Server.Text;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class DamageEaterPropertyTests
{
    private const int DamageEaterCliloc = 1113598;
    private const int KineticEaterCliloc = 1113597;
    private const int FireEaterCliloc = 1113593;
    private const int ColdEaterCliloc = 1113594;
    private const int PoisonEaterCliloc = 1113595;
    private const int EnergyEaterCliloc = 1113596;
    private static readonly DateTime TestNow = new(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void AbsorptionAttributes_StoresDupesAndSerializesDamageEatersOnArmorAndShields()
    {
        var armor = new LeatherChest();
        var armorDupe = new LeatherChest();
        var deserialized = new LeatherChest();
        var shield = new BronzeShield();
        var shieldDupe = new BronzeShield();

        try
        {
            armor.AbsorptionAttributes.DamageEater = 12;
            armor.AbsorptionAttributes.KineticEater = 13;
            armor.AbsorptionAttributes.FireEater = 14;
            armor.AbsorptionAttributes.ColdEater = 15;
            armor.AbsorptionAttributes.PoisonEater = 16;
            armor.AbsorptionAttributes.EnergyEater = 17;
            shield.AbsorptionAttributes.FireEater = 20;

            armor.Dupe(armorDupe);
            shield.Dupe(shieldDupe);

            Assert.Equal(12, armorDupe.AbsorptionAttributes.DamageEater);
            Assert.Equal(13, armorDupe.AbsorptionAttributes.KineticEater);
            Assert.Equal(14, armorDupe.AbsorptionAttributes.FireEater);
            Assert.Equal(15, armorDupe.AbsorptionAttributes.ColdEater);
            Assert.Equal(16, armorDupe.AbsorptionAttributes.PoisonEater);
            Assert.Equal(17, armorDupe.AbsorptionAttributes.EnergyEater);
            Assert.Equal(20, shieldDupe.AbsorptionAttributes.FireEater);

            var writer = new BufferWriter(true);
            armor.Serialize(writer);
            var buffer = new byte[writer.Position];
            writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);

            var reader = new BufferReader(buffer);
            deserialized.Deserialize(reader);

            Assert.Equal(buffer.Length, reader.Position);
            Assert.Equal(12, deserialized.AbsorptionAttributes.DamageEater);
            Assert.Equal(13, deserialized.AbsorptionAttributes.KineticEater);
            Assert.Equal(14, deserialized.AbsorptionAttributes.FireEater);
            Assert.Equal(15, deserialized.AbsorptionAttributes.ColdEater);
            Assert.Equal(16, deserialized.AbsorptionAttributes.PoisonEater);
            Assert.Equal(17, deserialized.AbsorptionAttributes.EnergyEater);
        }
        finally
        {
            armor.Delete();
            armorDupe.Delete();
            deserialized.Delete();
            shield.Delete();
            shieldDupe.Delete();
        }
    }

    [Fact]
    public void AbsorptionAttributes_EaterPropertiesAreStaffEditable()
    {
        var names = new[]
        {
            nameof(AbsorptionAttributes.DamageEater),
            nameof(AbsorptionAttributes.KineticEater),
            nameof(AbsorptionAttributes.FireEater),
            nameof(AbsorptionAttributes.ColdEater),
            nameof(AbsorptionAttributes.PoisonEater),
            nameof(AbsorptionAttributes.EnergyEater)
        };

        foreach (var name in names)
        {
            var property = typeof(AbsorptionAttributes).GetProperty(name);
            Assert.NotNull(property);

            var attribute = property.GetCustomAttribute<CommandPropertyAttribute>();
            Assert.NotNull(attribute);
            Assert.Equal(AccessLevel.GameMaster, attribute.ReadLevel);
            Assert.Equal(AccessLevel.GameMaster, attribute.WriteLevel);
        }
    }

    [Fact]
    public void BaseArmor_GetProperties_GatesAllDamageEaterTooltipsToStygianAbyss()
    {
        var previousExpansion = Core.Expansion;
        var armor = new LeatherChest();

        try
        {
            armor.AbsorptionAttributes.DamageEater = 3;
            armor.AbsorptionAttributes.KineticEater = 4;
            armor.AbsorptionAttributes.FireEater = 5;
            armor.AbsorptionAttributes.ColdEater = 6;
            armor.AbsorptionAttributes.PoisonEater = 7;
            armor.AbsorptionAttributes.EnergyEater = 8;

            Core.Expansion = Expansion.ML;
            var preStygianAbyss = new RecordingPropertyList();
            armor.GetProperties(preStygianAbyss);
            Assert.DoesNotContain(
                preStygianAbyss.Entries,
                entry => entry.Number is DamageEaterCliloc or KineticEaterCliloc or FireEaterCliloc or ColdEaterCliloc
                    or PoisonEaterCliloc or EnergyEaterCliloc
            );

            Core.Expansion = Expansion.SA;
            var stygianAbyss = new RecordingPropertyList();
            armor.GetProperties(stygianAbyss);

            Assert.Contains(stygianAbyss.Entries, entry => entry.Number == DamageEaterCliloc && entry.Argument == "3");
            Assert.Contains(stygianAbyss.Entries, entry => entry.Number == KineticEaterCliloc && entry.Argument == "4");
            Assert.Contains(stygianAbyss.Entries, entry => entry.Number == FireEaterCliloc && entry.Argument == "5");
            Assert.Contains(stygianAbyss.Entries, entry => entry.Number == ColdEaterCliloc && entry.Argument == "6");
            Assert.Contains(stygianAbyss.Entries, entry => entry.Number == PoisonEaterCliloc && entry.Argument == "7");
            Assert.Contains(stygianAbyss.Entries, entry => entry.Number == EnergyEaterCliloc && entry.Argument == "8");
        }
        finally
        {
            Core.Expansion = previousExpansion;
            armor.Delete();
        }
    }

    [Fact]
    public void AbsorptionAttributes_GetEaterValue_AppliesSpecificAndAllTypeCaps()
    {
        var previousExpansion = Core.Expansion;
        var mobile = CreateMobile();
        var first = new LeatherChest();
        var second = new LeatherLegs();

        try
        {
            first.AbsorptionAttributes.FireEater = 20;
            second.AbsorptionAttributes.FireEater = 20;
            first.AbsorptionAttributes.DamageEater = 12;
            second.AbsorptionAttributes.DamageEater = 12;
            mobile.AddItem(first);
            mobile.AddItem(second);

            Core.Expansion = Expansion.ML;
            Assert.Equal(0, AbsorptionAttributes.GetEaterValue(mobile, AbsorptionAttribute.FireEater));

            Core.Expansion = Expansion.SA;
            Assert.Equal(DamageEater.SpecificCap, AbsorptionAttributes.GetEaterValue(mobile, AbsorptionAttribute.FireEater));
            Assert.Equal(DamageEater.AllTypesCap, AbsorptionAttributes.GetEaterValue(mobile, AbsorptionAttribute.DamageEater));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            first.Delete();
            second.Delete();
            mobile.Delete();
        }
    }

    [Fact]
    public void DamagePipeline_PreStygianAbyssStoredValuesAreInert()
    {
        var previousExpansion = Core.Expansion;
        var previousNow = Core._now;
        var defender = CreateMobile(hitsMax: 1000, hits: 500);
        var source = CreateMobile();
        var armor = new LeatherChest();

        try
        {
            Core.Expansion = Expansion.ML;
            Core._now = TestNow;
            armor.AbsorptionAttributes.KineticEater = 20;
            defender.AddItem(armor);

            var appliedDamage = ApplyDamage(defender, source, 100, 100, 0, 0, 0, 0);
            Assert.True(appliedDamage > 0);
            Assert.Equal(500 - appliedDamage, defender.Hits);
            Assert.Equal(0, DamageEater.GetPendingChargesForTests(defender));
            Assert.False(DamageEater.TickForTests(defender));
        }
        finally
        {
            DamageEater.ClearAll();
            Core._now = previousNow;
            Core.Expansion = previousExpansion;
            armor.Delete();
            defender.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void DamagePipeline_UsesMatchingPostResistDamageAndNonAdditiveAllTypeEater()
    {
        var previousExpansion = Core.Expansion;
        var previousNow = Core._now;
        var defender = CreateMobile(hitsMax: 1000, hits: 500);
        var source = CreateMobile();
        var armor = new LeatherChest();

        try
        {
            Core.Expansion = Expansion.SA;
            Core._now = TestNow;
            armor.AbsorptionAttributes.FireEater = 20;
            defender.AddItem(armor);

            var wrongTypeDamage = ApplyDamage(defender, source, 100, 100, 0, 0, 0, 0);
            Assert.True(wrongTypeDamage > 0);
            Assert.Equal(0, DamageEater.GetPendingChargesForTests(defender));

            armor.AbsorptionAttributes.DamageEater = 10;
            var appliedDamage = ApplyDamage(defender, source, 100, 0, 100, 0, 0, 0);
            Assert.True(appliedDamage > 0);
            Assert.Equal(1, DamageEater.GetPendingChargesForTests(defender));

            Core._now = TestNow.Add(DamageEater.ConversionDelay);
            Assert.True(DamageEater.TickForTests(defender));
            Assert.Equal(500 - wrongTypeDamage - appliedDamage + appliedDamage * 20 / 100, defender.Hits);
        }
        finally
        {
            DamageEater.ClearAll();
            Core._now = previousNow;
            Core.Expansion = previousExpansion;
            armor.Delete();
            defender.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void DamagePipeline_ArmorIgnorePreservesTypedDamageForSpecificEaters()
    {
        var previousExpansion = Core.Expansion;
        var previousNow = Core._now;
        var defender = CreateMobile(hitsMax: 1000, hits: 500);
        var source = CreateMobile();
        var armor = new LeatherChest();

        try
        {
            Core.Expansion = Expansion.SA;
            Core._now = TestNow;
            armor.AbsorptionAttributes.KineticEater = 20;
            defender.AddItem(armor);

            var appliedDamage = ApplyDamage(defender, source, 100, 100, 0, 0, 0, 0, ignoreArmor: true);
            Assert.True(appliedDamage > 0);
            Assert.Equal(1, DamageEater.GetPendingChargesForTests(defender));

            Core._now = TestNow.Add(DamageEater.ConversionDelay);
            DamageEater.TickForTests(defender);
            Assert.Equal(500 - appliedDamage + appliedDamage * 20 / 100, defender.Hits);
        }
        finally
        {
            DamageEater.ClearAll();
            Core._now = previousNow;
            Core.Expansion = previousExpansion;
            armor.Delete();
            defender.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void DamagePipeline_AllTypeEaterIsCappedAndHandlesDirectDamage()
    {
        var previousExpansion = Core.Expansion;
        var previousNow = Core._now;
        var defender = CreateMobile(hitsMax: 1000, hits: 500);
        var source = CreateMobile();
        var armor = new LeatherChest();

        try
        {
            Core.Expansion = Expansion.SA;
            Core._now = TestNow;
            armor.AbsorptionAttributes.DamageEater = 25;
            armor.AbsorptionAttributes.KineticEater = 30;
            defender.AddItem(armor);

            var appliedDamage = ApplyDamage(defender, source, 100, 0, 0, 0, 0, 0, 100);
            Assert.True(appliedDamage > 0);
            Assert.Equal(1, DamageEater.GetPendingChargesForTests(defender));

            Core._now = TestNow.Add(DamageEater.ConversionDelay);
            DamageEater.TickForTests(defender);
            Assert.Equal(500 - appliedDamage + appliedDamage * DamageEater.AllTypesCap / 100, defender.Hits);
        }
        finally
        {
            DamageEater.ClearAll();
            Core._now = previousNow;
            Core.Expansion = previousExpansion;
            armor.Delete();
            defender.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void DamagePipeline_CapsPendingHealingAtTwentyCharges()
    {
        var previousExpansion = Core.Expansion;
        var previousNow = Core._now;
        var defender = CreateMobile(hitsMax: 5000, hits: 5000);
        var source = CreateMobile();
        var armor = new LeatherChest();

        try
        {
            Core.Expansion = Expansion.SA;
            Core._now = TestNow;
            armor.AbsorptionAttributes.KineticEater = 10;
            defender.AddItem(armor);

            for (var i = 0; i < DamageEater.MaxCharges + 5; i++)
            {
                ApplyDamage(defender, source, 100, 100, 0, 0, 0, 0);
            }

            Assert.Equal(DamageEater.MaxCharges, DamageEater.GetPendingChargesForTests(defender));
        }
        finally
        {
            DamageEater.ClearAll();
            Core._now = previousNow;
            Core.Expansion = previousExpansion;
            armor.Delete();
            defender.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void DamagePipeline_FullQueueStillResetsConversionCadence()
    {
        var previousExpansion = Core.Expansion;
        var previousNow = Core._now;
        var defender = CreateMobile(hitsMax: 5000, hits: 5000);
        var source = CreateMobile();
        var armor = new LeatherChest();

        try
        {
            Core.Expansion = Expansion.SA;
            Core._now = TestNow;
            armor.AbsorptionAttributes.KineticEater = 10;
            defender.AddItem(armor);

            var firstDamage = 0;
            for (var i = 0; i < DamageEater.MaxCharges; i++)
            {
                var appliedDamage = ApplyDamage(defender, source, 100, 100, 0, 0, 0, 0);
                firstDamage = i == 0 ? appliedDamage : firstDamage;
            }

            Core._now = TestNow.AddSeconds(2);
            ApplyDamage(defender, source, 100, 100, 0, 0, 0, 0);
            var hitsAfterDamage = defender.Hits;

            Core._now = TestNow.Add(DamageEater.ConversionDelay);
            DamageEater.TickForTests(defender);
            Assert.Equal(hitsAfterDamage, defender.Hits);
            Assert.Equal(DamageEater.MaxCharges, DamageEater.GetPendingChargesForTests(defender));

            Core._now = TestNow.AddSeconds(5);
            DamageEater.TickForTests(defender);
            Assert.Equal(hitsAfterDamage + firstDamage * 10 / 100, defender.Hits);
            Assert.Equal(DamageEater.MaxCharges - 1, DamageEater.GetPendingChargesForTests(defender));
        }
        finally
        {
            DamageEater.ClearAll();
            Core._now = previousNow;
            Core.Expansion = previousExpansion;
            armor.Delete();
            defender.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void DamagePipeline_ConvertsOneChargeEveryThreeSecondsAfterLastDamage()
    {
        var previousExpansion = Core.Expansion;
        var previousNow = Core._now;
        var defender = CreateMobile(hitsMax: 1000, hits: 500);
        var source = CreateMobile();
        var armor = new LeatherChest();

        try
        {
            Core.Expansion = Expansion.SA;
            Core._now = TestNow;
            armor.AbsorptionAttributes.KineticEater = 10;
            defender.AddItem(armor);

            var firstDamage = ApplyDamage(defender, source, 100, 100, 0, 0, 0, 0);
            var secondDamage = ApplyDamage(defender, source, 100, 100, 0, 0, 0, 0);
            var hitsAfterDamage = defender.Hits;

            Core._now = TestNow.AddSeconds(2);
            DamageEater.TickForTests(defender);
            Assert.Equal(hitsAfterDamage, defender.Hits);
            Assert.Equal(2, DamageEater.GetPendingChargesForTests(defender));

            var interveningDamage = ApplyDamage(defender, source, 100, 0, 100, 0, 0, 0);
            hitsAfterDamage -= interveningDamage;

            Core._now = TestNow.Add(DamageEater.ConversionDelay);
            DamageEater.TickForTests(defender);
            Assert.Equal(hitsAfterDamage, defender.Hits);
            Assert.Equal(2, DamageEater.GetPendingChargesForTests(defender));

            Core._now = TestNow.Add(DamageEater.ConversionDelay + DamageEater.ConversionDelay);
            DamageEater.TickForTests(defender);
            Assert.Equal(hitsAfterDamage + firstDamage * 10 / 100, defender.Hits);
            Assert.Equal(1, DamageEater.GetPendingChargesForTests(defender));

            Core._now = TestNow.Add(DamageEater.ConversionDelay + DamageEater.ConversionDelay + DamageEater.ConversionDelay);
            DamageEater.TickForTests(defender);
            Assert.Equal(hitsAfterDamage + firstDamage * 10 / 100 + secondDamage * 10 / 100, defender.Hits);
        }
        finally
        {
            DamageEater.ClearAll();
            Core._now = previousNow;
            Core.Expansion = previousExpansion;
            armor.Delete();
            defender.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void DamagePipeline_CleansPendingHealingWhenArmorIsRemovedOrOwnerDies()
    {
        var previousExpansion = Core.Expansion;
        var previousNow = Core._now;
        var defender = CreateMobile(hitsMax: 1000, hits: 500);
        var source = CreateMobile();
        var armor = new LeatherChest();

        try
        {
            Core.Expansion = Expansion.SA;
            Core._now = TestNow;
            armor.AbsorptionAttributes.KineticEater = 10;
            defender.AddItem(armor);
            ApplyDamage(defender, source, 100, 100, 0, 0, 0, 0);
            Assert.Equal(1, DamageEater.GetPendingChargesForTests(defender));

            armor.Delete();
            Assert.Equal(0, DamageEater.GetPendingChargesForTests(defender));

            var secondArmor = new LeatherChest();
            secondArmor.AbsorptionAttributes.KineticEater = 10;
            defender.AddItem(secondArmor);
            ApplyDamage(defender, source, 100, 100, 0, 0, 0, 0);
            defender.Kill();

            Core._now = TestNow.Add(DamageEater.ConversionDelay);
            DamageEater.TickForTests(defender);
            Assert.Equal(0, defender.Hits);
            Assert.Equal(0, DamageEater.GetPendingChargesForTests(defender));
            secondArmor.Delete();
        }
        finally
        {
            DamageEater.ClearAll();
            Core._now = previousNow;
            Core.Expansion = previousExpansion;
            armor.Delete();
            defender.Delete();
            source.Delete();
        }
    }

    private static int ApplyDamage(
        Mobile defender,
        Mobile source,
        int damage,
        int physical,
        int fire,
        int cold,
        int poison,
        int energy,
        int direct = 0,
        bool ignoreArmor = false
    )
    {
        var oldHits = defender.Hits;
        AOS.Damage(defender, source, damage, ignoreArmor, physical, fire, cold, poison, energy, 0, direct);

        return Math.Max(0, oldHits - defender.Hits);
    }

    private static Mobile CreateMobile(int hitsMax = 1000, int hits = 1000)
    {
        var mobile = new Mobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.RawStr = Math.Max(1, (hitsMax - 50) * 2);
        Assert.Equal(hitsMax, mobile.HitsMax);
        mobile.Hits = hits;
        mobile.MoveToWorld(new Point3D(6200, 500, 0), Map.Felucca);
        return mobile;
    }

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
        public void Add(ref IPropertyList.InterpolatedStringHandler handler) =>
            Entries.Add(new PropertyEntry(0, _interpolated));
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
