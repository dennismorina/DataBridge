namespace DataBridge.Domain;

public sealed class DomainValidationException : Exception
{
    public DomainValidationException(params string[] errors)
        : base(string.Join(" ", errors))
    {
        Errors = errors;
    }

    public IReadOnlyCollection<string> Errors { get; }
}
