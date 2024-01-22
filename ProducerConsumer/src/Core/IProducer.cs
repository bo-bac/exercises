namespace Core;

public interface IProducer
{
    Task Produce();
    Summary GetProducedSummary();
}