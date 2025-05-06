using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DiaryTelegramBot.Data
{
    public class UserDataService
    {
        private readonly AppDbContext _context;

        public UserDataService(AppDbContext context)
        {
            _context = context;
        }

        private async Task<User> GetOrCreateUserAsync(string userId)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                Console.WriteLine($"Creating new user: {userId}");
                user = new User
                {
                    UserId = userId,
                    UserJsonData = JsonSerializer.Serialize(new Dictionary<DateTime, List<string>>())
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            return user;
        }

        private async Task<Dictionary<DateTime, List<string>>> GetUserDataFromDatabaseAsync(string userId)
        {
            var user = await GetOrCreateUserAsync(userId);
            return DeserializeUserData(user.UserJsonData);
        }

        public async Task SaveUserDataAsync(string userId, Dictionary<DateTime, List<string>> userData)
        {
            var user = await GetOrCreateUserAsync(userId);

            try
            {
                user.UserJsonData = SerializeUserData(userData);
                Console.WriteLine($"Saving data for user {userId}");

                await _context.SaveChangesAsync();
                Console.WriteLine("Changes saved successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while saving data for user {userId}: {ex.Message}");
            }
        }

        public async Task AddOrUpdateUserDataAsync(string userId, DateTime date, string content)
        {
            try
            {
                var userData = await GetUserDataFromDatabaseAsync(userId);
                var dateKey = date.Date;

                if (!userData.ContainsKey(dateKey))
                    userData[dateKey] = new List<string>();

                userData[dateKey].Add(content);
                await SaveUserDataAsync(userId, userData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении данных пользователя {userId}: {ex.Message}");
            }
        }

        public async Task<List<string>> GetUserDataAsync(string userId, DateTime date)
        {
            try
            {
                var userData = await GetUserDataFromDatabaseAsync(userId);
                return userData.TryGetValue(date.Date, out var records) ? records : new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении данных пользователя {userId}: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<Dictionary<DateTime, List<string>>> GetUserDataAsync(string userId)
        {
            try
            {
                return await GetUserDataFromDatabaseAsync(userId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении всех данных пользователя {userId}: {ex.Message}");
                return new Dictionary<DateTime, List<string>>();
            }
        }

        public async Task RemoveUserDataAsync(string userId, DateTime date, string? content = null)
        {
            try
            {
                var userData = await GetUserDataFromDatabaseAsync(userId);
                var dateKey = date.Date;

                if (!userData.TryGetValue(dateKey, out var entries))
                {
                    Console.WriteLine($"Данные на {dateKey.ToShortDateString()} для пользователя {userId} не найдены.");
                    return;
                }

                if (content == null)
                {
                    userData.Remove(dateKey);
                    Console.WriteLine($"Все записи на {dateKey.ToShortDateString()} для пользователя {userId} удалены.");
                }
                else if (entries.Remove(content))
                {
                    if (entries.Count == 0)
                        userData.Remove(dateKey);

                    Console.WriteLine($"Запись '{content}' на {dateKey.ToShortDateString()} для пользователя {userId} удалена.");
                }
                else
                {
                    Console.WriteLine($"Запись '{content}' не найдена на {dateKey.ToShortDateString()} для пользователя {userId}.");
                    return;
                }

                await SaveUserDataAsync(userId, userData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении данных пользователя {userId} на {date.Date}: {ex.Message}");
            }
        }
        private Dictionary<DateTime, List<string>> DeserializeUserData(string? jsonData)
        {
            return JsonSerializer.Deserialize<Dictionary<DateTime, List<string>>>(jsonData ?? string.Empty)
                   ?? new Dictionary<DateTime, List<string>>();
        }

        private string SerializeUserData(Dictionary<DateTime, List<string>> data)
        {
            return JsonSerializer.Serialize(data);
        }
    }
}
