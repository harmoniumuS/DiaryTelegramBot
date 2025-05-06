using System.Net;

namespace DiaryTelegramBot;

public class UserStateService
{
    private readonly Dictionary<string,TempUserState> _userStates = new();
    
    public TempUserState GetOrCreateState(string userId)
    {
        if (!_userStates.ContainsKey(userId))
        {
            _userStates[userId] = new TempUserState { Stage = InputStage.None };
        }
        
        return _userStates[userId];
    }
    public void SetState(string userId, TempUserState state)
    {
        _userStates[userId] = state;
    }
    
    public void ResetUserState(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }
        SetState(userId, new TempUserState { Stage = InputStage.None });
    }
    public void ClearState(string userId)
    {
        _userStates.Remove(userId);
    }
}