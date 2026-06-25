using DataBaseAPI.Models;
using LinqToDB;
using LinqToDB.Async;

namespace DataBaseAPI.Services;

public class UserService : IUserService
{
    private readonly AppDataConnection _db;
    private readonly IConfiguration _configuration;
    private readonly IRedisCacheService _cache;

    public UserService(IConfiguration configuration, IRedisCacheService cache)
    {
        _db = new AppDataConnection();
        _configuration = configuration;
        _cache = cache;
    }

    public async Task<UserTableModel> CreateUser(string name, string password)
    {
        var user = new UserTableModel
        {
            name = name,
            password = password
        };

        var insertId = Convert.ToInt32(await _db.InsertWithIdentityAsync(user));
        var createdUser = await _db.Users.FirstOrDefaultAsync(u => u.id == insertId);
        Console.WriteLine($"name - {createdUser.name}, pass - {createdUser.password}");

        await _cache.SetAsync($"User:{createdUser.id}", createdUser);

        return createdUser;
    }

    public async Task<UserTableModel?> GetUser(int id, string name, string password)
    {
        var cached = await _cache.GetAsync<UserTableModel>($"User:{id}");
        if (cached != null && cached.name == name && cached.password == password)
            return cached;

        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.id == id && u.name == name && u.password == password);

        if (user != null)
            await _cache.SetAsync($"User:{user.id}", user);

        return user;
    }

    public async Task<bool> UserExists(int id, string name, string password)
    {
        var user = await GetUser(id, name, password);
        return user != null;
    }

    public async Task<UserTableModel?> DeleteUser(int id, string name, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.id == id && u.name == name && u.password == password);
        if (user == null) return null;

        await _db.DeleteAsync(user);

        await _cache.DeleteAsync($"User:{id}");

        return user;
    }
}
