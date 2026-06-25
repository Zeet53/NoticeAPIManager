using DataBaseAPI.Models;

namespace DataBaseAPI.Services;

public interface IUserService
{
    Task<UserTableModel> CreateUser(string name, string password);
    Task<UserTableModel?> GetUser(int id, string name, string password);
    Task<bool> UserExists(int id, string name, string password);
    Task<UserTableModel?> DeleteUser(int id, string name, string password);
}
