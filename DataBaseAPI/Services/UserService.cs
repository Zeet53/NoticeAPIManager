using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DataBaseAPI.Models;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.IdentityModel.Tokens;

namespace DataBaseAPI.Services;

public class UserService
{
    private readonly AppDataConnection _db;
    private readonly IConfiguration _configuration;

    public UserService(IConfiguration configuration)
    {
        _db = new AppDataConnection();
        _configuration = configuration;
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
        return createdUser;
    }

    public async Task<UserTableModel?> GetUser(int id, string name, string password)
    {
        return await _db.Users.FirstOrDefaultAsync(u =>
            u.id == id && u.name == name && u.password == password);
    }

    public async Task<UserTableModel?> DeleteUser(int id, string name, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.id == id && u.name == name && u.password == password);
        if (user == null) return null;

        await _db.DeleteAsync(user);
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
}
