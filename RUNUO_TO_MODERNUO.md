# From RunUO to ModernUO
RunUO was built using C# from .NET 1.1 in 2002. There have been massive changes to technology in the past 20+ years.
We believe it is time for Ultima Online to take advantage of this technology so a server can provide a richer experience with never before seen scale.

While it is possible (and many have done it) to migrate an _active server_ from RunUO to ModernUO, it is a daunting task.
Please ask for help in our discord!

## Technology
|              | RunUO                                                                                                 | ModernUO                                                    |
|:-------------|:------------------------------------------------------------------------------------------------------|:------------------------------------------------------------|
| Language     | C# 4                                                                                                  | C# 11 (.NET 8)                                            |
| Supported OS | 32 & 64bit Windows or Mono                                                                            | 64bit Windows, MacOS & Linux                                |
| IDEs         | [VS](https://visualstudio.microsoft.com/downloads/), [VSCode](https://code.visualstudio.com/download) | VS 2022+ or [Rider 2023+](https://www.jetbrains.com/rider/) |

## Code API Changes
* **ModernUO can use source generators to [serialization/deserialization](https://github.com/modernuo/SerializationGenerator#basic-usage) automatically.**
  * This is the biggest change to the API! While intimidating and different, it unlocks the ability for ModernUO to only serialize _data that has changed_.
  * We estimate a world save with 10mill objects on a busy server will take less than 1 second.
  * This feature is _optional and will remain optional indefinitely_.
* `Serialize(GenericWriter writer)` and equiv deserialize was changed to `Serialize(IGenericWriter writer)`.
* The following now use generics, e.g. `BeginAction(typeof(X))` is now `BeginAction<X>`.
  * CanBeginAction, BeginAction and EndAction
  * FindRegion and IsPartOf
  * FindGump, HasGump, and CloseGump
* Functions such as `OnAdded(object)` for both Items/Mobiles are now `OnAdded(IEntity)`.
* Most `delegate` have been changed to `Action`.
  * Example: `EventSink.PlayerDeath += new PlayerDeathEventHandler(EventSink_PlayerDeath);` is now `EventSink.PlayerDeath += EventSink_PlayerDeath;`.
* `ObjectPropertyList` is now `IPropertyList`,
  * Example: `GetProperties(ObjectPropertyList list)` is now `GetProperties(IPropertyList list)`.
* `[Constructable]` attribute is now `[Constructible]`.

### Object Property List API Changes
ObjectPropertyList has been drastically optimized, which means the API has been modernized:

❌ _Not valid_
```cs
list.Add(1061837, "{0}\t{1}", m_CurArcaneCharges, m_MaxArcaneCharges);
```
✅
```cs
list.Add(1061837, $"{_curArcaneCharges}\t{_maxArcaneCharges}");
```

Clilocs that use arguments:

❌ _Not valid_
```cs
list.Add(1060659, "Level\t{1}", m_Level);
```
✅ - _Note the string as an argument, this is mandatory!_
```cs
list.Add(1060659, $"{"Level"}\t{Level}"); // ~1_val~: ~2_val~
```

Clilocs that use other clilocs as an argument:

❌ _Do not prepend #_
```cs
list.Add(1060830, $"#{dirt.ToString()}");
```
✅ _Use the new custom cliloc argument formatter_
```cs
list.Add(1060830, $"{dirt:#}");
```

## Core Changes
* ModernUO is not inherently thread safe. Overall infrastructure improved with CPU and minimizing memory garbage in mind.
* Timer system improved drastically by using [Timer Wheels](http://www.cs.columbia.edu/~nahum/w6998/papers/sosp87-timing-wheels.pdf).
* Networking is 5-10x faster by using fixed [Circular Buffers](https://en.wikipedia.org/wiki/Circular_buffer).
* Network packets are no longer objects and write directly to the network buffer, improving performance by 10x.
* World saves are 30x faster by saving to memory and flushing to disk in the background.
* Improved RNG accuracy and performance by 5x using [Xoshiro256++](https://prng.di.unimi.it/)
* Converted quite a bit of configuration to JSON with a central settings file.
* Logging changed from Console.WriteLine to Serilog (still work in progress).
* Eliminated calling `DateTime.Now` which had a huge performance penalty.
* Accounts are now saved to a binary file (will eventually change to a databse) to improve world save performance by eliminating XML.
* Strings are now built using the new interpolated string syntax, and highly performant string builders.

## New Features
* IPv6 support.
* Generic serialization that is automatically done in parallel with the world save.
  * The [faction system](https://github.com/modernuo/ModernUO/blob/7adf52ef48df7ae2b034c27e67b0c332b37fb053/Projects/UOContent/Engines/Factions/Core/FactionSystem.cs#L15) is no longer powered by a single item and instead uses generic persistence.
* Timezone support for custom scripts that might need it.
* Hourly/daily/weekly/monthly backups & archiving using [Z-Standard](https://facebook.github.io/zstd).
* Client version detection (including CUO) for easy configuration.
* Localization (Cliloc) support.
* Better encryption for passwords using [Argon2](https://en.wikipedia.org/wiki/Argon2).
* Packet throttling that is configurable per packet and per connection.
* Enable packet logging per connection.
* Owner accounts can be protected from being locked out. The first account created is automatically added to this list.
* Use optional arguments from constructors for Add command.
* Captures Razor version and displays it in client gump
* Spawners can be exported/imported using commands and use a GUID for replacement.

## Major Feature Changes
* By default, world saves are now every 5th minute of a real world hour.
  * E.g. 5:00, 5:05, 5:10, regardless of when the server is booted.
* Some items have their constructor arguments rearranged.

## Changes to UO Mechanics
* Min/max skill requirements for magery adjusted to be accurate to OSI
* Buffs/Curses now apply appropriately

## Removed Features
* Reporting
* Remote Admin
* My RunUO
* Event Log
* DocsGen command

## New Development Features & Changes
* Added a shared list/queue for temporary processing such as accumulating players to damage/kill.
  * [PooledRefList](https://github.com/modernuo/ModernUO/blob/main/Projects/Server/Collections/PooledRefList.cs)
  * [PooledRefQueue](https://github.com/modernuo/ModernUO/blob/main/Projects/Server/Collections/PooledRefQueue.cs)
* Adds HashSet that is ordered by insertion time
  * [OrderedHashSet](https://github.com/modernuo/ModernUO/blob/main/Projects/Server/Collections/OrderedHashSet.cs)
  * [PooledOrderedHashSet](https://github.com/modernuo/ModernUO/blob/main/Projects/Server/Collections/PooledOrderedHashSet.cs)
* Adds [StringBuilder](https://github.com/modernuo/ModernUO/blob/main/Projects/Server/Buffers/ValueStringBuilder.cs) that is fast and does not impose garbage collection.
* Adds performant [JSON](https://github.com/modernuo/ModernUO/blob/main/Projects/Server/Json/JsonConfig.cs) support with simple API.
* Adds performant, thread _unsafe_, [ArrayPool](https://github.com/modernuo/ModernUO/blob/main/Projects/Server/Buffers/STArrayPool.cs)
* Adds highly performant and easy to use converters for [HexString](https://github.com/modernuo/ModernUO/blob/main/Projects/Server/Text/HexStringConverter.cs) representation of data
