using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace HyperLinkDisarmer;

public sealed class Worker : BackgroundService
{
    private const string HTML = "*.html";

    private bool _disposed;
    private readonly FileSystemWatcher _watcher;
    private readonly Options _options;
    private readonly Service _service;
    private readonly ILogger<Worker> _logger;
    private readonly BlockingCollection<string> _queue;

    public Worker(
        IOptions<Options> options,
        Service service,
        ILogger<Worker> logger)
    {
        _options = options.Value;
        _service = service;
        _logger = logger;

        if (!Directory.Exists(_options.SourcePath))
        {
            throw new ArgumentException("Source directory {path} does not exist.", _options.SourcePath);
        }

        _queue = [];

        _watcher = new FileSystemWatcher
        {
            Path = _options.SourcePath,
            Filter = HTML
        };
        _watcher.Created += (object sender, FileSystemEventArgs args) =>
        {
            _queue.Add(args.FullPath);
        };
        _watcher.Error += (object sender, ErrorEventArgs args) => _logger.LogError("The file system watcher experienced an error: {error}", args.GetException().Message);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Watching {path} ...", _options.SourcePath);
            _watcher.EnableRaisingEvents = true;

            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (string path in _queue.GetConsumingEnumerable(cancellationToken))
                    await _service.DoAsync(path, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);

            // Terminates this process and returns an exit code to the operating system.
            // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
            // performs one of two scenarios:
            // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
            // 2. When set to "StopHost": will cleanly stop the host, and log errors.
            //
            // In order for the Windows Service Management system to leverage configured
            // recovery options, we need to terminate the process with a non-zero exit code.
            Environment.Exit(1);
        }
    }

    #region Dispose

    public new void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _watcher?.Dispose();
        _queue?.Dispose();
        _disposed = true;

        base.Dispose();
    }

    #endregion /Dispose
}
