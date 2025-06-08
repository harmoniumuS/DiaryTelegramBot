using DiaryTelegramBot.Models;
using ReminderWorker.Data;

namespace DiaryTelegramBot.Data;

public class StateContext
{
    public User User { get; init; }
    public long ChatId { get; init; }
    public CancellationToken CancellationToken { get; set; }
    public Record? TempRecord { get; set; } = new ();
    public Remind? TempRemind { get; set; } = new ();
    public string CallbackData { get; set; }                                            
    public string? MessageText { get; set; }
    public int CallBackQueryId { get; set; }

    public StateContext(User user
        , long chatId
        , CancellationToken cancellationToken
        , string? callbackData = null
        , string? messageText = null
        , int callBackQueryId = 0)
    {
        User = user;
        ChatId = chatId;
        CancellationToken = cancellationToken;
        CallbackData = callbackData;
        MessageText = messageText;
        CallBackQueryId = callBackQueryId;
    }
}
