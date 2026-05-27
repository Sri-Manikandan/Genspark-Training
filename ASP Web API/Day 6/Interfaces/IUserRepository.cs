using ExcelAPI.Models;
using System;
using System.Collections.Generic;

namespace ExcelAPI.Interfaces
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllUsers();
        Task<User?> GetUserById(int id);
        Task<User> CreateUser(User user);
        Task<User> UpdateUser(User user);
        Task DeleteUser(int id);
    }
}