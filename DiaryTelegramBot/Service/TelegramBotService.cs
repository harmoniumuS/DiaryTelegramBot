using DiaryTelegramBot.Data;
using DiaryTelegramBot.Handlers;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Options;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

public class TelegramBotService : IHostedService
{
    private readonly ILogger<TelegramBotService> _logger;
    private readonly TelegramOptions _telegramOptions;
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private CancellationTokenSource _cts;

    public TelegramBotService(ITelegramBotClient botClient, ILogger<TelegramBotService> logger, IOptions<TelegramOptions> telegramOptions,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _telegramOptions = telegramOptions.Value;
        _botClient = botClient;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Bot starting...");
        _cts = new CancellationTokenSource();
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated(); 
        }
        try
        {
            await StartBotUpdatesAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while starting the bot.");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Bot is stopping...");
        _cts?.Cancel();
        await Task.CompletedTask;
    }

    private async Task StartBotUpdatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            int offset = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                var updates = await _botClient.GetUpdates(offset, 100, cancellationToken: cancellationToken);

                foreach (var update in updates)
                {
                    using (var scope = _serviceScopeFactory.CreateScope()) 
                    {
                        var messageHandler = scope.ServiceProvider.GetRequiredService<MessageHandler>(); 
                        await messageHandler.HandleUpdateAsync(_botClient, update, cancellationToken);
                    }
                }
                if (updates.Length > 0)
                {
                    offset = updates[updates.Length - 1].Id + 1;
                }

                await Task.Delay(1000, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while handling updates.");
        }
    }
}
