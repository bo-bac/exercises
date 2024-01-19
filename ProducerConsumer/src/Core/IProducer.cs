namespace Core;

public interface IProducer
{
    void Produce();
    Summary GetProducedSummary();
}