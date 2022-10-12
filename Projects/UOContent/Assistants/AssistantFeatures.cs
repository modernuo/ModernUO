using System;

namespace Server.Assistants;

[Flags]
public enum AssistantFeatures : ulong
{
    None = 0,

    FilterWeather = 1ul << 0,   // Weather Filter
    FilterLight = 1ul << 1,     // Light Filter
    SmartTarget = 1ul << 2,     // Smart Last Target
    RangedTarget = 1ul << 3,    // Range Check Last Target
    AutoOpenDoors = 1ul << 4,   // Automatically Open Doors
    DequipOnCast = 1ul << 5,    // Unequip Weapon on spell cast
    AutoPotionEquip = 1ul << 6, // Un/Re-equip weapon on potion use
    PoisonedChecks = 1ul << 7,  // Block heal If poisoned/Macro If Poisoned condition/Heal or Cure self
    LoopedMacros = 1ul << 8,    // Disallow Looping macros, For loops, and macros that call other macros
    UseOnceAgent = 1ul << 9,    // The use once agent
    RestockAgent = 1ul << 10,   // The restock agent
    SellAgent = 1ul << 11,      // The sell agent
    BuyAgent = 1ul << 12,       // The buy agent
    PotionHotkeys = 1ul << 13,  // All potion hotkeys
    RandomTargets = 1ul << 14,  // All random target hotkeys (not target next, last target, target self)
    ClosestTargets = 1ul << 15, // All closest target hotkeys
    OverheadHealth = 1ul << 16, // Health and Mana/Stam messages shown over player's heads

    // AssistUO Only
    AutolootAgent = 1ul << 17, // The autoloot agent

    BoneCutterAgent = 1ul << 18, // The bone cutter agent
    JScriptMacros = 1ul << 19,   // Javascript macro engine
    AutoRemount = 1ul << 20,     // Auto remount after dismount
    AutoBandage = 1 << 21,       // Auto bandage friends, self, last and mount option
    EnemyTargetShare = 1 << 22,  // Enemy target share on guild, party or alliance chat
    FilterSeason = 1 << 23,      // Season Filter
    SpellTargetShare = 1 << 24,  // Spell target share on guild, party or alliance chat

    All = ~None // Every feature possible
}
