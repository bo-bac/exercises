namespace Core;

public class Options
{
    public string AppId { get; set; } = "PC";

    public string BrokerHost { get; set; } = "localhost";
    public string QueueName { get; set; } = "hashes";

    /// <summary>
    /// sec
    /// </summary>
    public int NetworkRecoveryInterval { get; set; } = 10;

    /// <summary>
    /// sec
    /// </summary>
    public int ContinuationTimeout { get; set; } = 60;

    public int BatchSize { get; set; } = 1000;

    public int XConsumers { get; set; } = 1;
    public int XPublishers { get; set; } = 1;
}
