using ExcelAPI.Models;

namespace ExcelAPI.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllUsers();
        Task<User?> GetUserById(int id);
        Task<User> CreateUser(User user);
        Task<User> UpdateUser(User user);
        Task DeleteUser(int id);
    }
}