using DiaryTelegramBot.Handlers;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Options;
using DiaryTelegramBot.Wrappers;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace DiaryTelegramBot
{
    public class TelegramBotService : BackgroundService
    {
        private readonly ILogger<TelegramBotService> _logger;
        private readonly TelegramOptions _telegramOptions;
        private readonly MessageHandler _messageHandler;
        private readonly BotClientWrapper _clientWrapper;
        private readonly ITelegramBotClient _botClient;

        public TelegramBotService(ITelegramBotClient botClient,ILogger<TelegramBotService> logger,IOptions<TelegramOptions> telegramOptions,
            MessageHandler messageHandler,BotClientWrapper botClientWrapper)
        {
            _logger = logger;
            _telegramOptions = telegramOptions.Value;
            _messageHandler = messageHandler;
            _clientWrapper = botClientWrapper;
            _botClient = botClient;
            
        }
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
         
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var updates = await _botClient.GetUpdatesAsync(cancellationToken: cancellationToken);

                    foreach (var update in updates)
                    {
                        await HandleUpdateAsync(_botClient, update, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ќшибка при обработке обновлений Telegram");
                    await ErrorHandler(_botClient, ex, cancellationToken);
                }

                // ∆дем немного перед следующим запросом обновлений
                await Task.Delay(1000, cancellationToken);
            }
        }
        private  async Task HandleUpdateAsync(ITelegramBotClient botClient, Telegram.Bot.Types.Update update, CancellationToken cancellationToken)
        {
            if (update.Message == null) return;

            var chatId = update.Message.Chat.Id;

            if (update.Type == UpdateType.Message && update.Message.Text == "/start")
            {
                await BotKeyboardManager.SendMainKeyboardAsync(botClient, chatId, cancellationToken);
            }
        }
        private static Task ErrorHandler(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            
            return Task.CompletedTask;
        }
    }
}
