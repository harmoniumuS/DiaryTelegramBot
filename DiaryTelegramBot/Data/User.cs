using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiaryTelegramBot.Data
{
    public class User
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserJsonData { get; set; }

        public List<UserReminder> Reminders { get; set; } = new();

    }
}
