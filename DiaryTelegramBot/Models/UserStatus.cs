namespace DiaryTelegramBot.States;
    public enum UserStatus
    {
        None,
        AwaitingContent,
        AwaitingDate,
        AwaitingRemoveDate,
        AwaitingRemoveChoice,
        AwaitingRemind,
        AwaitingTime, 
        AwaitingRemoveRemind
    }
