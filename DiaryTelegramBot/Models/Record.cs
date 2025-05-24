namespace DiaryTelegramBot.Data;

public class Record
{
    public long id { get; set; }
    public long UserId { get; set; }
    public string Text { get; set; }
    public DateTime SentTime { get; set; }
}