using DiaryTelegramBot.States;

namespace DiaryTelegramBot;


public class TempUserState
{
    public InputStage Stage { get; set; } = InputStage.None;
    public string TempContent { get; set; }                                             
    public DateTime TempDate { get; set; }
    public TimeSpan? TempTime { get; set; }
    public List<string> TempRecords { get; set; } = new();
}