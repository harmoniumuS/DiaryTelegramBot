using System.Collections.Concurrent;
using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Wrappers;
using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot;

namespace DiaryTelegramBot.States;

public class UserStateService
{
    private readonly IMemoryCache _memoryCache;

    public UserStateService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public TempUserState GetOrCreateState(string userId)
    {
        if (!_memoryCache.TryGetValue(userId, out TempUserState userState))
        {
            userState = new TempUserState { Stage = InputStage.None };
            _memoryCache.Set(userId, userState);
        }

        return userState;
    }

    public void SetState(string userId, TempUserState state)
    {
        _memoryCache.Set(userId, state);
    }

    public void ResetUserState(string userId)
    {
        _memoryCache.Remove(userId);
    }

    public void ClearState(string userId)
    {
        _memoryCache.Set(userId, new TempUserState { Stage = InputStage.None });
    }

    public void SetStateToAwaitingContent(string userId)
    {
        _memoryCache.Set(userId, new TempUserState { Stage = InputStage.AwaitingContent });
    }

    public void SaveState(string userId, TempUserState state)
    {
        _memoryCache.Set(userId, state);
    }
}
    