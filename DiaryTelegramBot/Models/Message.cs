namespace DiaryTelegramBot.Data;

public class Message
{
    public long id { get; set; }
    public long UserId { get; set; }
    public string Text { get; set; }
    public DateOnly SentTime { get; set; }
}