using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.States;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;

namespace DiaryTelegramBot.Handlers;

public class RemoveRemindHandler
{
    private readonly BotClientWrapper _botClientWrapper;
    private readonly UserDataService _userDataService;
    private readonly UserStateService _userStateService;
    public RemoveRemindHandler(BotClientWrapper botClientWrapper, UserDataService userDataService, 
        UserStateService userStateService)
    {
        _botClientWrapper = botClientWrapper;
        _userDataService = userDataService;
        _userStateService = userStateService;
    }
    public async Task HandleRemoveRemind(ITelegramBotClient botClient, long chatId, string userId, CancellationToken cancellationToken)
        {
            var userDataRemind = await _userDataService.GetUserRemindDataAync(userId);
            
            if (userDataRemind == null || !userDataRemind.Any())
            {
                await _botClientWrapper.SendTextMessageAsync(chatId, "У вас нет записей для удаления напоминаний.",cancellationToken);
                return;
            }

            var formattedReminders = userDataRemind
                .Select(r => $"{r.Time:yyyy-MM-dd HH:mm} | {r.Message}")
                .ToList();
            var reminderIds = userDataRemind.Select(r => r.Id).ToList();
            _userStateService.SetState(userId, new TempUserState
            {
                Stage = UserStatus.AwaitingRemoveRemind,
                TempReminders = formattedReminders,
                TempReminderIds = reminderIds
            });

            await BotKeyboardManager.SendRemoveKeyboardAsync(botClient, chatId, formattedReminders, cancellationToken,iSReminderKeyboard:true);
        }
    public async Task HandleRemoveRemind(ITelegramBotClient botClient, long chatId, string userId, int indexRemind,CancellationToken cancellationToken)
    {
        var state = _userStateService.GetOrCreateState(userId);
        if (state?.TempReminders == null || indexRemind < 0 || indexRemind >= state.TempReminders.Count)
        {
            await _botClientWrapper.SendTextMessageAsync(chatId, "Ошибка с индексом напоминания.", cancellationToken);
            return;
        }
        var userReminds = await _userDataService.GetUserRemindDataAync(userId);
        if (userReminds == null || !userReminds.Any())
        {
            await _botClientWrapper.SendTextMessageAsync(chatId, "Напоминания не найдены.", cancellationToken);
            return;
        }

        var remindToRemove = userReminds[indexRemind];
        var deleteSuccess = await _userDataService.DeleteUserRemindDataAsync(userId, remindToRemove.Id);
        if (!deleteSuccess)
        {
            await botClient.SendMessage(chatId,
                "Не удалось удалить напоминание.",
                cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.SendMessage(chatId,
                "Напоминание было удалено.",
                cancellationToken:cancellationToken);
            
        }
        var updatedUserRemind = await _userDataService.GetUserRemindDataAync(userId);
        if (!updatedUserRemind.Any())
        {
            _userStateService.ClearState(userId);
            await _botClientWrapper.SendTextMessageAsync(chatId, "Больше нет напоминаний для удаления.", cancellationToken);
            return;
        }

        var formattedReminders = updatedUserRemind
            .Select(r => $"{r.Time:yyyy-MM-dd HH:mm} | {r.Message}")
            .ToList();

        var reminderIds = updatedUserRemind.Select(r => r.Id).ToList();

        _userStateService.SetState(userId, new TempUserState
        {
            Stage = UserStatus.AwaitingRemoveRemind,
            TempReminders = formattedReminders,
            TempReminderIds = reminderIds
        });

        await BotKeyboardManager.SendRemoveKeyboardAsync(botClient, chatId, formattedReminders, cancellationToken, iSReminderKeyboard: true);
    }
    
    
}