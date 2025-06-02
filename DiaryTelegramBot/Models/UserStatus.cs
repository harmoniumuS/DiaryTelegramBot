namespace DiaryTelegramBot.States;
    public enum UserStatus
    {
        None,
        AwaitingContent,
        AwaitingDate,
        AwaitingTime,
        AwaitingGetAllRecords,
        AwaitingRemoveRecord,
        AwaitingRemind,
        AwaitingRemoveRemind,
        AwaitingRemoveSelectedRecord,
        AwaitingGetAllReminds,
        AwaitingRemoveChoiceRemind,
        AwaitingAddRecord,
        AwaitingDeleteConfirmation,
        NoStatus
    }
