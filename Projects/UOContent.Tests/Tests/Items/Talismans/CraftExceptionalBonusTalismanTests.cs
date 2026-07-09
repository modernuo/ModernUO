using System;
using System.Collections.Generic;
using System.Globalization;
using Server;
using Server.Engines.Craft;
using Server.Items;
using Server.Random;
using Server.Text;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class CraftExceptionalBonusTalismanTests
{
    private const int CraftExceptionalBonusCliloc = 1072395;
    private const int CraftSuccessBonusCliloc = 1072394;

    [Fact]
    public void BaseTalisman_StoresDupesAndSerializesCraftExceptionalBonus()
    {
        var talisman = new BaseTalisman
        {
            Skill = SkillName.Blacksmith,
            ExceptionalBonus = 23
        };
        var dupe = new BaseTalisman();
        var serialized = new BaseTalisman();

        try
        {
            talisman.Dupe(dupe);

            Assert.Equal(SkillName.Blacksmith, dupe.Skill);
            Assert.Equal(23, dupe.ExceptionalBonus);

            var writer = new BufferWriter(1024, false);
            talisman.Serialize(writer);

            var bytes = writer.Buffer.AsSpan(0, (int)writer.Position).ToArray();
            serialized.Deserialize(new BufferReader(bytes));

            Assert.Equal(SkillName.Blacksmith, serialized.Skill);
            Assert.Equal(23, serialized.ExceptionalBonus);
        }
        finally
        {
            talisman.Delete();
            dupe.Delete();
            serialized.Delete();
        }
    }

    [Fact]
    public void GetProperties_DisplaysCraftExceptionalBonusAsSeparateTalismanCraftingModifier()
    {
        var talisman = new BaseTalisman
        {
            Skill = SkillName.Blacksmith,
            ExceptionalBonus = 17,
            SuccessBonus = 11
        };

        try
        {
            var properties = new RecordingPropertyList();
            talisman.GetProperties(properties);

            Assert.Contains(
                properties.Entries,
                entry => entry.Number == CraftExceptionalBonusCliloc &&
                         entry.Argument == $"{AosSkillBonuses.GetLabel(SkillName.Blacksmith)}\t17"
            );
            Assert.Contains(
                properties.Entries,
                entry => entry.Number == CraftSuccessBonusCliloc &&
                         entry.Argument == $"{AosSkillBonuses.GetLabel(SkillName.Blacksmith)}\t11"
            );
        }
        finally
        {
            talisman.Delete();
        }
    }

    [Fact]
    public void GetExceptionalChance_AppliesOnlyMatchingCraftExceptionalBonus()
    {
        var system = new TestCraftSystem(SkillName.Blacksmith);
        var craftItem = CreateCraftItem();
        var crafter = CreateCrafter();
        var matchingTalisman = CreateEquippedTalisman(crafter, SkillName.Blacksmith, exceptionalBonus: 25);

        try
        {
            Assert.Equal(0.45, craftItem.GetExceptionalChance(system, 0.80, crafter), precision: 5);
        }
        finally
        {
            matchingTalisman.Delete();
            crafter.Delete();
        }
    }

    [Fact]
    public void GetExceptionalChance_IgnoresNonMatchingCraftSystem()
    {
        var system = new TestCraftSystem(SkillName.Blacksmith);
        var craftItem = CreateCraftItem();
        var crafter = CreateCrafter();
        var nonMatchingTalisman = CreateEquippedTalisman(crafter, SkillName.Tailoring, exceptionalBonus: 25);

        try
        {
            Assert.Equal(0.20, craftItem.GetExceptionalChance(system, 0.80, crafter), precision: 5);
        }
        finally
        {
            nonMatchingTalisman.Delete();
            crafter.Delete();
        }
    }

    [Fact]
    public void GetExceptionalChance_DoesNotUseCraftSuccessBonusAsExceptionalBonus()
    {
        var system = new TestCraftSystem(SkillName.Blacksmith);
        var craftItem = CreateCraftItem();
        var crafter = CreateCrafter();
        var talisman = CreateEquippedTalisman(crafter, SkillName.Blacksmith, successBonus: 20);

        try
        {
            Assert.Equal(0.0, craftItem.GetExceptionalChance(system, 0.80, crafter), precision: 5);
        }
        finally
        {
            talisman.Delete();
            crafter.Delete();
        }
    }

    [Fact]
    public void GetExceptionalChance_DoesNotCreateExceptionalChanceFromZeroBaseOrForceNonExceptional()
    {
        var system = new TestCraftSystem(SkillName.Blacksmith);
        var craftItem = CreateCraftItem();
        var forceNonExceptional = CreateCraftItem(forceNonExceptional: true);
        var crafter = CreateCrafter();
        var talisman = CreateEquippedTalisman(crafter, SkillName.Blacksmith, exceptionalBonus: 25);

        try
        {
            Assert.Equal(0.0, craftItem.GetExceptionalChance(system, 0.60, crafter), precision: 5);
            Assert.Equal(0.0, forceNonExceptional.GetExceptionalChance(system, 0.95, crafter), precision: 5);
        }
        finally
        {
            talisman.Delete();
            crafter.Delete();
        }
    }

    [Fact]
    public void GetRandomExceptional_PreservesCurrentZeroOrTenToThirtyGenerationPolicy()
    {
        using (new FixedRandom(nextDouble: 0.0, nextInt: 0))
        {
            Assert.Equal(30, BaseTalisman.GetRandomExceptional());
        }

        using (new FixedRandom(nextDouble: 0.0, nextInt: 396))
        {
            Assert.Equal(10, BaseTalisman.GetRandomExceptional());
        }

        using (new FixedRandom(nextDouble: 0.30, nextInt: 0))
        {
            Assert.Equal(0, BaseTalisman.GetRandomExceptional());
        }
    }

    private static CraftItem CreateCraftItem(bool forceNonExceptional = false)
    {
        var craftItem = new CraftItem(typeof(Katana), "Weapons", "katana")
        {
            ForceNonExceptional = forceNonExceptional
        };
        craftItem.AddSkill(SkillName.Blacksmith, 0.0, 100.0);
        return craftItem;
    }

    private static Mobile CreateCrafter()
    {
        var mobile = new Mobile
        {
            Player = true
        };
        mobile.DefaultMobileInit();
        mobile.Skills[SkillName.Blacksmith].Base = 100.0;
        mobile.Skills[SkillName.Tailoring].Base = 100.0;
        return mobile;
    }

    private static BaseTalisman CreateEquippedTalisman(
        Mobile mobile,
        SkillName skill,
        int exceptionalBonus = 0,
        int successBonus = 0
    )
    {
        var talisman = new BaseTalisman
        {
            Skill = skill,
            ExceptionalBonus = exceptionalBonus,
            SuccessBonus = successBonus
        };

        Assert.True(mobile.EquipItem(talisman));
        Assert.Same(talisman, mobile.Talisman);
        return talisman;
    }

    private sealed class TestCraftSystem : CraftSystem
    {
        public TestCraftSystem(SkillName mainSkill) : base(1, 1, 1.0)
        {
            MainSkill = mainSkill;
        }

        public override SkillName MainSkill { get; }
        public override double GetChanceAtMin(CraftItem item) => 0.0;
        public override void InitCraftList()
        {
        }

        public override void PlayCraftEffect(Mobile from)
        {
        }

        public override int PlayEndingEffect(
            Mobile from,
            bool failed,
            bool lostMaterial,
            bool toolBroken,
            int quality,
            bool makersMark,
            CraftItem item
        ) => 0;

        public override int CanCraft(Mobile from, BaseTool tool, Type itemType) => 0;
    }

    private sealed class FixedRandom : System.Random, IDisposable
    {
        private readonly System.Random _original;
        private readonly double _nextDouble;
        private readonly int _nextInt;

        public FixedRandom(double nextDouble, int nextInt)
        {
            _original = BuiltInRng.Generator;
            _nextDouble = nextDouble;
            _nextInt = nextInt;
            BuiltInRng.Generator = this;
        }

        public void Dispose()
        {
            BuiltInRng.Generator = _original;
        }

        public override double NextDouble() => _nextDouble;
        public override int Next(int maxValue) => Math.Clamp(_nextInt, 0, maxValue - 1);
        public override int Next(int minValue, int maxValue) => Math.Clamp(_nextInt, minValue, maxValue - 1);
    }

    private sealed class RecordingPropertyList : IPropertyList
    {
        private string _interpolated = string.Empty;

        public List<(int Number, string Argument)> Entries { get; } = [];

        public void Reset()
        {
        }

        public void Terminate()
        {
        }

        public void Add(int number) => Entries.Add((number, null));
        public void Add(int number, string argument) => Entries.Add((number, argument));
        public void Add(ReadOnlySpan<char> argument) => Entries.Add((0, argument.ToString()));
        public void Add(int number, ReadOnlySpan<char> argument) => Entries.Add((number, argument.ToString()));
        public void AddChunked(ReadOnlySpan<char> text) => Entries.Add((0, text.ToString()));
        public OplTextBlock TextBlock() => new(this);
        public void Add(int number, int value) => Entries.Add((number, value.ToString(CultureInfo.InvariantCulture)));
        public void AddLocalized(int value) => Entries.Add((0, value.ToString(CultureInfo.InvariantCulture)));
        public void AddLocalized(int number, int value) => Entries.Add((number, value.ToString(CultureInfo.InvariantCulture)));
        public void Add(ref IPropertyList.InterpolatedStringHandler handler) => Entries.Add((0, _interpolated));
        public void Add(int number, ref IPropertyList.InterpolatedStringHandler handler) => Entries.Add((number, _interpolated));
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
