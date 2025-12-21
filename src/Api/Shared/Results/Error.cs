namespace Api.Shared.Results;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new("none", string.Empty);
}
