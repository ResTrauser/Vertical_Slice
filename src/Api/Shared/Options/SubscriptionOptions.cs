namespace Api.Shared.Options;

public sealed class SubscriptionOptions
{
    public DowngradeMode DowngradeMode { get; init; } = DowngradeMode.Block;
}

public enum DowngradeMode
{
    Block = 1,
    Enforce = 2
}
