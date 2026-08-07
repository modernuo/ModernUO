namespace BuildTool;

public enum BuildAction
{
    Publish,
    Migrate
}

public sealed class BuildOptions
{
    public BuildAction Action { get; set; } = BuildAction.Publish;
    public string Config { get; set; } = "Release";
    public string? Os { get; set; }
    public string? Arch { get; set; }
    public bool SkipPrereqs { get; set; }
    public bool Interactive { get; set; }

    /// <summary>
    /// Report the native library prerequisites and exit. The interactive flow is the only other
    /// path that runs these checks, so without this there is no way to verify a deployment target
    /// from a script or a container.
    /// </summary>
    public bool CheckPrereqsOnly { get; set; }
}
