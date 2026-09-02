namespace DataBridge.Application.Models;

public sealed record ImportJob(
    Guid Id,
    string FileName,
    string SourceHash,
    string Status,
    int TotalRows,
    int AcceptedRows,
    int RejectedRows,
    int UpsertedRows,
    long DurationMilliseconds,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    string? ErrorMessage);
