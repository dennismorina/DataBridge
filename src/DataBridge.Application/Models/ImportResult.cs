namespace DataBridge.Application.Models;

public sealed record ImportResult(
    string FileName,
    string SourceHash,
    int TotalRows,
    int AcceptedRows,
    int RejectedRows,
    int UpsertedRows,
    string? RejectFilePath,
    bool DryRun,
    bool SkippedAlreadyProcessed,
    TimeSpan Duration);
