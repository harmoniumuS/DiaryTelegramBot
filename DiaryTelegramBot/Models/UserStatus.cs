namespace DiaryTelegramBot.States;
    public enum UserStatus
    {
        None,
        AwaitingContent,
        AwaitingDate,
        AwaitingTime,
        AwaitingChooseRemoveRecord,
        AwaitingGetAllRecords,
        AwaitingRemoveRecord,
        AwaitingRemind,
        AwaitingRemoveRemind,
        AwaitingRemoveChoice,
        AwaitingOffsetRemind,
        AwaitingGetAllReminds,
        AwaitingRemoveChoiceRemind
    }
