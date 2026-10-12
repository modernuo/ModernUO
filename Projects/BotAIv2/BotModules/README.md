# Modules

The loading frame. Holds the list of subsystems, works out the order to start them in, starts them, and says
what happened.

| File | What is in it |
|---|---|
| `BotPhase.cs` | two phases: before the world and after it |
| `BotModule.cs` | the contract: name, phase, dependencies, a switch, `Start`, `Reset` |
| `BotModules.cs` | the registry, the topological order, the run, the report |

This folder has no configuration file of its own: its only setting is each module's switch, and that lives in
`modernuo.json` as `bots.<name>.enabled`. Why it is not in the subsystem's own file is below.

## Order is declared, not arranged

A module says what it needs ready; the loader derives the sequence. The registration list in `BotCore` is "what
exists", and its order means nothing.

It is built this way because in the first version the list **was** the order: thirty calls held together by
fifteen comments of the form "this has to come after that". A mistake in such a sequence cannot be found by
reading. The worst of them cost eleven `null`s in a recipe index — it was built during the configuration step,
and the shard's craft tables are created later. There was nothing wrong with the code: it asked a question
before the world could answer, and the answer to a question asked too early is not an error, it is an empty
list.

Hence the two phases. `Settings` is numbers, files and settings: everything knowable without looking at the map.
`World` is everything that asks where something stands, whether a tile can be walked, which region a point is
in.

## Failure is loud and local

| What happened | What the log says |
|---|---|
| module switched off | `Module Trade is switched off` |
| a dependency is not ready | `Module Trade needs Classes, which is not ready; Trade did not start` |
| the module threw | `Module Trade threw while starting; it is not running` + the stack |
| two modules with one name | `Two modules are both called Trade (...); the second is ignored` |
| circular dependency | `Module Trade is part of a circular dependency` — the ring is broken and each member refuses by name |

Named from both ends on purpose: "Trade did not start" is a riddle, while "Trade needs Classes, which is not
ready" is a sentence somebody can act on.

An unknown name in a dependency list counts as unsatisfied and means something different: not "it fell over"
but "you depend on something that does not exist" — a typo rather than a breakage.

## A switch on each one

`bots.<name>.enabled` in `modernuo.json`, read by the loader at registration.

**Why not in the subsystem's own file:** a module cannot read its file before it starts, and a switch that
lives inside the thing it switches off is a switch with an ordering problem. Balance numbers stay in the
subsystem's file, where they belong.

**What they are actually for** is diagnosis rather than flexibility. When a population does something
inexplicable, halving the number of running modules is a two-minute experiment. The first version left exactly
one method available: read a 37 MB log.

## A module is not the same thing as a folder

A module has work to do at startup, or a switch worth having, or both.

A folder that only offers services to a caller is **not** a module. `BotOutfit` is exactly that: it does nothing
until a bot is born, and switching it off would break every bot rather than disable one capability. Making it a
module would be filing a form. That distinction is the only thing keeping the module frame from turning into
bureaucracy.

If the bind ever needs a switch of its own — to check whether weightlessness is behind some oddity, say — it
will be a dial in `BotOutfit`'s configuration, not a separate module.

## Reloading the world

`WorldLoad` can happen more than once. So the `World` phase is restartable: `Rewind(BotPhase.World)` calls
`Reset()` on every running module of that phase **and clears their ready mark**.

The second half is the one that is easy to forget, and I forgot it on the first pass. A reset without clearing
the mark would mean that on a world reload the counters zero and `Start` skips everybody as already ready: a
shard with no population and not one line explaining why.

The `Settings` phase is not restarted. A world reload is a second world, not a second process: what was read out
of the settings file is still true, and re-reading it would at best do the work twice and at worst apply the
configuration overrides a second time.

## How to add a module

Three lines in the subsystem and one in `BotCore`:

```csharp
public sealed class BotTradeModule : BotModule
{
    public override string Name => "Trade";
    public override BotPhase Phase => BotPhase.World;
    public override string[] Requires => ["Classes"];
    public override void Start() { /* ... */ }
    public override void Reset() { /* counters back to zero */ }
}
```

```csharp
BotModules.Register(new BotTradeModule());
```

A module's descriptor lives **in the subsystem's folder**, not here. The loader has no business knowing what a
role or an order is; a module that cannot report on itself would have to be reported on by something that
reaches inside it — and that is exactly how a loader turns into the thing it was written to replace.
