using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.States;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DiaryTelegramBot.Handlers;

public class AddRemindHandler
{
    private readonly UserStateService _userStateService;
    private readonly UserDataService _userDataService;
    private readonly BotClientWrapper _botClientWrapper;

    public AddRemindHandler(UserStateService userStateService,BotClientWrapper botClientWrapper,UserDataService userDataService)
    {
        _userStateService = userStateService;
        _botClientWrapper = botClientWrapper;
        _userDataService = userDataService;
    }

    public async Task HandleAddRemind(ITelegramBotClient botClient, long chatId, string userId,
        CancellationToken cancellationToken)
    {
        /*
       botClient.SendMessage(chatId,
           "В процессе разработки, просим прощения за неудобства...",
           replyMarkup: new[]
       {
           InlineKeyboardButton.WithCallbackData("Вернуться в главное меню", "return_main_menu"),
       },
       cancellationToken: cancellationToken);

    }*/
        try
        {
            var userData = _userDataService.GetUserDataAsync(userId).Result;
            var allRecords = userData
                .SelectMany(kv => kv.Value.Select(record => $"{kv.Key:yyyy-MM-dd}: {record}"))
                .ToList(); 

            _userStateService.SetState(userId, new TempUserState
            {
                Stage = InputStage.AwaitingRemind,
                TempRecords = allRecords
            });

            await BotKeyboardManager.SendAddRemindersKeyboard(botClient, chatId, allRecords, cancellationToken);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Ошибка при получении данных для пользователя {userId}: {e.Message}");
            await _botClientWrapper.SendTextMessageAsync(chatId, "Произошла ошибка при обработке вашего запроса.",cancellationToken);
        }
    }

    public async Task HandleAddRemind(ITelegramBotClient botClient, long chatId, string userId, int index,
        CancellationToken
            cancellationToken)
    {
        var state = _userStateService.GetOrCreateState(userId);
    
        if (state.Stage != InputStage.AwaitingRemind || state.TempRecords == null || index < 0 || index >= state.TempRecords.Count)
        {
            await _botClientWrapper.SendTextMessageAsync(chatId, "Некорректный выбор записи.", cancellationToken);
            return;
        }

        var selectedRecord = state.TempRecords[index];
        state.TempRecords = new List<string> { selectedRecord };
        state.Stage = InputStage.AwaitingRemindOffset;

        var buttons = new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("За 5 минут", "remind_offset_5") },
            new[] { InlineKeyboardButton.WithCallbackData("За 30 минут", "remind_offset_30") },
            new[] { InlineKeyboardButton.WithCallbackData("За 1 час", "remind_offset_60") },
            new[] { InlineKeyboardButton.WithCallbackData("За 24 часа", "remind_offset_1440") }
        };
        var keyboard = new InlineKeyboardMarkup(buttons);

        await botClient.SendMessage(chatId,
            text:$"Вы выбрали: {selectedRecord}\n",
            replyMarkup:keyboard,
            cancellationToken: cancellationToken);
    }
    
}