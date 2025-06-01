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
        AwaitingOffsetRemind,
        AwaitingGetAllReminds,
        AwaitingRemoveChoiceRemind,
        AwaitingAddRecord,
        AwaitingDeleteConfirmation
    }
