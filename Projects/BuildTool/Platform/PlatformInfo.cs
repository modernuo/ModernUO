namespace BuildTool.Platform;

public enum PackageManager
{
    Unknown,
    Apt,
    Dnf,
    Zypper,
    Pacman,
    Apk,
    Brew
}

public sealed class PlatformInfo
{
    public required string OsName { get; init; }
    public required string OsRid { get; init; }
    public required string ArchRid { get; init; }
    public string Rid => $"{OsRid}-{ArchRid}";
    public string? DistroId { get; init; }
    public string? DistroName { get; init; }
    public string? KernelVersion { get; init; }
    public PackageManager PackageManager { get; init; } = PackageManager.Unknown;

    public bool IsWindows => OsRid == "win";
    public bool IsMacOS => OsRid == "osx";
    public bool IsLinux => OsRid == "linux";
}
