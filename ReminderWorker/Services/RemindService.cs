using Microsoft.Extensions.Options;
using ReminderWorker.Data;
using ReminderWorker.Settings;
using Telegram.Bot;

namespace ReminderWorker.Services;

public class RemindsService(RemindContext context
    ,ITelegramBotClient botClient
    ,IOptions<RemindsSettings> settings
    ,IOptions<MessageSettings> messages)
{
    public async Task SendAllReminds()
    {
        var from = DateTime.Now.AddSeconds(settings.Value.PeriodStartInSeconds);
        var to = DateTime.Now.AddSeconds(settings.Value.PeriodEndInSeconds);
        var reminds = await context.ReadRemindsInTimespan(from, to);
        var tasks = Task.WhenAll(reminds.Select(SendRemind));

        try
        {
            await tasks;
        }
        catch (Exception e)
        {
            if (tasks is { IsFaulted: true, Exception: not null })
            {
                throw tasks.Exception;
            }
        }
        finally
        {
            context.SaveChangesAsync();
        }
    }

    private async Task SendRemind(Remind remind)
    {
        try
        {
            var telegramUserId = remind.UserId;
            var message = string.Format(messages.Value.RemindHeader, remind.Record);
            await botClient.SendMessage(telegramUserId, message);
        }
        catch (Exception e)
        {
            throw new Exception();
        }
    }
}
