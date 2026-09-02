namespace DataBridge.Cli;

internal sealed record CliOptions(
    string FilePath,
    string RejectFilePath,
    string? ConnectionString,
    bool DryRun,
    bool Force,
    int BatchSize);
