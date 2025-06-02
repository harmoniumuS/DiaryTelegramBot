using DiaryTelegramBot.States;

namespace DiaryTelegramBot.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class TelegramCallbackCommandAttribute:Attribute
{
    public string Command { get; }
    public UserStatus InitialStatus { get; }

    public TelegramCallbackCommandAttribute(string command, UserStatus initialStatus = UserStatus.NoStatus)
    {
        Command = command;
        InitialStatus = initialStatus;
    }
}