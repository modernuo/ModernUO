# Packet Documentation Tracking

## Protocol Version Reference

### Client Version → ProtocolChanges Mapping
| Version | Flag | Description |
|---------|------|-------------|
| 5.0.0a | NewSpellbook | AOS spellbook format (0xBF/0x1B) |
| 5.0.0a | DamagePacket | Damage packet (0x0B) |
| 5.0.2b | BuffIcon | Buff/debuff icons |
| 6.0.0.0 | NewHaven | New Haven starting city |
| **6.0.1.7** | **ContainerGridLines** | Container item grid positions (+1 byte per item) |
| 6.0.14.2 | ExtendedSupportedFeatures | 0xB9 uses 4 bytes instead of 2 |
| 7.0.0.0 | StygianAbyss | SA client features, 0xF3 world entity |
| 7.0.9.0 | HighSeas | HS features, extended 0xF3 |
| 7.0.13.0 | NewCharacterList | Extended city info in 0xA9 |
| 7.0.16.0 | NewCharacterCreation | 4th skill slot |
| 7.0.30.0 | ExtendedStatus | Mobile status version 6 |
| 7.0.33.1 | NewMobileIncoming | 0x78 always includes hue |
| 7.0.45.65 | NewSecureTrading | New secure trade format |
| 7.0.50.0 | UltimaStore | Ultima Store packets |
| 7.0.61.0 | EndlessJourney | EJ features |

### Mobile Status Versions (Expansion-based)
| Version | Expansion | Size | Description |
|---------|-----------|------|-------------|
| 0 | Any | 43 | Compact (viewing others) |
| 3 | Pre-AOS | 70 | Basic stats |
| 4 | AOS | 88 | Resistances, luck, damage |
| 5 | ML | 91 | Weight, race |
| 6 | HS | 121 | Extended AOS status |

---

## Incoming Packets Tracking

### Account (IncomingAccountPackets.cs)
- [x] 0x00 - Create Character (Old, 104 bytes)
- [x] 0x5D - Play Character
- [x] 0x80 - Account Login
- [x] 0x83 - Delete Character
- [x] 0x91 - Game Login
- [x] 0xA0 - Play Server
- [x] 0xBD - Client Version (response)
- [x] 0xE1 - Client Type
- [x] 0xEF - Login Server Seed
- [x] 0xF8 - Create Character (New, 106 bytes)

### Movement (IncomingMovementPackets.cs)
- [x] 0x02 - Movement Request

### Entity (IncomingEntityPackets.cs)
- [x] 0x06 - Use Request (Double-Click)
- [x] 0x09 - Look Request (Single-Click)
- [x] 0xB6 - Object Help Request
- [x] 0xD6 - Batch Query Properties

### Item (IncomingItemPackets.cs)
- [ ] 0x07 - Lift Request
- [ ] 0x08 - Drop Request (VERSION: 6017 adds grid location byte)
- [ ] 0x13 - Equip Request
- [ ] 0xEC - Equip Macro
- [ ] 0xED - Unequip Macro

### Mobile (IncomingMobilePackets.cs)
- [ ] 0x6F - Secure Trade
- [ ] 0x75 - Rename Request
- [ ] 0x98 - Mobile Name Request (incoming)

### Player (IncomingPlayerPackets.cs)
- [ ] 0x01 - Disconnect
- [ ] 0x05 - Attack Request
- [ ] 0x12 - Text Command (skills, cast)
- [ ] 0x22 - Resynchronize
- [ ] 0x2C - Death Status Response
- [ ] 0x34 - Mobile Query
- [ ] 0x3A - Change Skill Lock
- [ ] 0x72 - Set War Mode
- [ ] 0x73 - Ping Request
- [ ] 0x7D - Menu Response
- [ ] 0x95 - Hue Picker Response
- [ ] 0x9A - ASCII Prompt Response
- [ ] 0x9B - Help Request
- [ ] 0xA4 - System Info
- [ ] 0xA7 - Request Scroll Window
- [ ] 0xC2 - Unicode Prompt Response
- [ ] 0xC8 - Set Update Range
- [ ] 0xD0 - Configuration File
- [ ] 0xD1 - Logout Request
- [ ] 0xD7 - Encoded Command (wrapper)
- [ ] 0xF4 - Crash Report

### Message (IncomingMessagePackets.cs)
- [ ] 0x03 - ASCII Speech
- [ ] 0xAD - Unicode Speech

### Vendor (IncomingVendorPackets.cs)
- [ ] 0x3B - Vendor Buy Reply
- [ ] 0x9F - Vendor Sell Reply

### Targeting (IncomingTargetingPackets.cs)
- [ ] 0x6C - Target Response

### House (IncomingHousePackets.cs)
- [ ] 0xFB - Show Public House Content

### Extended Commands (IncomingExtendedCommandPackets.cs) - 0xBF subpackets
- [ ] 0xBF/0x05 - Screen Size
- [ ] 0xBF/0x06 - Party Message
- [ ] 0xBF/0x09 - Disarm Request
- [ ] 0xBF/0x0A - Stun Request
- [ ] 0xBF/0x0B - Language
- [ ] 0xBF/0x0C - Close Status
- [ ] 0xBF/0x0E - Animate
- [ ] 0xBF/0x0F - Empty (unused)
- [ ] 0xBF/0x10 - Query Properties
- [ ] 0xBF/0x13 - Context Menu Request
- [ ] 0xBF/0x15 - Context Menu Response
- [ ] 0xBF/0x1A - Stat Lock Change
- [ ] 0xBF/0x1C - Cast Spell
- [ ] 0xBF/0x1E - Query Design Details
- [ ] 0xBF/0x24 - Unhandled BF
- [ ] 0xBF/0x2A - Race Change Reply
- [ ] 0xBF/0x2C - Bandage Target
- [ ] 0xBF/0x2D - Targeted Spell
- [ ] 0xBF/0x2E - Targeted Skill Use
- [ ] 0xBF/0x30 - Target By Resource Macro
- [ ] 0xBF/0x32 - Toggle Flying

### Encoded Commands (0xD7 subpackets)
- [ ] 0xD7/0x02 - House Designer Backup
- [ ] 0xD7/0x03 - House Designer Restore
- [ ] 0xD7/0x04 - House Designer Commit
- [ ] 0xD7/0x05 - House Designer Delete
- [ ] 0xD7/0x06 - House Designer Build
- [ ] 0xD7/0x0C - House Designer Close
- [ ] 0xD7/0x0D - House Designer Stairs
- [ ] 0xD7/0x0E - House Designer Sync
- [ ] 0xD7/0x10 - House Designer Clear
- [ ] 0xD7/0x12 - House Designer Level
- [ ] 0xD7/0x13 - House Designer Roof
- [ ] 0xD7/0x14 - House Designer Roof Delete
- [ ] 0xD7/0x19 - Set Ability
- [ ] 0xD7/0x1A - House Designer Revert
- [ ] 0xD7/0x28 - Guild Gump Request
- [ ] 0xD7/0x32 - Quest Gump Request

### Gumps (GumpSystem.cs)
- [ ] 0xB1 - Display Gump Response

### Books (BookPackets.cs)
- [ ] 0x66 - Book Content Change
- [ ] 0x93 - Old Book Header Change
- [ ] 0xD4 - Book Header Change

### Bulletin Boards (BulletinBoardPackets.cs)
- [ ] 0x71 - Bulletin Board Request

### Maps (MapItemPackets.cs)
- [ ] 0x56 - Map Command

### Mahjong (MahjongPackets.cs)
- [ ] 0xDA - Mahjong (with subcommands)

### Chat (ChatPackets.cs)
- [ ] 0xB3 - Chat Action
- [ ] 0xB5 - Open Chat Window Request

### Ultima Store (UltimaStorePackets.cs)
- [ ] 0xFA - Ultima Store Open Request

### Hardware Info (HardwareInfo.cs)
- [ ] 0xD9 - Hardware Info

### Assistants (AssistantHandler.cs)
- [ ] 0xBE - Assistant Version

### Tracking (Tracking.cs)
- [ ] 0xBF/0x07 - Quest Arrow Click

---

## Outgoing Packets Tracking

### Account (OutgoingAccountPackets.cs)
- [x] 0x1B - Login Confirmation
- [x] 0x53 - Popup Message
- [x] 0x55 - Login Complete
- [x] 0x81 - Change Character
- [x] 0x82 - Account Login Rejected
- [x] 0x85 - Character Delete Result
- [x] 0x86 - Character List Update
- [x] 0x8C - Play Server Ack
- [x] 0xA8 - Account Login Ack (Server List)
- [x] 0xA9 - Character List (VERSION: 7.0.13.0 extended cities)
- [x] 0xB9 - Supported Features (VERSION: 6.0.14.2 extended to 4 bytes)
- [x] 0xBD - Client Version Request

### Movement (OutgoingMovementPackets.cs)
- [x] 0x21 - Movement Rejection
- [x] 0x22 - Movement Acknowledgment
- [x] 0x97 - Move Player
- [x] 0xBF/0x26 - Speed Control
- [x] 0xF2 - Time Sync Response

### Entity (OutgoingEntityPackets.cs)
- [x] 0x1D - Remove Entity
- [x] 0xDC - OPL Info
- [x] 0xF3 - World Entity (VERSION: 7.0.0.0 SA, 7.0.9.0 HS adds 2 bytes)

### Mobile (OutgoingMobilePackets.cs)
- [x] 0x11 - Mobile Status (VERSION: Multiple versions 0-6 based on expansion)
- [x] 0x17 - Mobile Healthbar
- [x] 0x20 - Mobile Update
- [x] 0x2D - Mobile Attributes
- [x] 0x6E - Mobile Animation
- [x] 0x77 - Mobile Moving
- [x] 0x78 - Mobile Incoming (VERSION: 7.0.33.1 always includes hue)
- [x] 0x98 - Mobile Name
- [x] 0xA1 - Mobile Hits
- [x] 0xA2 - Mobile Mana
- [x] 0xA3 - Mobile Stamina
- [x] 0xAF - Death Animation
- [x] 0xBF/0x19 - Bonded Status
- [x] 0xE2 - New Mobile Animation

### Item (OutgoingItemPackets.cs)
- [ ] 0x1A - World Item (pre-SA)

### Container (OutgoingContainerPackets.cs)
- [ ] 0x24 - Display Container (VERSION: 6017 adds gump type)
- [ ] 0x25 - Container Content Update (VERSION: 6017 adds grid location)
- [ ] 0x3C - Container Content (VERSION: 6017 adds grid location per item)
- [ ] 0xBF/0x1B - New Spellbook Content (AOS)

### Message (OutgoingMessagePackets.cs)
- [ ] 0x15 - Follow Message
- [ ] 0x1C - ASCII Message
- [ ] 0xAE - Unicode Message
- [ ] 0xB7 - Help Response
- [ ] 0xC1 - Localized Message
- [ ] 0xC2 - Prompt (outgoing)
- [ ] 0xCC - Localized Message Affix

### Player (OutgoingPlayerPackets.cs)
- [ ] 0x23 - Drag Effect
- [ ] 0x27 - Lift Reject
- [ ] 0x2C - Death Status
- [ ] 0x38 - Pathfind Message
- [ ] 0x3A - Skills Update
- [ ] 0x5B - Current Time
- [ ] 0x65 - Weather
- [ ] 0x6D - Play Music
- [ ] 0x73 - Ping Ack
- [ ] 0x76 - Server Change
- [ ] 0x7B - Sequence
- [ ] 0x88 - Display Paperdoll
- [ ] 0x95 - Display Hue Picker
- [ ] 0xA5 - Launch Browser
- [ ] 0xA6 - Scroll Message
- [ ] 0xBC - Season Change
- [ ] 0xBF/0x19 - Stat Lock Info
- [ ] 0xC8 - Change Update Range
- [ ] 0xD1 - Logout Ack

### Combat (OutgoingCombatPackets.cs)
- [ ] 0x2F - Swing
- [ ] 0x72 - Set War Mode
- [ ] 0xAA - Change Combatant

### Damage (OutgoingDamagePackets.cs)
- [ ] 0x0B - Damage (VERSION: 5.0.0a, or 0xBF/0x22 for older)

### Light (OutgoingLightPackets.cs)
- [ ] 0x4E - Personal Light Level
- [ ] 0x4F - Global Light Level

### Effects (OutgoingEffectPackets.cs)
- [ ] 0x54 - Sound Effect
- [ ] 0x70 - Screen Effect
- [ ] 0xC0 - Hued Effect
- [ ] 0xC7 - Particle Effect

### Target (OutgoingTargetPackets.cs)
- [ ] 0x6C - Target Request/Cancel (VERSION: HS adds 4 bytes to multi target)
- [ ] 0x99 - Multi Target Request

### Equipment (OutgoingEquipmentPackets.cs)
- [ ] 0x2E - Equip Update
- [ ] 0xBF/0x10 - Display Equipment Info

### Vendor (OutgoingVendorBuyPackets.cs, OutgoingVendorSellPackets.cs)
- [ ] 0x3B - End Vendor Buy
- [ ] 0x74 - Vendor Buy List
- [ ] 0x9E - Vendor Sell List

### Map (OutgoingMapPackets.cs)
- [ ] 0xBF/0x08 - Map Change
- [ ] 0xBF/0x18 - Map Patches
- [ ] 0xC6 - Invalid Map

### Menu (OutgoingMenuPackets.cs)
- [ ] 0x7C - Display Item List Menu

### Secure Trade (OutgoingSecureTradePackets.cs)
- [ ] 0x6F - Secure Trade (VERSION: 7.0.45.65 new format)

### Gumps (OutgoingGumpPackets.cs)
- [ ] 0x8B - Display Sign Gump
- [ ] 0xBF/0x04 - Close Gump

### Party (PartyPackets.cs)
- [ ] 0xBF/0x06 - Party packets (multiple sub-types)

### Buff Icons (BuffIconPackets.cs)
- [ ] 0xDF - Buff/Debuff System

### Books (BookPackets.cs)
- [ ] 0x66 - Book Content
- [ ] 0x93 - Book Header (old)
- [ ] 0xD4 - Book Header (new)

### Bulletin Boards (BulletinBoardPackets.cs)
- [ ] 0x71 - Bulletin Board Content

### Maps (MapItemPackets.cs)
- [ ] 0x56 - Map Details
- [ ] 0x90 - Map Display
- [ ] 0xF5 - New Map Display

### Chat (ChatPackets.cs)
- [ ] 0xB2 - Chat Message
- [ ] 0xB5 - Open Chat Window

### Context Menus (ContextMenuSystem.cs)
- [ ] 0xBF/0x14 - Display Context Menu

### Corpse (CorpsePackets.cs)
- [ ] 0x3C - Corpse Content

### House (HousePackets.cs)
- [ ] House Design packets (multiple)

### Boats (BoatPackets.cs)
- [ ] 0xF6 - Move Boat HS

### Quest (MLQuestPackets.cs)
- [ ] Quest-related packets

### Tracking (OutgoingArrowPackets.cs)
- [ ] 0xBA - Quest Arrow

### Character Statue (CharacterStatuePackets.cs)
- [ ] 0xBF/0x20 - Custom House Info

### Weapon Abilities (WeaponAbilityPackets.cs)
- [ ] 0xBF/0x21 - Clear Weapon Ability
- [ ] 0xBF/0x25 - Toggle Special Ability

### Mahjong (MahjongPackets.cs)
- [ ] 0xDA - Mahjong game packets

---

## Questions for Clarification

### Version/Protocol Questions

1. **Drop Request (0x08)**: The code shows `ContainerGridPacketHandler` - is this the 6017 version that adds the grid byte? What was the original format?

2. **Container Content Update (0x25)**: I see the size varies by `ContainerGridLines`. Is it 20 bytes pre-6017 and 21 bytes post-6017?

3. **Container Content (0x3C)**: Same question - does each item entry gain 1 byte for grid position?

4. **Display Container (0x24)**: The code shows 7 or 9 bytes. What's the difference and which version introduced the extra 2 bytes?

5. **Secure Trade (0x6F)**: What fields changed in 7.0.45.65 (NewSecureTrading)?

6. **Multi Target (0x99)**: You mentioned HS adds bytes - is it 26 pre-HS and 30 post-HS?

7. **Damage Packet**: Pre-5.0.0a clients use 0xBF/0x22, post-5.0.0a use 0x0B. Is this correct?

### Enum/Flag Questions

8. **ALRReason (Account Login Rejection)**: Is this a sequential enum (0-3, 254, 255)?

9. **PMMessage (Popup Message)**: Sequential enum (0-7)?

10. **DeleteResultType**: Sequential enum (0-5)?

11. **Direction**: Sequential 0-7 plus 0x80 running flag - should this be documented as enum + bitflag?

12. **Notoriety**: Sequential 0-7?

13. **Mobile Packet Flags**: This appears to be a bitfield (Hidden=0x80, Poisoned=0x04, etc.) - can you confirm all flag values?

14. **FeatureFlags (0xB9)**: This is a bitfield - should we document all the flag values?

15. **CharacterListFlags (0xA9)**: Also a bitfield?

16. **ClientFlags**: Also a bitfield?

### Packet Structure Questions

17. **Book packets (0x66, 0x93, 0xD4)**: These use UTF-8. Is the string format length-prefixed or null-terminated?

18. **Speech packets (0x03, 0xAD, 0x1C, 0xAE)**: What determines ASCII vs Unicode? Is 0x03/0x1C always ASCII and 0xAD/0xAE always Unicode?

19. **Localized Message (0xC1, 0xCC)**: The arguments field - is it Unicode LE with tab separators?

20. **Extended Packet (0xBF)**: Some subpackets like 0x06 (Party) have further sub-types. Should we nest these or list each as separate packets?

21. **Mobile Status (0x11)**: The version is determined by:
    - Version 0 = always used when viewing OTHER mobiles (compact)
    - Versions 3-6 = used when viewing SELF, based on ExpansionInfo.MobileStatusVersion
    Is this correct?

22. **Mobile Incoming (0x78)**: The equipment list - for pre-NewMobileIncoming clients, the hue field is only included if itemId has bit 0x8000 set. For NewMobileIncoming clients, hue is always included. Is this correct?

---

## Schema Updates Needed

### 1. Enum Field Support
```json
{
  "name": "reason",
  "type": "enum",
  "enumType": "sequential",
  "size": 1,
  "values": [
    { "value": 0, "name": "Invalid" },
    { "value": 1, "name": "InUse" },
    { "value": 2, "name": "Blocked" },
    { "value": 3, "name": "BadPass" },
    { "value": 254, "name": "Idle" },
    { "value": 255, "name": "BadComm" }
  ]
}
```

### 2. Bitfield Support
```json
{
  "name": "flags",
  "type": "bitfield",
  "size": 1,
  "flags": [
    { "bit": 0x80, "name": "Hidden" },
    { "bit": 0x40, "name": "Warmode" },
    { "bit": 0x08, "name": "Blessed" },
    { "bit": 0x04, "name": "Poisoned" },
    { "bit": 0x02, "name": "Female" }
  ]
}
```

### 3. Packet Variants Support
```json
{
  "id": "0x11",
  "name": "Mobile Status",
  "variants": [
    {
      "version": "0",
      "name": "Compact (Other Mobile)",
      "condition": "Viewing other mobile",
      "size": 43,
      "fields": [...]
    },
    {
      "version": "3",
      "name": "Basic (Pre-AOS)",
      "condition": "Self, Expansion < AOS",
      "size": 70,
      "fields": [...]
    }
  ]
}
```

---

## Progress Summary
- **Incoming Packets**: 66 documented (including 16 extended 0xBF subpackets)
- **Outgoing Packets**: 79 documented
- **Total**: 145 packets

## Recently Added Files
- `incoming/extended.json` - 0xBF extended command subpackets (16 packets)
- `incoming/items.json` - Lift, Drop, Equip packets
- `incoming/player.json` - Attack, TextCommand, Skills, War mode, etc.
- `incoming/message.json` - ASCII/Unicode speech packets
- `incoming/mobile.json` - Secure trade, rename, profile
- `incoming/vendor.json` - Buy/sell responses
- `incoming/targeting.json` - Target response
- `outgoing/message.json` - ASCII/Unicode messages, localized messages
- `outgoing/player.json` - Stats, skills, weather, music, paperdoll
- `outgoing/container.json` - Container display, content, spellbook
- `outgoing/combat.json` - Swing, war mode, combatant
- `outgoing/effects.json` - Sound, particle effects, screen effects
- `outgoing/targeting.json` - Target request, multi target
- `outgoing/items.json` - World item (pre-SA)
- `outgoing/light.json` - Global/personal light levels
- `outgoing/damage.json` - Damage packets (new and old formats)
