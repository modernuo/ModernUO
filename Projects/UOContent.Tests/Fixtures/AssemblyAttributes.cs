using Xunit;

// Every ModernUO test touches the same process-global singletons (Core, World, Timer,
// NetState, serialization workers). The global bootstrap is designed to happen once per
// process (see TestServerBootstrap), so collections must never run concurrently. Force the
// whole assembly to run test collections strictly sequentially.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
