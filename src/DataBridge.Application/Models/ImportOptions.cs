namespace DataBridge.Application.Models;

public sealed record ImportOptions(
    string FilePath,
    string RejectFilePath,
    bool DryRun = false,
    bool Force = false,
    int BatchSize = 1_000);
