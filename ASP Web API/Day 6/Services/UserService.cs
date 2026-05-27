using ExcelAPI.Models;
using ExcelAPI.Interfaces;

namespace ExcelAPI.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public Task<IEnumerable<User>> GetAllUsers()
        {
            return _userRepository.GetAllUsers();
        }

        public Task<User?> GetUserById(int id)
        {
            return _userRepository.GetUserById(id);
        }

        public Task<User> CreateUser(User user)
        {
            return _userRepository.CreateUser(user);
        }

        public Task<User> UpdateUser(User user)
        {
            return _userRepository.UpdateUser(user);
        }

        public Task DeleteUser(int id)
        {
            return _userRepository.DeleteUser(id);
        }
    }
}