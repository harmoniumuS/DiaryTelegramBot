using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DiaryTelegramBot.Handlers
{
    public  class MessageHandler
    {
        
        private readonly BotClientWrapper _botClientWrapper;
        private readonly UserDataService _userDataService;

        public MessageHandler(BotClientWrapper botClientWrapper, UserDataService userDataService)
        { 
            _botClientWrapper = botClientWrapper;
            _userDataService = userDataService;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient,Update update,CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.CallbackQuery)
            { 
                var callbackQuery = update.CallbackQuery;
                var userId = callbackQuery.From.Id.ToString();
                var chatId = callbackQuery.Message.Chat.Id;

                switch (callbackQuery.Data)
                {
                    case "add_record":
                        await HandleAddRecord(botClient, chatId, userId);
                        break;

                    case "remove_record":
                        await HandleRemoveRecord(botClient, chatId, userId);
                        break;

                    case "view_records":
                        await HandleViewRecords(botClient, chatId, userId);
                        break;
                }
                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            }
        }

        private async Task HandleAddRecord(ITelegramBotClient botClient, long chatId, string userId)
        {
            await botClient.SendTextMessageAsync(chatId, "Введите запись:");
            var content = (await botClient.GetUpdatesAsync()).FirstOrDefault()?.Message.Text;

            await botClient.SendTextMessageAsync(chatId, "Введите дату в формате ГГГГ-ММ-ДД, либо автоматом поставится сегодняшняя дата:");
            var dateInput = (await botClient.GetUpdatesAsync()).FirstOrDefault()?.Message.Text;
           
            
            DateTime date = DateTime.TryParse(dateInput, out var parsedDate) ? parsedDate : DateTime.UtcNow;


            await _userDataService.AddOrUpdateUserDataAsync(userId, date, content);
            await botClient.SendTextMessageAsync(chatId, "Запись успешно добавлена!");

        }

        private async Task HandleRemoveRecord(ITelegramBotClient botClient, long chatId, string userId)
        {
            await botClient.SendTextMessageAsync(chatId, "Введите дату в формате ГГГГ-ММ-ДД:");
            var dateInput = (await botClient.GetUpdatesAsync()).FirstOrDefault()?.Message.Text;

            if (DateTime.TryParse(dateInput, out DateTime date))
            {
                var records = await _userDataService.GetUserDataAsync(userId,date);

                if (records != null && records.Any())
                {
                    if (records.Count == 1)
                    {
                        await _userDataService.RemoveUserDataAsync(userId, date);
                        await botClient.SendTextMessageAsync(chatId, "Единственная запись на эту дату была удалена.");
                    }
                    else
                    {
                        string message = "Выберите запись для удаления:\n";
                        for (int i = 0; i < records.Count; i++)
                        {
                            message += $"{i+1}. {records[i]}\n";
                        }

                        await botClient.SendTextMessageAsync(chatId, message);
                        var choiceInput = (await botClient.GetUpdatesAsync()).FirstOrDefault()?.Message.Text;

                        if (int.TryParse(choiceInput, out int choice) && choice > 0 && choice <= records.Count)
                        {
                            await _userDataService.RemoveUserDataAsync(userId, date, records[choice - 1]);
                            await botClient.SendTextMessageAsync(chatId, "Запись успешно удалена!");
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId, "Неверный выбор!");                    
                        }   
                    }
                    
                }

            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Неверный формат даты");
            }

        }

        private async Task HandleViewRecords(ITelegramBotClient botClient, long chatId, string userId)
        {
            var userData = await _userDataService.GetUserDataAsync(userId);

            if (userData.Any())
            {
                var dataString = string.Join("\n", userData.Select(r => $"{r.Key}: {r.Value}"));
                await botClient.SendTextMessageAsync(chatId, dataString);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Записи не найдены!");
            }
        }
    }
}
