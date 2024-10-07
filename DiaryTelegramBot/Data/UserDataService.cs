using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiaryTelegramBot.Data
{
    public class UserDataService
    {
        private readonly AppDbContext _context;
        public UserDataService(AppDbContext context)
        { 
        _context=context;
        }

        public void InsertOrUpdateData(string userId, string jsonData)
        { 
            var existingUser = _context.UsersData.Find(userId);

            if (existingUser == null)
            {
                var newUser = new UserData
                {
                    Id = userId,
                    Data = jsonData
                };

                _context.UsersData.Add(newUser);
            }
            else
            {
            existingUser.Data = jsonData;
                _context.UsersData.Update(existingUser);
            }

            _context.SaveChanges();
        }
    }
}
