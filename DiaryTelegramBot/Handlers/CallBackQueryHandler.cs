using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.States;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DiaryTelegramBot.Handlers;

public class CallBackQueryHandler
{
    private readonly ViewAllRemindersHandler _viewAllRemindersHandler;
    private readonly RemoveRemindHandler _removeRemindHandler;
    private readonly RemoveRecordState _removeRecordState;
    private readonly ViewAllRecordsState _viewAllRecordsState;
    private readonly UserStateHandler _userStateHandler;
    private readonly UserContext _userContext;

    public CallBackQueryHandler(BotClientWrapper botClientWrapper, RemoveRecordState removeRecordState
        , ViewAllRecordsState viewAllRecordsState, RemoveRemindHandler removeRemindHandler, ViewAllRemindersHandler viewAllRemindersHandler,
        UserStateHandler userStateHandler, UserContext userContext)
    {
        _removeRecordState= removeRecordState;
        _viewAllRecordsState = viewAllRecordsState;
        _removeRemindHandler = removeRemindHandler;
        _viewAllRemindersHandler = viewAllRemindersHandler;
        _userStateHandler = userStateHandler;
        _userContext = userContext;
    }
     public async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery? callbackQuery, CancellationToken cancellationToken)
            {
                if (callbackQuery != null)
                {
                    var userId = callbackQuery.From.Id;
                    if (callbackQuery.Message != null)
                    {
                        var chatId = callbackQuery.Message.Chat.Id;
                        var user = await _userContext.GetUserAsync(userId);
                        try
                        {
                            switch (callbackQuery.Data)
                            {
                                case "add_record":
                                    user.CurrentStatus = UserStatus.AwaitingContent;
                                    await _userStateHandler.HandleState(user,chatId,cancellationToken);
                                    break;
                                case "remove_record":
                                    user.CurrentStatus = UserStatus.AwaitingRemoveRecord;
                                    await _userStateHandler.HandleState(user,chatId,cancellationToken);
                                    break;
                                case "view_records":
                                    user.CurrentStatus = UserStatus.AwaitingGetAllRecords;
                                    await _userStateHandler.HandleState(user,chatId,cancellationToken);
                                    break;
                                case "return_main_menu":
                                    await BotKeyboardManager.SendMainKeyboardAsync(botClient, chatId, cancellationToken);
                                    break;
                                case "add_reminder":
                                    user.CurrentStatus = UserStatus.AwaitingRemind;
                                    await _userStateHandler.HandleState(user,chatId,cancellationToken);
                                    break;
                                case "remove_reminder":
                                    user.CurrentStatus = UserStatus.AwaitingRemoveRemind;
                                    await _userStateHandler.HandleState(user,chatId,cancellationToken);
                                    break;
                                case "view_reminders":
                                    _viewAllRemindersHandler.HandleViewReminders(botClient, chatId, userId, cancellationToken);
                                    break;
                    
                                case { } dataCalendar when dataCalendar.StartsWith("date:"):
                                {
                                    user.CurrentStatus = UserStatus.AwaitingDate;
                                    await _userStateHandler.HandleState(user, chatId, cancellationToken,dataCalendar);
                                    break;
                                }
                                case {} dataCalendarButtons when dataCalendarButtons.StartsWith("calendar:prev:") || dataCalendarButtons.StartsWith("calendar:next:"):
                                    await _userStateHandler.HandleState(user, chatId, cancellationToken,dataCalendarButtons);
                                    break;
                                case {} data when data.StartsWith("add_remind_"):
                                    if (int.TryParse(data["add_remind_".Length..], out int result))
                                    {
                                        await _addRemindState.HandleAddRemind(botClient, chatId, userId, result,
                                            cancellationToken);
                                    }
                                    break;
                                case {} data when data.StartsWith("deleteReminder_"):
                                    if (int.TryParse(data["deleteReminder_".Length..], out int indexRemind))
                                    {
                                        _removeRemindHandler.HandleRemoveRemind(botClient, chatId, userId, indexRemind, cancellationToken);
                                    }
                                    else
                                    {
                                        try
                                        {
                                            await botClient.AnswerCallbackQuery(
                                                callbackQuery.Id,           
                                                text: "Невозможно удалить запись. Некорректный индекс.",   
                                                showAlert: true,           
                                                cancellationToken: cancellationToken
                                            );
                                        }
                                        catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("query is too old"))
                                        {
                                            Console.WriteLine("CallbackQuery is too old to answer: " + ex.Message);
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine("Unexpected error in AnswerCallbackQuery: " + ex.Message);
                                        }
                                        return;
                                    }
                                    break;
                                case { } data when data.StartsWith("delete_"):
                                    if (int.TryParse(data["delete_".Length..], out int index))
                                    {
                                        user.SelectedIndexRecord = index;
                                        _removeRecordState.Handle(_userStateHandler, user,userId,cancellationToken);
                                    }
                                    else
                                    {
                                        try
                                        {
                                            await botClient.AnswerCallbackQuery(
                                                callbackQuery.Id,           
                                                text: "Невозможно удалить запись. Некорректный индекс.",   
                                                showAlert: true,           
                                                cancellationToken: cancellationToken
                                            );
                                        }
                                        catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("query is too old"))
                                        {
                                            Console.WriteLine("CallbackQuery is too old to answer: " + ex.Message);
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine("Unexpected error in AnswerCallbackQuery: " + ex.Message);
                                        }
                                        return;
                                    }
                                    break;
                                case "remind_offset_5":
                                    await _addRemindState.HandleRemindOffset(botClient, chatId, userId,-5,cancellationToken);
                                    break;
                                case "remind_offset_30":
                                    await _addRemindState.HandleRemindOffset(botClient, chatId, userId,-30,cancellationToken);
                                    break;
                                case "remind_offset_60":
                                    await _addRemindState.HandleRemindOffset(botClient, chatId, userId,-60,cancellationToken);
                                    break;
                                case "remind_offset_1440":
                                    await _addRemindState.HandleRemindOffset(botClient, chatId, userId,-1440,cancellationToken);
                                    break;

                                default:
                                    try
                                    {
                                        await botClient.AnswerCallbackQuery(
                                            callbackQuery.Id,           
                                            text: "Неизвестная команда.",   
                                            showAlert: true,           
                                            cancellationToken: cancellationToken
                                        );
                                    }
                                    catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("query is too old"))
                                    {
                                        Console.WriteLine("CallbackQuery is too old to answer: " + ex.Message);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("Unexpected error in AnswerCallbackQuery: " + ex.Message);
                                    }
                                    return;
                            }

                            try
                            {
                                await botClient.AnswerCallbackQuery(
                                    callbackQuery.Id,           
                                    text: "Запрос обработан",   
                                    showAlert: false,           
                                    cancellationToken: cancellationToken 
                                );
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
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка при обработке callback-запроса: {ex.Message}");
                            await botClient.AnswerCallbackQuery(
                                callbackQuery.Id,           
                                text: "Произошла ошибка при обработке запроса.",   
                                showAlert: true,           
                                cancellationToken: cancellationToken
                            );
                        }
                    }
                }
            }
}