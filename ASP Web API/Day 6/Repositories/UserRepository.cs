using ExcelAPI.Interfaces;
using ExcelAPI.Models;
using System;
using ExcelAPI.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ExcelAPI.Repositories
{
    public class UserRepository : IUserRepository
    {
        protected UserContext _context;
        public UserRepository(UserContext context)
        {
            _context = context;
        }
        
        public async Task<User> CreateUser(User item)
        {
            _context.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task DeleteUser(int key)
        {
            var item = await _context.Users.FindAsync(key);
            if (item == null)
                throw new Exception("No Such item for delete");
            _context.Remove(item);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<User>> GetAllUsers()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User?> GetUserById(int id)
        {
            var item = await _context.Users.FindAsync(id);
            return item;
        }

        public async Task<User> UpdateUser(User user)
        {
            var myItem = await _context.Users.FindAsync(user.Id);
            if (myItem == null)
                throw new Exception("No such item for update");
            _context.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }
    }
}