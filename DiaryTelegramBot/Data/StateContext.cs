using DiaryTelegramBot.Models;

namespace DiaryTelegramBot.Data;

public class StateContext
{
    public User User { get; init; }
    public long ChatId { get; init; }
    public CancellationToken CancellationToken { get; init; }
    public string CallbackData { get; set; }
    public string? MessageText { get; init; }
    public int CallBackQueryId { get; init; }

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
