using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using DataBaseAPI.Models;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.IdentityModel.Tokens;

namespace DataBaseAPI.Services;

public class UserService
{
    private readonly AppDataConnection _db;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _cacheHttpClient;

    public UserService(IConfiguration configuration)
    {
        _db = new AppDataConnection();
        _configuration = configuration;
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        _cacheHttpClient = new HttpClient(handler);
        _cacheHttpClient.BaseAddress = new Uri(_configuration.GetValue<string>("CacheServer:Url") ?? "http://localhost:5000");
        _cacheHttpClient.Timeout = TimeSpan.FromSeconds(0.5);
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

        await SetCacheAsync("User", createdUser);

        return createdUser;
    }

    public async Task<UserTableModel?> GetUser(int id, string name, string password)
    {
        var cached = await GetCacheAsync<UserTableModel>($"User/{id}");
        if (cached != null && cached.name == name && cached.password == password)
            return cached;

        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.id == id && u.name == name && u.password == password);

        if (user != null)
            await SetCacheAsync("User", user);

        return user;
    }

    public async Task<UserTableModel?> DeleteUser(int id, string name, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.id == id && u.name == name && u.password == password);
        if (user == null) return null;

        await _db.DeleteAsync(user);

        await DeleteCacheAsync($"User/{id}");

        return user;
    }

    public string GenerateJwtToken(Dictionary<string, object?> payload)
    {
        var username = payload.GetValueOrDefault("username") as string
            ?? throw new ArgumentException("Dictionary must contain 'username' key with a non-null string value");

        var expiration = payload.GetValueOrDefault("expiration") as DateTime?
            ?? throw new ArgumentException("Dictionary must contain 'expiration' key with a DateTime value");

        var secretKey = _configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey is not configured");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: expiration,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Dictionary<string, object?>? GetTokenInfo(string token)
    {
        try
        {
            var secretKey = _configuration["Jwt:SecretKey"]
                    ?? throw new InvalidOperationException("JWT SecretKey is not configured");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            var jwtToken = validatedToken as JwtSecurityToken;

            var username = principal.FindFirst(ClaimTypes.Name)?.Value;
            DateTime? expirationDate = jwtToken?.ValidTo;

            return new Dictionary<string, object?> { { "username", username }, { "expiration", expirationDate } };
        }
        catch
        {
            return null;
        }
    }

    private async Task SetCacheAsync<T>(string endpoint, T obj)
    {
        try
        {
            var json = JsonSerializer.Serialize(obj);
            await _cacheHttpClient.PostAsync(endpoint, new StringContent(json, Encoding.UTF8, "application/json"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cache set error ({endpoint}): {ex.Message}");
        }
    }

    private async Task<T?> GetCacheAsync<T>(string endpoint) where T : class
    {
        try
        {
            var response = await _cacheHttpClient.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cache get error ({endpoint}): {ex.Message}");
            return null;
        }
    }

    private async Task DeleteCacheAsync(string endpoint)
    {
        try
        {
            await _cacheHttpClient.DeleteAsync(endpoint);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cache delete error ({endpoint}): {ex.Message}");
        }
    }
}
