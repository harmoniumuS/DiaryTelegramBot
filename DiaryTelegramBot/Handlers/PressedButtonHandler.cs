using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.States;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DiaryTelegramBot.Handlers;

public class PressedButtonHandler
{
    private readonly RemoveRemindState _removeRemindState;
    private readonly UserStateHandler _userStateHandler;
    private readonly UserContext _userContext;

    public PressedButtonHandler(RemoveRemindState removeRemindState,
        UserStateHandler userStateHandler, UserContext userContext)
    {
        
        _removeRemindState = removeRemindState;
        _userStateHandler = userStateHandler;
        _userContext = userContext;
    }
     public async Task HandlePressedButtonAsync(ITelegramBotClient botClient, CallbackQuery? callbackQuery, CancellationToken cancellationToken)
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
                                    user.CurrentStatus = UserStatus.AwaitingGetAllReminds;
                                    await _userStateHandler.HandleState(user,chatId,cancellationToken);
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
                                        user.CurrentStatus = UserStatus.AwaitingOffsetRemind;
                                        await _userStateHandler.HandleState(user, chatId, cancellationToken,data);
                                        break;
                                case {} data when data.StartsWith("deleteReminder_"):
                                    if (int.TryParse(data["deleteReminder_".Length..], out int indexRemind))
                                    {
                                        user.CurrentStatus = UserStatus.AwaitingRemoveChoiceRemind;
                                        user.SelectedIndex = indexRemind;
                                        await _userStateHandler.HandleState(user,chatId,cancellationToken);
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
                                        user.SelectedIndex = index;
                                        await _userStateHandler.HandleState(user,chatId,cancellationToken);
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