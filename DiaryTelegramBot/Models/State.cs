using DiaryTelegramBot.Data;
using ReminderWorker.Data;

namespace DiaryTelegramBot.States;

public class State
{
    public UserStatus CurrentUserStatus { get; set; }
    public Message BufferMessage { get; set; }
    public Remind BufferRemind { get; set; }
    
}