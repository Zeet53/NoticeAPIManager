using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ControlNode.Jwt
{
    public static class Jwt_funcs
    {
        public static Dictionary<string, object?>? GetTokenInfo(string token)
        {
            try
            {
                var secretKey = Environment.GetEnvironmentVariable("SECRET_KEY")
                        ?? throw new InvalidOperationException("SECRET_KEY environment variable is not set");

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

        public static string GenerateJwtToken(Dictionary<string, object?> payload)
        {
            var username = payload.GetValueOrDefault("username") as string
            ?? throw new ArgumentException("Dictionary must contain 'username' key with a non-null string value");

            var expiration = payload.GetValueOrDefault("expiration") as DateTime?
                ?? throw new ArgumentException("Dictionary must contain 'expiration' key with a DateTime value");

            var secretKey = Environment.GetEnvironmentVariable("SECRET_KEY")
                ?? throw new InvalidOperationException("SECRET_KEY environment variable is not set");

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
    }
}
