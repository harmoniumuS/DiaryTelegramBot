using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.States;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DiaryTelegramBot.Handlers;

public class CallBackQueryHandler
{
    private readonly BotClientWrapper _botClientWrapper;
    private readonly UserDataService _userDataService;
    private readonly UserStateService _userStateService;
    private readonly AddRecordHandler _addRecordHandler;
    private readonly RemoveRecordHandler _removeRecordHandler;
    private readonly ViewAllRecordsHandler _viewAllRecordsHandler;

    public CallBackQueryHandler(BotClientWrapper botClientWrapper, UserDataService userDataService, 
        UserStateService userStateService, AddRecordHandler addRecordHandler,RemoveRecordHandler removeRecordHandler
        , ViewAllRecordsHandler viewAllRecordsHandler)
    {
        _botClientWrapper = botClientWrapper;
        _userDataService = userDataService;
        _userStateService = userStateService;
        _addRecordHandler = addRecordHandler;
        _removeRecordHandler= removeRecordHandler;
        _viewAllRecordsHandler = viewAllRecordsHandler;
    }
     public async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
            {
            var userId = callbackQuery?.From?.Id.ToString();
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }
            var chatId = callbackQuery.Message.Chat.Id;
            try
            {
                switch (callbackQuery.Data)
                {
                    case "add_record":
                        var userState = _userStateService.GetOrCreateState(userId);
                        userState.Stage = InputStage.AwaitingContent;
                        Console.WriteLine($"User {userId} current stage: {userState.Stage}");
                        await _addRecordHandler.HandleAddRecord(botClient, chatId, userId, cancellationToken);
                        break;

                    case "remove_record":
                        _removeRecordHandler.HandleRemoveRecord(botClient, chatId, userId, cancellationToken);
                        break;

                    case "view_records":
                        await _viewAllRecordsHandler.HandleViewRecords(botClient, chatId, userId, cancellationToken);
                        break;
                    case "return_main_menu":
                        await BotKeyboardManager.SendMainKeyboardAsync(botClient, chatId, cancellationToken);
                        _userStateService.SetStateToAwaitingContent(userId);
                        break;
                    
                    case { } dataCalendar when dataCalendar.StartsWith("calendar:day:"):
                    {
                            var datePart = dataCalendar.Substring("calendar:day:".Length);
                            if (DateTime.TryParse(datePart, out var parsedDate))
                            {
                                var userStateCalendar = _userStateService.GetOrCreateState(userId);
                                if (userStateCalendar.Stage == InputStage.AwaitingDate)
                                {
                                    userStateCalendar.TempDate = parsedDate;
                                    userStateCalendar.Stage = InputStage.None;
                                    await _botClientWrapper.SendTextMessageAsync(
                                        chatId,
                                        $"Вы выбрали дату: {parsedDate:dd.MM.yyyy}. Запись успешно добавлена.",
                                        cancellationToken: cancellationToken);
                                }

                                await _userDataService.AddOrUpdateUserDataAsync(userId, parsedDate,
                                    userStateCalendar.TempContent);
                            }
                            else
                            {
                                await botClient.SendMessage(
                                    chatId,
                                    "Некорректная дата, попробуйте ещё раз.",
                                    cancellationToken: cancellationToken);
                            }
                            break;
                    }
                    
                    case {} dataCalendarButtons when dataCalendarButtons.StartsWith("calendar:prev:") || dataCalendarButtons.StartsWith("calendar:next:"):
                        var action = dataCalendarButtons.Split(':')[0] == "calendar" ? dataCalendarButtons.Split(':')[1] : string.Empty;
                        var partOfDate = dataCalendarButtons.Substring($"calendar:{action}:".Length);
                        if (DateTime.TryParse(partOfDate, out var changeMonthDate))
                        {
                            var newDate = action == "prev" 
                                ? changeMonthDate.AddMonths(-1)
                                : changeMonthDate.AddMonths(1); 
                            var calendarMarkup = BotKeyboardManager.CreateCalendarMarkUp(newDate);
                            await botClient.SendMessage(
                                chatId,
                                $"Вы перешли к {newDate:MMMM yyyy}.",
                                replyMarkup: calendarMarkup,
                                cancellationToken: cancellationToken
                            );
                        }
                        else
                        {
                            await botClient.SendMessage(
                                chatId,
                                "Некорректная дата для перехода, попробуйте ещё раз.",
                                cancellationToken: cancellationToken);
                        }
                        break;
                    
                    case { } data when data.StartsWith("delete_"):
                        if (int.TryParse(data["delete_".Length..], out int index))
                        {
                            _removeRecordHandler.HandleRemoveRecord(botClient, chatId, userId, index,
                                cancellationToken);
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