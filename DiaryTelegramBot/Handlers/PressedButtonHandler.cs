using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.States;
using DiaryTelegramBot.Wrappers;
using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DiaryTelegramBot.Handlers;

public class PressedButtonHandler
{
    private readonly RemoveRemindState _removeRemindState;
    private readonly UserStateHandler _userStateHandler;
    private readonly UserContext _userContext;
    private readonly IMemoryCache _memoryCache;

    public PressedButtonHandler(RemoveRemindState removeRemindState,
        UserStateHandler userStateHandler, UserContext userContext, IMemoryCache memoryCache)
    {
        _removeRemindState = removeRemindState;
        _userStateHandler = userStateHandler;
        _userContext = userContext;
        _memoryCache = memoryCache;
    }

    public async Task HandlePressedButtonAsync(ITelegramBotClient botClient, CallbackQuery? callbackQuery, CancellationToken cancellationToken)
    {
        if (callbackQuery == null || callbackQuery.Message == null)
            return;

        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message.Chat.Id;

        var user = await _memoryCache.GetOrCreateAsync($"user_{userId}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return await _userContext.GetUserAsync(userId);
        });

        try
        {
            var data = callbackQuery.Data ?? "";

            switch (data)
            {
                case "add_record":
                    await _userStateHandler.SetUserStatusAsync(user, UserStatus.AwaitingContent);
                    await _userStateHandler.HandleState(user, chatId, cancellationToken);
                    break;

                case "remove_record":
                    await _userStateHandler.SetUserStatusAsync(user, UserStatus.AwaitingRemoveRecord);
                    await _userStateHandler.HandleState(user, chatId, cancellationToken);
                    break;

                case "view_records":
                    await _userStateHandler.SetUserStatusAsync(user, UserStatus.AwaitingGetAllRecords);
                    await _userStateHandler.HandleState(user, chatId, cancellationToken);
                    break;

                case "add_reminder":
                    await _userStateHandler.HandleState(user, chatId, cancellationToken);
                    break;

                case "remove_reminder":
                    await _userStateHandler.HandleState(user, chatId, cancellationToken);
                    break;

                case "view_reminders":
                    await _userStateHandler.HandleState(user, chatId, cancellationToken);
                    break;

                case "return_main_menu":
                    await BotKeyboardManager.SendMainKeyboardAsync(botClient, chatId, cancellationToken);
                    break;

                case { } s when s.StartsWith("date:"):
                    await _userStateHandler.HandleState(user, chatId, cancellationToken, s);
                    break;

                case { } s when s.StartsWith("calendar:prev:") || s.StartsWith("calendar:next:"):
                    await _userStateHandler.HandleState(user, chatId, cancellationToken, s);
                    break;

                case { } s when s.StartsWith("add_remind_"):
                    await _userStateHandler.HandleState(user, chatId, cancellationToken, s);
                    break;

                case { } s when s.StartsWith("deleteReminder_"):
                    if (int.TryParse(s["deleteReminder_".Length..], out int indexRemind))
                    {
                        user.TempRecord.SelectedIndex = indexRemind;
                        await _userStateHandler.HandleState(user, chatId, cancellationToken);
                    }
                    else
                    {
                        await SafeAnswerCallback(botClient, callbackQuery.Id, "Невозможно удалить запись. Некорректный индекс.", cancellationToken);
                    }
                    break;

                case { } s when s.StartsWith("delete_"):
                    if (int.TryParse(s["delete_".Length..], out int recordNumber))
                    {
                        user.TempRecord.SelectedIndex = recordNumber;
                        await _userStateHandler.SetUserStatusAsync(user, UserStatus.AwaitingDeleteConfirmation);

                        var keyboard = BotKeyboardManager.GetDeleteConfirmationKeyboard(recordNumber);
                        await botClient.SendMessage(chatId,
                            $"Вы действительно хотите удалить запись №{recordNumber + 1}?",
                            replyMarkup: keyboard,
                            cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await SafeAnswerCallback(botClient, callbackQuery.Id, "Некорректный индекс для удаления", cancellationToken);
                    }
                    break;

                case { } s when s.StartsWith("confirm_delete_"):
                    if (int.TryParse(s["confirm_delete_".Length..], out int confirmDeleteNumber))
                    {
                        user.TempRecord.SelectedIndex = confirmDeleteNumber;
                        await _userStateHandler.SetUserStatusAsync(user, UserStatus.AwaitingRemoveSelectedRecord);
                        await _userStateHandler.HandleState(user, chatId, cancellationToken);
                    }
                    else
                    {
                        await SafeAnswerCallback(botClient, callbackQuery.Id, "Некорректный индекс подтверждения", cancellationToken);
                    }
                    break;

                case "cancel_delete":
                    await _userStateHandler.SetUserStatusAsync(user, UserStatus.AwaitingRemoveRecord);
                    await _userStateHandler.HandleState(user, chatId, cancellationToken);
                    break;

                case { } s when s.StartsWith("time_hour_"):
                    if (int.TryParse(s["time_hour_".Length..], out int hour) && hour >= 0 && hour <= 23)
                    {
                        var minuteKeyboard = BotKeyboardManager.SendMinutesMarkUp(hour);
                        await botClient.SendMessage(chatId, "Вы выбрали час. Теперь выберите минуты:", replyMarkup: minuteKeyboard, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await botClient.SendMessage(chatId, "Некорректный выбор часа. Попробуйте ещё раз.", cancellationToken: cancellationToken);
                    }
                    break;

                case { } s when s.StartsWith("time_minute_"):
                    var timePart = s["time_minute_".Length..];
                    if (TimeSpan.TryParse(timePart, out TimeSpan selectedTime))
                    {
                        if (user.TempRecord?.SentTime != null)
                        {
                            var date = user.TempRecord.SentTime.Date;
                            user.TempRecord.SentTime = date.Add(selectedTime);
                            user.CurrentStatus = UserStatus.AwaitingAddRecord;

                            await _userStateHandler.HandleState(user, chatId, cancellationToken, s);
                        }
                        else
                        {
                            await botClient.SendMessage(chatId, "Сначала выберите дату. Начните заново.", cancellationToken: cancellationToken);
                        }
                    }
                    else
                    {
                        await botClient.SendMessage(chatId, "Некорректное время. Попробуйте снова.", cancellationToken: cancellationToken);
                    }
                    break;

                default:
                    await SafeAnswerCallback(botClient, callbackQuery.Id, "Неизвестная команда.", cancellationToken);
                    break;
            }

            await SafeAnswerCallback(botClient, callbackQuery.Id, "Запрос обработан", cancellationToken, showAlert: false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обработке callback-запроса: {ex.Message}");
            await SafeAnswerCallback(botClient, callbackQuery.Id, "Произошла ошибка при обработке запроса.", cancellationToken, showAlert: true);
        }
    }

    private async Task SafeAnswerCallback(ITelegramBotClient botClient, string callbackQueryId, string text, CancellationToken cancellationToken, bool showAlert = true)
    {
        try
        {
            await botClient.AnswerCallbackQuery(
                callbackQueryId,
                text: text,
                showAlert: showAlert,
                cancellationToken: cancellationToken);
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("query is too old"))
        {
            Console.WriteLine("CallbackQuery is too old to answer: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unexpected error in AnswerCallbackQuery: " + ex.Message);
        }
    }
}
