using DiaryTelegramBot.Options;
using Microsoft.Extensions.Options;
using System;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DiaryTelegramBot
{
    public class TelegramBotService : BackgroundService
    {
        private readonly ILogger<TelegramBotService> _logger;
        private readonly TelegramOptions _telegramOptions;

        public TelegramBotService(ILogger<TelegramBotService> logger,IOptions<TelegramOptions> telegramOptions)
        {
            _logger = logger;
            _telegramOptions = telegramOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var botClient = new TelegramBotClient(_telegramOptions.Token);
            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = []
            }; 

            while (!stoppingToken.IsCancellationRequested)
            {
                await botClient.ReceiveAsync(UpdateHandler,ErrorHandler, receiverOptions,stoppingToken);
  
            }
        }
        private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message)
            {
                return;
            }
            if (message.Text is not { } messageText)
            {
                return;
            }
            var chatId = message.Chat.Id;
            Console.WriteLine($"Received a'{message.Text}' message in chat{chatId}");

            switch (update.Type)
            {
                case UpdateType.Message:
                    {
                        var user = message.From;
                        switch (message.Type)
                        {
                            case MessageType.Text:
                                if (message.Text == "/start")
                                {
                                    await botClient.SendTextMessageAsync(
                                        chatId,
                                        "������ ����������:\n" +
                                        "/inline\n" +
                                        "/reply\n",
                                        cancellationToken:cancellationToken);
                                }
                                if (message.Text == "/inline")
                                {
                                    var inlineKeyboard = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>() 
                                        {
                                        new InlineKeyboardButton[] // ��� ������� ������ ������
                                        {
                                            InlineKeyboardButton.WithCallbackData("��� ������ � ������", "data1"),
                                            InlineKeyboardButton.WithCallbackData("� ��� ������ ������", "data2"),
                                        },
                                        });

                                    await botClient.SendTextMessageAsync(
                                        chatId,
                                        "��� inline ����������!",
                                        replyMarkup: inlineKeyboard,
                                        cancellationToken:cancellationToken);
                                    return;
                                }
                                if (message.Text == "/reply")
                                {
                                    var replyKeyboard = new ReplyKeyboardMarkup(
                                    new List<KeyboardButton[]>()
                                    {
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("������!"),
                                            new KeyboardButton("����!"),
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("��� ����?")
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("��� ������!")
                                        }
                                    }){ ResizeKeyboard = true};
                                    await botClient.SendTextMessageAsync(
                                        chatId,
                                        text: "��� reply ����������:",
                                        replyMarkup: replyKeyboard);
                                }
                                return;
                        }
                    }
                Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "You said:\n" + messageText,
                cancellationToken: cancellationToken);
                    return;
            }   
        }

        private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
            // ��� �������� ����������, � ������� �������� ��� ������ � � ��������� 
            var ErrorMessage = error switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => error.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }
}
