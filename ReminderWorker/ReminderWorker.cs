using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReminderWorker.Services;
using ReminderWorker.Settings;
using Telegram.Bot;

namespace ReminderWorker;
public class ReminderWorker:BackgroundService
{
    public ILogger<ReminderWorker> Logger { get; }
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<RemindsSettings> _reminderSettings;
    private readonly ILogger _logger;
    private readonly IOptions<RemindsSettings> _settings;
    public ReminderWorker(IServiceProvider serviceProvider,ITelegramBotClient botClient,
    ILogger<ReminderWorker> logger, IOptions<RemindsSettings> settings)
    {
        Logger = logger;
        _settings = settings;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("ReminderService: ExecuteAsync стартовал");
        while (!cancellationToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var services = scope.ServiceProvider.GetRequiredService<RemindsService>();
            try
            {
                await services.SendAllReminds();
            }
            catch (AggregateException ex)
            {
                foreach (var inner in ex.InnerExceptions)
                {
                    _logger.LogError(inner,inner.Message);
                }
            }   

            await Task.Delay(_settings.Value.Delay, cancellationToken);
        }
    }
}