namespace Server.BotAI.V2;

/// <summary>
/// One switchable, ordered piece of the population.
///
/// <para>
/// <b>A module is not the same thing as a folder.</b> A module has start-up work, or a switch worth
/// having, or both. A folder that only offers services to callers — the kit handout, for instance,
/// which does nothing until a bot is born and would break every bot if it were turned off — is not a
/// module, and making it one would be ceremony. The distinction is what keeps this from becoming
/// paperwork.
/// </para>
///
/// <para>
/// <b>Order is declared, not arranged.</b> A module names what it needs ready and the loader works out
/// the sequence. That is the whole point: in the first version the load order was a list of calls held
/// together by fifteen comments saying "this must come after that", and a mistake in it could not be
/// detected by reading — only by noticing, hours later, that something had quietly read an empty list.
/// A declared dependency that cannot be met is a named failure at boot.
/// </para>
/// </summary>
public abstract class BotModule
{
    /// <summary>
    /// Short name. It is the key in the log, the name other modules depend on, and half of the setting
    /// that switches this module off, so it is short and it does not change.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>When this module may run. See <see cref="BotPhase"/>.</summary>
    public abstract BotPhase Phase { get; }

    /// <summary>
    /// Names of the modules this one needs ready before it can start. Empty for most.
    ///
    /// Checked against what actually started, not against what was registered — so a module whose
    /// dependency was switched off, or which failed, does not run either. It is told so by name, which
    /// is the difference between a module that is off and a module that is quietly broken.
    /// </summary>
    public virtual string[] Requires => [];

    /// <summary>
    /// Whether this module ran and finished. Read by the loader when checking somebody else's
    /// <see cref="Requires"/>.
    /// </summary>
    public bool Ready { get; internal set; }

    /// <summary>
    /// Whether this module is switched on, from <c>bots.&lt;name&gt;.enabled</c> in modernuo.json.
    ///
    /// The switch is read by the loader rather than out of the module's own config file, and that is
    /// deliberate: a module cannot read its own file before it starts, and a switch that lives inside
    /// the thing it switches off is a switch with an ordering problem. Balance numbers stay in the
    /// module's own file, where they belong.
    ///
    /// <para>
    /// The reason to want these at all is diagnosis rather than flexibility. When the population does
    /// something inexplicable, halving the number of running modules is a two-minute experiment; the
    /// alternative, which the first version left as the only option, is reading a thirty-seven megabyte
    /// log.
    /// </para>
    /// </summary>
    public bool Enabled { get; internal set; } = true;

    /// <summary>Do the work. Called once, in the module's phase, after everything it requires is ready.</summary>
    public abstract void Start();

    /// <summary>
    /// Put counters and accumulated state back to nothing. Called when the world is reloaded, before
    /// the world phase runs again.
    ///
    /// Worth being part of the contract rather than left to each module's conscience: totals that are
    /// never reset are lifetime-of-process figures wearing the name of a population count, and they do
    /// not look wrong — they look like a population twice the size.
    /// </summary>
    public virtual void Reset()
    {
    }
}
