using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DiaryTelegramBot.Data
{
    public class UserDataService
    {
        private readonly AppDbContext _context;
        public UserDataService(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddUserAsync(string userId, string userData)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                user = new User { UserId = userId };
                _context.Users.Add(user);
            }

            // Сериализация Dictionary в JSON
            user.UserJsonData = JsonSerializer.Serialize(userData);
            await _context.SaveChangesAsync();
        }
        public async Task AddOrUpdateUserDataAsync(string userId, DateTime date, string content)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.UserId == userId);
            if (user != null)
            {
                // Десериализация существующих данных
                var userData = string.IsNullOrEmpty(user.UserJsonData)
                    ? new Dictionary<DateTime, string>()
                    : JsonSerializer.Deserialize<Dictionary<DateTime, string>>(user.UserJsonData);

                // Добавление или обновление записи
                userData[date] = content;

                // Сериализация обратно в JSON
                user.UserJsonData = JsonSerializer.Serialize(userData);
                await _context.SaveChangesAsync();
            }

        }

        public async Task<List<string>> GetUserDataAsync(string userId, DateTime date)
        {
               var user = await _context.Users.SingleOrDefaultAsync(u => u.UserId == userId);

            if (string.IsNullOrEmpty(user.UserJsonData))
            { 
                return new List<string>();
            }

            var userRecords = JsonSerializer.Deserialize<Dictionary<DateTime, string>>(user.UserJsonData);

            var records = userRecords.Where(d => d.Key.Date == date.Date).Select(r => r.Value).ToList();

            return records;
        }

        public async Task RemoveUserDataAsync(string userId,DateTime date)
        { 
            var user = await _context.Users.SingleOrDefaultAsync(u => u.UserId == userId);
            if (user != null)
            {
                var userData = string.IsNullOrEmpty(user.UserJsonData)
                    ? new Dictionary<DateTime, string>()
                    : JsonSerializer.Deserialize<Dictionary<DateTime, string>>(user.UserJsonData);
                if (userData.Remove(date))
                { 
                    user.UserJsonData = JsonSerializer.Serialize<Dictionary<DateTime, string>>(userData);
                    await _context.SaveChangesAsync();
                }
            }
        }
        public async Task RemoveUserDataAsync(string userId, DateTime date,string content)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.UserId == userId);
            if (user == null || string.IsNullOrEmpty(user.UserJsonData))
                return;

            var userData = JsonSerializer.Deserialize<Dictionary<DateTime, List<string>>>(user.UserJsonData);
            if (!userData.TryGetValue(date, out var entries) || entries == null)
                return;

            if (!entries.Remove(content))
                return;

            if (entries.Count == 0)
                userData.Remove(date);

            user.UserJsonData = JsonSerializer.Serialize(userData);
            await _context.SaveChangesAsync();
            
        }

        public async Task<Dictionary<DateTime, string>> GetUserDataAsync(string userId)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.UserId == userId);
            if (user != null && !string.IsNullOrEmpty(user.UserJsonData))
            {
                return JsonSerializer.Deserialize<Dictionary<DateTime, string>>(user.UserJsonData);
            }
            return new Dictionary<DateTime, string>();
        }

    }
}
