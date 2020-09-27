### Migrating from RunUO 2.0
There are several considerations before migration from RunUO 2.0 to ModernUO.
Many of these issues can be fixed by writing scripts that do world file fixes.
Others will require careful code review to fix compile/runtime issues.

We will introduce templates to create migration scripts in a future iteration.

#### Deserialization Issues (Startup Issues)
* All BODs will fail to deserialize due to the introduction of BaseBOD
  * Recommend a migration script to fix this.
* Serial is now a `uint` which could break certain scripts expecting negative numbers or fixed encoding.

#### Constructor Inheritance (Runtime Errors, Corruption, or Crashes)
* Items that inherit `Food` will pass the wrong arguments to their parent constructor.
* Items that inherit spells or scrolls will pass the wrong arguments to their parent constructors.

#### Code Changes (Possible Compile Errors)
* The following functions no longer take a Type as their argument and now utilize generics:
  * CanBeginAction, BeginAction and EndAction
  * FindRegion and IsPartOf
  * FindGump, HasGump, and CloseGump
* Timers use lambda functions instead of callback objects with generic `object state` argument.
* Various functions such as `OnAdded` which previously took `object parent` now take `IEntity`
* Functions that utilized non-generic structures (ArrayList, etc) have been changed to use generics.

### Profiling
* Disabled

#### Reports
* Removed

#### RemoteAdmin
* Removed

#### MyRunUO
* Removed

#### EventLog
* Removed

### Future Migration Concerns

#### Account System
* Accounts from the XML format will need to be migrated the new interface/format.
* IP Logs from the Accounts XML will need to be offloaded to a new database.

#### Deserialization
* All resources including but not limited to the following will need to be migrated:
  * Gold, Silver, Platinum
  * Arrow, Bolt, Feature, Shaft
  * BaseOre, BaseIngot, BaseScale and all children
  * BigFish, Fish, MagicFish
  * Granite, Sand
  * BaseReagent and all children
  * BoltOfCloth, Bone, Cloth, Cotton, Flax, BaseHides, BaseLeather, UncutCloth, Wool, YarnAndThreads and all children
  * ML Resources (Check Scripts/Items/Resources/MiscMLResources.cs on RunUO 2.0)
  * Log and Board
  * Bottle
  * Solen Items (e.g. ZoogiFungus)
  * Other commodity types I can't remember
* Spawners will need to be migrated since the old Spawner has been replaced and deserialization is not guaranteed.

#### Timers
* Scripts that rely on Timers, TimerPriority, etc will need to be fixed to work with the format.

#### Fastwalk
* Other fastwalk/speedhack detection systems will need to be deleted or fixed.

#### Packets
* Packet, ByteQueue, PacketWriter, and PacketReader have been removed.
* `ns.Send(packet)` has been changed to `Packets.Send(ns, PacketFunc, arg1, arg2...)`
* Packets that are not used in the core have been moved to content distribution
* God client packets have been removed
