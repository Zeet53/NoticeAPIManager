using ControlNode.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RabbitMQ.Client;
using System.Security.Claims;
using System.Text;
using System.Text.Json;


namespace ControlNode.Controllers
{
    [ApiController]
    [Route("")]
    public class MainController : ControllerBase
    {
        private readonly HttpClient _databaseHttpClient;
        private readonly HttpClient _emailHttpClient;
        private readonly HttpClient _pushHttpClient;
        private readonly HttpClient _phoneHttpClient;
        private readonly HttpClient _cacheHttpClient;
        private readonly IChannel _channel;

        public MainController(IConfiguration configuration, IChannel channel)
        {
            _channel = channel;
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            _databaseHttpClient = new HttpClient(handler);
            _databaseHttpClient.BaseAddress = new Uri(configuration.GetValue<string>("DataBaseServer:Url")!);
            _databaseHttpClient.Timeout = TimeSpan.FromSeconds(2);

            _emailHttpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5203"), Timeout = TimeSpan.FromSeconds(2) };
            _pushHttpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5204"), Timeout = TimeSpan.FromSeconds(2) };
            _phoneHttpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5205"), Timeout = TimeSpan.FromSeconds(2) };
            _cacheHttpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5202"), Timeout = TimeSpan.FromSeconds(2) };
        }

        public class RegData
        {
            public string name { get; set; } = string.Empty;
            public string password { get; set; } = string.Empty;
        }
        public class UserData
        {
            public int? id { get; set; }
            public string name { get; set; } = string.Empty;
            public string password { get; set; } = string.Empty;
        }
        public class TaskModel
        {
            public int UserId { get; set; }
            public string Text { get; set; } = string.Empty;
            public string? EmailData { get; set; }
            public string? PhoneData { get; set; }
            public int? PersonalNumber { get; set; }

        }
        public class QueuedTask
        {
            public string Text { get; set; } = string.Empty;
            public string sendData { get; set; } = string.Empty;
            public int id { get; set; }
        }
        public class NotificationRequest
        {
            public string text { get; set; } = string.Empty;
            public string sendData { get; set; } = string.Empty;
        }

        [HttpPost("User")] //reg
        public async Task<IActionResult> createUser([FromBody] RegData data)
        {
            if (data == null)
                return BadRequest("Fill registration data");
            if (string.IsNullOrWhiteSpace(data.name) || string.IsNullOrWhiteSpace(data.password))
                return BadRequest("Name and password are required");

            var response = await _databaseHttpClient.PostAsJsonAsync("User", data);
            var content = await response.Content.ReadFromJsonAsync<UserData>();

            return StatusCode((int)response.StatusCode, content);
        }

        [HttpPost("Login")] //get JWT
        public async Task<IActionResult> loginUser([FromBody] UserData user)
        {
            if (user == null)
                return BadRequest("User data are required");
            if (string.IsNullOrWhiteSpace(user.name) || string.IsNullOrWhiteSpace(user.password) || user.id == null)
                return BadRequest("All fields are required");

            try
            {
                var checkResponse = await _databaseHttpClient.PostAsJsonAsync("User/exists", user);
                if (!checkResponse.IsSuccessStatusCode)
                    return StatusCode(503, "Database service unavailable");

                var doc = await JsonDocument.ParseAsync(await checkResponse.Content.ReadAsStreamAsync());
                var exists = doc.RootElement.GetProperty("exists").GetBoolean();

                if (!exists)
                    return Unauthorized("Invalid credentials");
            }
            catch (Exception ex)
            {
                return StatusCode(503, $"Database service unavailable: {ex.Message}");
            }

            var token = Jwt_funcs.GenerateJwtToken(new Dictionary<string, object?>()
            {
                { "username", user.name },
                { "user_id", user.id },
                { "expiration", DateTime.UtcNow.AddHours(1) }
            });
            return Ok(new { token });
        }

        [HttpPost("Check_token")]
        public async Task<IActionResult> checkToken([FromBody] string token)
        {
            var tokenInfo = Jwt_funcs.GetTokenInfo(token);

            if (tokenInfo == null)
                return BadRequest("token not valid");
            else
                return Ok("token is valid");
        }

        [Authorize]
        [EnableRateLimiting("UserLimit")]
        [HttpGet("Task/{id}/status")]
        public async Task<IActionResult> getStatus(int id)
        {
            try
            {
                var response = await _databaseHttpClient.GetAsync($"Task/{id}/status");
                var content = await response.Content.ReadAsStringAsync();

                return StatusCode((int)response.StatusCode, content);
            }
            catch (Exception ex)
            {
                return StatusCode(503, $"Database service unavailable: {ex.Message}");
            }
        }

        [Authorize]
        [EnableRateLimiting("UserLimit")]
        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User not found in token");

            try
            {
                var response = await _databaseHttpClient.GetAsync($"notifications/{userIdClaim}");
                var content = await response.Content.ReadAsStringAsync();

                return StatusCode((int)response.StatusCode, content);
            }
            catch (Exception ex)
            {
                return StatusCode(503, $"Database service unavailable: {ex.Message}");
            }
        }

        [Authorize]
        [EnableRateLimiting("UserLimit")]
        [HttpPost("notifications/email")]
        public async Task<IActionResult> SendEmail([FromBody] NotificationRequest data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.sendData))
                return BadRequest("Fill required fields");
            if (!data.sendData.Contains('@') || !data.sendData.Contains('.'))
                return BadRequest("Invalid email format");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User not found in token");

            var task = new TaskModel
            {
                UserId = int.Parse(userIdClaim),
                Text = data.text,
                EmailData = data.sendData
            };

            var response = await _databaseHttpClient.PostAsJsonAsync("Task", task);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

            var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var taskId = doc.RootElement.GetProperty("id").GetInt32();

            var body = JsonSerializer.SerializeToUtf8Bytes(new QueuedTask
            {
                id = taskId,
                Text = data.text,
                sendData = data.sendData
            });

            await _channel.BasicPublishAsync(exchange: "", routingKey: "email_notifications", body: body);
            return Ok("Succeful sending");
        }

        [Authorize]
        [EnableRateLimiting("UserLimit")]
        [HttpPost("notifications/phone")]
        public async Task<IActionResult> SendPhone([FromBody] NotificationRequest data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.sendData))
                return BadRequest("Fill required fields");
            if (data.sendData.Length != 11 || !data.sendData.StartsWith("8"))
                return BadRequest("Phone must start with 8 and be 11 characters");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User not found in token");

            var task = new TaskModel
            {
                UserId = int.Parse(userIdClaim),
                Text = data.text,
                PhoneData = data.sendData
            };

            var response = await _databaseHttpClient.PostAsJsonAsync("Task", task);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

            var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var taskId = doc.RootElement.GetProperty("id").GetInt32();

            var body = JsonSerializer.SerializeToUtf8Bytes(new QueuedTask
            {
                id = taskId,
                Text = data.text,
                sendData = data.sendData
            });

            await _channel.BasicPublishAsync(exchange: "", routingKey: "phone_notifications", body: body);
            return Ok("Succeful sending");
        }

        [Authorize]
        [EnableRateLimiting("UserLimit")]
        [HttpPost("notifications/push")]
        public async Task<IActionResult> SendPush([FromBody] NotificationRequest data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.sendData))
                return BadRequest("Fill required fields");
            if (!int.TryParse(data.sendData, out _))
                return BadRequest("Push sendData must be a valid integer");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User not found in token");

            var task = new TaskModel
            {
                UserId = int.Parse(userIdClaim),
                Text = data.text,
                PersonalNumber = Convert.ToInt32(data.sendData)
            };

            var response = await _databaseHttpClient.PostAsJsonAsync("Task", task);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

            var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var taskId = doc.RootElement.GetProperty("id").GetInt32();

            var body = JsonSerializer.SerializeToUtf8Bytes(new QueuedTask
            {
                id = taskId,
                Text = data.text,
                sendData = data.sendData
            });

            await _channel.BasicPublishAsync(exchange: "", routingKey: "push_notifications", body: body);
            return Ok("Succeful sending");
        }

        [HttpGet("health")]
        public async Task<IActionResult> HealthCheck()
        {
            var result = new Dictionary<string, string>();

            result["control_node"] = "ok";

            result["database"] = await PingService(_databaseHttpClient, "DataBaseAPI");
            result["email"] = await PingService(_emailHttpClient, "EmailService");
            result["push"] = await PingService(_pushHttpClient, "PushNotice");
            result["phone"] = await PingService(_phoneHttpClient, "PhoneNotice");
            result["cache"] = await PingService(_cacheHttpClient, "CacheService");

            try
            {
                result["rabbitmq"] = _channel.IsOpen ? "ok" : "error";
            }
            catch
            {
                result["rabbitmq"] = "error";
            }

            var allOk = result.Values.All(v => v == "ok");
            return StatusCode(allOk ? 200 : 503, result);
        }

        private static async Task<string> PingService(HttpClient client, string? name = null)
        {
            try
            {
                await client.GetAsync("");
                return "ok";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[health] {name ?? "service"} недоступен: {ex.Message}");
                return "error";
            }
        }

    }
}
