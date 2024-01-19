namespace Core;

public interface IConsumer : IDisposable
{
    void StartConsume();
    void StopConsume();

    string GetStatistics();
}
