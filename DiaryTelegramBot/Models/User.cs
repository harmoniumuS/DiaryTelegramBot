using DiaryTelegramBot.Data;
using DiaryTelegramBot.States;
using ReminderWorker.Data;

namespace DiaryTelegramBot.Models
{
    public class User
    {
        public long Id { get; set; }
        public UserStatus CurrentStatus { get; set; }

        public IReadOnlyList<Message> Messages => _messages;
        public IReadOnlyList<Remind> Reminders => _reminders;

        private List<Message> _messages = new List<Message>();
        private List<Remind> _reminders = new List<Remind>();

        public void AddMessage(Message message)
        {
            if (message.UserId != Id)
                throw new Exception("UserId mismatch");
            _messages.Add(message);
        }

        public void AddRemind(Remind remind)
        {
            if (remind.UserId != Id)
                throw new Exception("UserId mismatch");
            _reminders.Add(remind);
        }
    }
}