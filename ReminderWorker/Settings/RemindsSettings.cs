namespace ReminderWorker.Settings;

public class RemindsSettings
{
    public TimeSpan Delay { get; set; } = TimeSpan.FromMinutes(1);
    public int PeriodStartInSeconds { get; set; } = -30;
    public int PeriodEndInSeconds { get; set; } = 60;
}