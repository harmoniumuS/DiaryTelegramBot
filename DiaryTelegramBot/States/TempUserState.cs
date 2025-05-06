namespace DiaryTelegramBot;

public enum InputStage
{
    None,
    AwaitingContent,
    AwaitingDate,
    AwaitingRemoveDate,
    AwaitingRemoveChoice
}

public class TempUserState
{
    public InputStage Stage { get; set; } = InputStage.None;
    public string TempContent { get; set; }
    public DateTime TempDate { get; set; }
    public List<string> TempRecords { get; set; } = new();
}