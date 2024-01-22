namespace Core;

public interface IConsumer : IAsyncDisposable
{
    Task StartConsume();
    Task StopConsume();

    string GetStatistics();
}
