# GOALS

The goal of ModernUO is a complete rewrite of RunUO that focuses on performance.
Some of the many high level goals include:

### Architecture
- [x] Upgrade Server/Scripts to target .NET Core 3
- [X] Upgrade the code to use C# 8 standards
- [X] Remove compilation of scripts at startup in favor of loading DLLs.
- [X] Support configuration via a `modernuo.json` file

### Networking
- [ ] Replace Packet classes with functions
- [X] Improve asynchronous socket handling using Pipes
- [X] Improve socket handling (2-5x) and event loop using libuv

### Administration
- [ ] Move IP logging and account data to SQL
- [ ] Move account management to web

### Serialization & World Saves
- [ ] Create a flag to indicate when objects will be serialized.
  * Players, all items held by players, houses, etc.
- [ ] Create transactions which handle changes to objects including:
  * Transitioning from not serialized to serialized (created)
  * Transitioning from serialized to not serialized (deleted)
  * Property value changes
- [ ] Create asynchronous request from cold storage
    * External system must provide objects and their nested objects (e.g. containers)

### Optimizations for large shards
- [ ] Eject inactive players and their items after they are serialized to cold storage
- [ ] Create hydration techniques for non-persisted systems (e.g. spawners, decorations, static, champions, etc)
- [ ] Simplify certain items by eliminating all of their individual types and using an enum to reference their individual type
  * Items subject to change: reagents, logs, ore, ingots, potions, and containers
  * Syntax change: `new MandrakeRoot()` -> `new Reagent(ReagentType.MandrakeRoot)`
- [ ] Create object pools for high availability items such as gold and reagents
  * For example, `new MandrakeRoot()` -> `ObjectPool.Get<Reagent>(ReagentType.MandrakeRoot)`.
  * Pools should be elastic and adjust according to nominal usage. For example, if thousands of gold objects are created and destroyed in a small period of time, the pool should be expanded and replenished properly so it is never empty, or full.
- [ ] Replace timer system with libuv implementation
- [X] Create `DefaultName` for mobiles

### Plugins (Separate Repos & Optional)
- [ ] RunUO 2.0 Migration Utility
  * Template code for updating serializations
  * Convert World file and dump to new serialization technique if there is one
- [ ] Login/Auth Gateway (Like OSI!)
  * Use central AuthID via redis or similar
  * Support 2FA Auths via at login gump
- [ ] 2FA Auth at Login to prevent password theft
  * Support Authy via universal interface
  * Support external app/device, magic number email, SMS, and FCM
  * Support user identity via web browser to curb flooding
- [ ] Serialization via Database (no world saves)
  * Asynchronously offload the transaction to another service for permanent serialization
  * Support basic KVP databases such as MongoDB, RocksDB, and Redis, etc.

### Bugs & Misc
- [X] Rounding errors with new bank system
- [X] Replace specific checks for `Item`/`Mobile` by extended `IEntity` and mainstreaming the `NEWPARENT` code
- [ ] TryDropItem dropping items until it fails when it should be all-or-nothing
- [ ] Consumable items are deleted directly in several places instead of using `Item.Consume()`
- [ ] Stack/Hold checks are broken in certain situations
