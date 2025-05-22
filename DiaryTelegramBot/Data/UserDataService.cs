using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DiaryTelegramBot.Data
{
    public class UserDataService
    {
        //для того чтобы база не закрылась раньше вызвав dispose, открываем все через IServiceScopeFactory. Почитать про него и его плюсы и минусы
        private readonly IServiceScopeFactory _scopeFactory;

        public UserDataService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        private async Task<User> GetOrCreateUserAsync(AppDbContext context, string userId)
        {
            var parsedId = int.Parse(userId);
            var user = await context.Users.SingleOrDefaultAsync(u => u.UserId == parsedId);

            if (user == null)
            {
                Console.WriteLine($"Создаем нового пользователя: {userId}");
                user = new User
                {
                    UserId = parsedId,
                    UserJsonData = JsonSerializer.Serialize(new Dictionary<DateTime, List<string>>())
                };
                context.Users.Add(user);
                await context.SaveChangesAsync();
            }

            return user;
        }

        private async Task<Dictionary<DateTime, List<string>>> GetUserDataFromDatabaseAsync(AppDbContext context, string userId)
        {
            var user = await GetOrCreateUserAsync(context, userId);
            return DeserializeUserData(user.UserJsonData);
        }

        public async Task SaveUserDataAsync(string userId, Dictionary<DateTime, List<string>> userData)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await GetOrCreateUserAsync(context, userId);
            try
            {
                user.UserJsonData = SerializeUserData(userData);
                Console.WriteLine($"Сохраняем data для {userId}");

                await context.SaveChangesAsync();
                Console.WriteLine("Изменения успешно сохранены!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while saving data for user {userId}: {ex.Message}");
            }
        }

        public async Task AddOrUpdateUserDataAsync(string userId, DateTime date, string content)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            try
            {
                var userData = await GetUserDataFromDatabaseAsync(context, userId);
                var dateKey = date;

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

        public async Task<List<Remind>> GetUserRemindDataAync(string userId)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            try
            {
                int parsedUserId = int.Parse(userId);
                var user = await context.Users
                    .Include(u => u.Reminders)
                    .FirstOrDefaultAsync(u => u.UserId == parsedUserId);

                return user?.Reminders ?? new List<Remind>();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка при получении данных пользователя {userId}: {e.Message}");
                return new List<Remind>();
            }
        }

        public async Task<bool> DeleteUserRemindDataAsync(string userId, long reminderId)
        {
            try
            {
                Console.WriteLine($"Удаление напоминания с userId={userId}, reminderId={reminderId}");

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var parsedUserId = int.Parse(userId);
                var user = await context.Users.FirstOrDefaultAsync(u => u.UserId == parsedUserId);
                if (user == null)
                {
                    Console.WriteLine("Пользователь не найден.");
                    return false;
                }

                var reminder = await context.UserReminders
                    .FirstOrDefaultAsync(r => r.UserId == user.Id && r.Id == reminderId); 

                if (reminder == null)
                {
                    Console.WriteLine("Напоминание не найдено.");
                    return false;
                }

                context.UserReminders.Remove(reminder);
                await context.SaveChangesAsync();

                Console.WriteLine("Напоминание удалено.");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка удаления напоминания: {e.Message}");
                return false;
            }
        }

        public async Task<List<string>> GetUserDataAsync(string userId, DateTime date)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            try
            {
                var userData = await GetUserDataFromDatabaseAsync(context, userId);
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
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            try
            {
                return await GetUserDataFromDatabaseAsync(context, userId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении всех данных пользователя {userId}: {ex.Message}");
                return new Dictionary<DateTime, List<string>>();
            }
        }

        public async Task RemoveUserDataAsync(string userId, DateTime date, string? content = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            try
            {
                var userData = await GetUserDataFromDatabaseAsync(context, userId);
                var dateKey = date;

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

        public async Task SaveRemindDataAsync(string userId, Remind reminder)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await GetOrCreateUserAsync(context, userId);

            try
            {
                reminder.UserId = user.Id;
                await context.UserReminders.AddAsync(reminder);
                await context.SaveChangesAsync();
                Console.WriteLine($"Напоминание для пользователя {userId} успешно сохранено");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка при сохранении напоминания для пользователя {userId}: {e.Message}");
                throw;
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