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
}
