using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace HyperLinkDisarmer;

public class Service
{
    private readonly Options _options;
    private readonly ILogger<Service> _logger;

    public Service(
        ILogger<Service> logger,
        IOptions<Options> options) =>
        (_options, _logger) = (options.Value, logger);

    public async Task DoAsync(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var i = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var fileName = Path.GetFileName(path);
                var content = await File.ReadAllTextAsync(path, cancellationToken);
                var disarmed = Regex.Replace(content, @"<a[^>]*?href=""(.*?)""*?>([^<]+)", m =>
                    (Regex.IsMatch(m.Groups[2].Value, @"^http(s)?://([\w-]+.)+[\w-]+(/[\w- ./?%&=])?$")
                        && m.Groups[2].Value != m.Groups[1].Value)
                            ? m.Value.Replace(m.Groups[1].Value, m.Groups[2].Value)
                            : m.Value
                );

                var output = Path.Combine(Path.GetFullPath(_options.TargetPath), fileName);
                await File.WriteAllTextAsync(output, disarmed, cancellationToken);
                File.Delete(path);

                _logger.LogInformation("{action}: {file}", content != disarmed ? "Disarmed" : "Processed", path);
                break;
            }
            //catch (FileNotFoundException)
            //{                    
            //}
            catch (IOException e)
            {
                _logger.LogWarning("IOException[{retry}]: {message} : {file}", ++i, e.Message, path);
            }
            //catch (UnauthorizedAccessException)
            //{
            //}
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot process '{file}': {Message}", path, ex.Message);
                break;
            }

            await Task.Delay(500, cancellationToken);
        }
    }
}