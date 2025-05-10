namespace DiaryTelegramBot.Data;

public class UserReminder
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public DateTime ReminderTime { get; set; }
    public string ReminderMessage { get; set; }
    public bool IsRemind { get; set; }
}