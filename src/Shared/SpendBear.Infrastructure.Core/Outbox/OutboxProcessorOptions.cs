namespace SpendBear.Infrastructure.Core.Outbox;

public class OutboxProcessorOptions
{
    public int PollingIntervalMs { get; set; } = 1000;
    public int BatchSize { get; set; } = 50;
    public int MaxRetryCount { get; set; } = 5;
    public int RetentionDays { get; set; } = 7;
}
