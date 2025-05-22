using System;
using System.Collections.Generic;
using DiaryTelegramBot.Data.Remind; 

namespace DiaryTelegramBot.Data
{
    public class User
    {
        public long Id { get; set; }
        public long UserId { get; set; }

        public IReadOnlyList<Message> Messages => _messages;
        public IReadOnlyList<Remind> Reminders => _reminders;

        private List<Message> _messages = new List<Message>();
        private List<Remind> _reminders = new List<Remind>();

        public void AddMessage(Message message)
        {
            if (message.UserId != UserId)
                throw new Exception("UserId mismatch");
            _messages.Add(message);
        }

        public void AddRemind(Remind remind)
        {
            if (remind.UserId != UserId)
                throw new Exception("UserId mismatch");
            _reminders.Add(remind);
        }
    }
}