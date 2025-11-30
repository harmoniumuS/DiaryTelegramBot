namespace DiaryTelegramBot.Data;

public class  UserReminder
{
    public long Id { get; set; } 
    
    public DateTime ReminderTime { get; set; }
    public string ReminderMessage { get; set; }
    public bool IsRemind { get; set; }
    
    public long UserId { get; set; } 
    public User User { get; set; }
}