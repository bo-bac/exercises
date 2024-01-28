namespace WebApi.Middlewares.RequestThrottling;

public enum RequestThrottlingLimitExceededPolicy
{
    Drop,
    UseQueueDropTail,
    UseQueueDropHead
}

public class RequestThrottlingOptions
{
    public const int UNLIMITED = -1;

    private int _limit;
    private int _maxQueueLength;
    private int _maxTimeInQueue;


    public RequestThrottlingOptions()
    {
        _limit = UNLIMITED;
        LimitExceededPolicy = RequestThrottlingLimitExceededPolicy.Drop;
        _maxQueueLength = 0;
        _maxTimeInQueue = UNLIMITED;
    }

    public int Limit
    {
        get { return _limit; }
        set { _limit = value < UNLIMITED ? UNLIMITED : value; }
    }

    public RequestThrottlingLimitExceededPolicy LimitExceededPolicy { get; set; }

    public int MaxQueueLength
    {
        get { return _maxQueueLength; }
        set { _maxQueueLength = value < 0 ? 0 : value; }
    }

    public int MaxTimeInQueue
    {
        get { return _maxTimeInQueue; }
        set { _maxTimeInQueue = value <= 0 ? UNLIMITED : value; }
    }
}
