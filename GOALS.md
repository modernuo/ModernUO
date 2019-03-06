# GOALS

### Architecture
- [x] Upgrade Server/Scripts compiler to use Roslyn/C# 7
- [ ] Upgrade the code to use C# 7 standards

### Administration
- [ ] Move IP logging and account data to SQL
- [ ] Move account management to web

### Serialization & World Saves
- [ ] Remove Serialization/Deserialization entirely
- [ ] Create a flag to indicate when objects will be serialized.
  * Players, all items held by players, houses, etc.
- [ ] Create transactions which handle changes to objects including:
  * Transitioning from not serialized to serialized (created)
  * Transitioning from serialized to not serialized (deleted)
  * Property value changes
- [ ] Asynchronously offload the transaction to another service for permanent serialization
  * Support basic KVP databases such as MongoDB, RocksDB, and Redis
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
- [ ] Replace timer system with a [wheel](https://github.com/runuo/runuo/pull/42) implementation
- [X] Create `DefaultName` for mobiles
- [ ] Improve asynchronous socket handling

### Code Architecture Fixes
- [ ] Create a LayeredItem child and move related code to that class. All layered items should inherit that.
  * Items that are placed on the paperdoll will need to inherit this.
- [ ] Create a StackedItem child and move related code to that class.
  * Items that have an amount will need to inherit this.

### Bugs & Misc
- [ ] Naked corpses (container display issue)
- [X] Rounding errors with new bank system
- [ ] Replace `double` with `int` for skills to fix rounding errors and optimize code
- [X] Replace specific checks for `Item`/`Mobile` by extended `IEntity` and mainstreaming the `NEWPARENT` code
- [ ] TryDropItem dropping items until it fails when it should be all-or-nothing
- [ ] Consuming items are deleted directly in several places instead of using `Item.Consume()`
- [ ] Content displaying for newer clients is not working properly in several situations
- [ ] Stack/Hold checks are broken in certain situations
