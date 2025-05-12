namespace DiaryTelegramBot.States;
    public enum InputStage
    {
        None,
        AwaitingContent,
        AwaitingDate,
        AwaitingRemoveDate,
        AwaitingRemoveChoice,
        AwaitingRemind,
        AwaitingTime, 
        AwaitingRemindOffset,
        AwaitingRemoveRemind
    }
