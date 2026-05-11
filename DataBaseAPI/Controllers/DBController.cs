using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DataBaseAPI.Models;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using static DataBaseAPI.Controllers.DBController;

// + изменить статус задачи (POST, вручную передаём новый статус)
// + проверить статус задачи
// + удалить задачу (перенос в архив, реализовать позже)
// + удалить пользователя


namespace DataBaseAPI.Controllers
{
    [ApiController]
    [Route("")]
    public class DBController : ControllerBase
    {
        private readonly AppDataConnection _db;
        private readonly IConfiguration _configuration;
        public DBController(IConfiguration configuration)
        {
            _db = new AppDataConnection();
            _configuration = configuration;
        }

        public class Task
        {
            public string Text { get; set; }
            public string? EmailData { get; set; }
            public string? PhoneData { get; set; }
            public int? PersonalNumber { get; set; }

        }

        public class User
        {
            public int? id { get; set; }
            public string name { get; set; }
            public string password { get; set; }
        }

        public class CreateUserRequest
        {
            public string name { get; set; }
            public string password { get; set; }
        }

        public class UpdateTaskModel
        {
            public int id { get; set; }
            public string status { get; set; }
        }

        [HttpPost("Task")]
        public async Task<IActionResult> CreateNewTask([FromBody] Task task)
        {
            try
            {
                if (task == null)
                    return BadRequest("Task data is null");

                if (string.IsNullOrWhiteSpace(task.Text))
                    return BadRequest("Text field is required");

                var taskModel = new TaskTableModel
                {
                    text = task.Text,
                    email_data = task.EmailData,
                    phone_data = task.PhoneData,
                    personal_number = task.PersonalNumber,
                    created_time = DateTime.UtcNow,
                    updated_time = DateTime.UtcNow,
                    status = "accepted" //accepted, sended, error
                };

                var insertId = Convert.ToInt32(await _db.InsertWithIdentityAsync(taskModel));

                var createdTask = await _db.MailTasks.FirstOrDefaultAsync(t => t.id == insertId);

                return CreatedAtAction(nameof(GetTask), new { id = insertId }, createdTask);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("Task/{id}")]
        public async Task<ActionResult<TaskTableModel>> GetTask(int id)
        {
            var task = await _db.MailTasks.FirstOrDefaultAsync(t => t.id == id);

            if (task == null)
                return NotFound();

            return Ok(task);
        }

        [HttpPost("User")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest userModel)
        {
            try
            {
                if (userModel == null)
                    return BadRequest("user data is null");

                if (string.IsNullOrWhiteSpace(userModel.name) || string.IsNullOrWhiteSpace(userModel.password))
                    return BadRequest("name and password is required");

                var user = new UserTableModel
                {
                    name = userModel.name,
                    password = userModel.password,
                };

                var insertId = Convert.ToInt32(await _db.InsertWithIdentityAsync(user));
                var createdUser = await _db.Users.FirstOrDefaultAsync(u => u.id == insertId);
                Console.WriteLine($"name - {createdUser.name}, pass - {createdUser.password}");

                return CreatedAtAction(nameof(GetTask), new { id = insertId }, createdUser);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("check_token")]
        public async Task<IActionResult> checkToken([FromBody] string token)
        {
            var tokenInfo = GetTokenInfo(token);

            if (tokenInfo == null)
                return BadRequest("token not valid");
            else
                return Ok("token is valid");
        }

        [HttpPost("token")]
        public async Task<IActionResult> GetToken([FromBody] User userModel)
        {
            try
            {
                if (userModel == null)
                    return BadRequest("user data is null");

                var dbUser = await _db.Users.FirstOrDefaultAsync(u =>
                    u.id == userModel.id && u.name == userModel.name && u.password == userModel.password);

                if (dbUser == null)
                    return Unauthorized("user not found or data is incorrect");

                var token = GenerateJwtToken(new Dictionary<string, object?>()
                {
                    { "username", userModel.name },
                    { "expiration", DateTime.UtcNow.AddHours(1) }
                });

                return Ok(new { token });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            var token = GenerateJwtToken(new Dictionary<string, object?>()
                    {
                        { "username", "12345" },
                        { "expiration", DateTime.Now.AddHours(-1) }
                    });

            var tokenInfo = GetTokenInfo(token);
            if (tokenInfo != null)
            {
                Console.WriteLine($"Username: {tokenInfo["username"]}");
                Console.WriteLine($"Expiration: {tokenInfo["expiration"]}");
            }
            else
            {
                Console.WriteLine("Токен невалиден");
            }

            return Ok(token);
        }

        [HttpPut("Task")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateTaskModel model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.status))
                    return BadRequest("status is required");

                var validStatuses = new[] { "accepted", "sended", "error" };
                if (!validStatuses.Contains(model.status.ToLower()))
                    return BadRequest($"invalid status. allowed values: {string.Join(", ", validStatuses)}");

                var task = await _db.MailTasks.FirstOrDefaultAsync(t => t.id == model.id);
                if (task == null)
                    return NotFound("task not found");

                task.status = model.status.ToLower();
                task.updated_time = DateTime.UtcNow;
                await _db.UpdateAsync(task);

                return Ok(task);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("Task/{id}/status")]
        public async Task<IActionResult> GetTaskStatus(int id)
        {
            var task = await _db.MailTasks.FirstOrDefaultAsync(t => t.id == id);
            if (task == null)
                return NotFound("Task not found");

            return Ok(task.status);
        }

        [HttpDelete("User")]
        public async Task<IActionResult> DeleteUser([FromBody] User userModel)
        {
            try
            {
                if (userModel == null)
                    return BadRequest("user data is null");

                var user = await _db.Users.FirstOrDefaultAsync(u =>
                    u.id == userModel.id && u.name == userModel.name && u.password == userModel.password);

                if (user == null)
                    return NotFound("user not found");

                await _db.DeleteAsync(user);
                return Ok("user deleted");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private string GenerateJwtToken(Dictionary<string, object?> payload)
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

        private Dictionary<string, object?>? GetTokenInfo(string token)
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
}