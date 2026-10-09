# Avatar system: features not ported from Memento

Source: `Jascen/ultima-memento`, `World/Source/Scripts/Engines and Systems/Avatar/`.
These features were left out because Veritas has no system for them to hook into. Each entry
names the Memento file to start from if the feature is implemented later.

## Ascensions
| Ascension | Memento hook | Depends on |
|---|---|---|
| Power Overwhelming (`UnlockTemptations`) | `Temptations/*`, `PlayerMobile.CreateCopy` | Temptations system |
| Primal Awakening (`UnlockSavageRace`) | `Mobiles/Civilized/ShardGreeter.cs`, `PlayerSettings.cs` | Savage race tarot card, land discovery |
| Bestial Transformation (`UnlockMonsterRaces`) | `Mobiles/Races/RacePotions.cs` | Monster races |
| Outlaw's Mark (`UnlockFugitiveMode`) | `ShardGreeter.cs` | Fugitive start mode |
| World Class Cartographer (`UnlockRecordDiscovered`) | `System/Misc/CharacterCreation.cs` (`CharacterDiscovered`) | Per-character land discovery |
| Fast Seaman (`BoatSpeedLevel`) | `Items/Boats/BaseBoat.cs` (`MAX_SPEED_BOOSTS`) | Boat speed boosts |

## Templates
- Jester (Bag of Tricks), Mystic (Monk's Tome), Shinobi (Shinobi Scroll), Death Knight (Death Knight
  spellbook) and Holy Man (Holy Man spellbook + Holy Symbol). This includes their `UnlockTemplate*` Ascensions
  and the `CanUnlockTemplate*` skill prerequisites in `PlayerContext.cs`.

## Coins and statistics
- Combat-quest coins (`CustomEventSink.CombatQuestCompleted`, 5× award) and `LifetimeCombatQuestCompletions`.
- `CoinRewardCalculatorLegacy`, which valued Memento currencies (DD copper/silver/xormite, crystals, gems, jewels, nuggets).
- Kill scoring by creature level (`Misc.IntelligentAction.GetCreatureLevel`) and the `IsEphemeral` exclusion.
- Breath scoring by `BreathAttackForm` (small/trick/special/area/large). Veritas scores breath from the
  `FireBreath` ability's damage types and `BreathDamageScalar` instead.
- Rival factions that need Memento-only slayers: WizardSlayer, AvianHunter, SlimyScourge, AnimalHunter,
  GiantKiller, GolemDestruction, WeedRuin, NeptunesBane.

## Integrations elsewhere in Memento
- `IAvatarOnlyItem`: equip and use checks in `BaseArmor`, `BaseWeapon`, `BaseClothing`, `BaseTrinket` and
  `BaseInstrument`, plus clean-up in `PlayerMobile.OnAfterDelete`.
- `CombatBar` coin and Draft display (`System/Commands/Player/CombatBar.cs`).
- `ShardGreeter` tarot-card entry ("THE AVATAR"). Template items were also granted there when the player left the
  encampment. Veritas grants them as soon as a template is selected.
- `SoulOrb` permadeath placeholder.
- `-$AV` ID suffixes in `AchievementSystem` and the Global Shoppe (`ShoppeEngine`).
- House-sharing rule between Avatars and non-Avatars (`BaseHouse.cs`).
- Soulstone restriction (`Items/Special/SoulStone.cs`).
- No stat-gain delay for Avatars (`System/Skills/SkillCheck.cs`).
- "Player Type: Avatar" on player vendors and the advertiser vendor check.
- `avatar-migrate--game-time` command (a Memento save migration).

## Behavior differences in the port
- **Death:** the same mobile is reset in place instead of being swapped for a new one, and the player is not
  disconnected. Followers are released and stabled pets deleted.
- **Secondary skills:** Memento's engine excludes crafting and gathering skills from the skill total. Veritas
  counts them, so restoring any skill from the archive respects the total cap (pulling from skills locked Down).
- **Draft:** undrafted skills are blocked from gaining in `SkillCheck.Gain`. Veritas `Skill` has no `CanGain` flag.
- **Death records** are named `{serial}_{deathNumber}.bin` instead of `{name}_{deathNumber}.bin`.
- **Sanctuary:** the Gypsy Encampment is the configurable `avatar.sanctuary.*` region and respawn point (see
  `AvatarSanctuary.cs`).
