using APISinout.Models;

namespace APISinout.Services;

public interface IUserService
{
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> GetByIdAsync(int id);
    Task<User> GetByEmailAsync(string email);
    Task<User> CreateUserAsync(CreateUserRequest request, string createdBy);
    Task UpdateUserAsync(int id, UpdateUserRequest request);
    Task DeleteUserAsync(int id);

    Task UpdatePatientNameAsync(int userId, string patientName);
}