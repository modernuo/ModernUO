namespace BuildTool.Prerequisites;

public sealed class PrerequisiteResult
{
    public required string Name { get; init; }
    public required bool Passed { get; init; }
    public string? Details { get; init; }
    public string? InstallCommand { get; init; }
    public string? DownloadUrl { get; init; }
    public bool IsWarning { get; init; }
}
