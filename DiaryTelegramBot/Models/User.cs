using DiaryTelegramBot.Data;
using DiaryTelegramBot.States;
using ReminderWorker.Data;

namespace DiaryTelegramBot.Models
{
    public class User
    {
        public long Id { get; set; }
        public UserStatus CurrentStatus { get; set; } = UserStatus.None;
        public Record TempRecord { get; set; }
        public int SelectedIndexRecord { get; set; }
        public IReadOnlyList<Record> Messages => _messages;
        public IReadOnlyList<Remind> Reminders => _reminders;
        private List<Record> _messages = new List<Record>();
        private List<Remind> _reminders = new List<Remind>();

        public void AddRecord(Record record)
        {
            if (record.UserId != Id)
                throw new Exception("UserId mismatch");
            _messages.Add(record);
        }

        public void AddRemind(Remind remind)
        {
            if (remind.UserId != Id)
                throw new Exception("UserId mismatch");
            _reminders.Add(remind);
        }
    }
}