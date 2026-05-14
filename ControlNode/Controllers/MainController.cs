using Microsoft.AspNetCore.Mvc;
using ControlNode.Jwt;

// ========== AUTH(прокси в DataBaseAPI) ==========
// POST   /api/auth/register     - регистрация нового пользователя
// POST   /api/auth/login        - логин (получить JWT)
// POST   /api/auth/validate     - проверить JWT токен

// ========== NOTIFICATIONS (публикация в RabbitMQ) ==========
// POST   /api/notifications/email   - создать задачу на email уведомление
// POST   /api/notifications/phone   - создать задачу на SMS/звонок
// POST   /api/notifications/push    - создать задачу на push уведомление

// ========== HISTORY (через DataBaseAPI) ==========
// GET    /api/notifications/{id}           - получить статус уведомления по id
// GET    /api/notifications                - список уведомлений с фильтрацией
//         ?recipient=&from=&to=&status=&page=&size=

// ========== HEALTH ==========
// GET    /health    - проверить состояние всех микросервисов

namespace ControlNode.Controllers
{
    [ApiController]
    [Route("")]
    public class MainController : ControllerBase
    {
        private readonly HttpClient _databaseHttpClient;

        public MainController(IConfiguration configuration)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            _databaseHttpClient = new HttpClient(handler);
            _databaseHttpClient.BaseAddress = new Uri(configuration.GetValue<string>("DataBaseServer:Url")!);
            _databaseHttpClient.Timeout = TimeSpan.FromSeconds(1);
        }

        public class RegData
        {
            public string name { get; set; } = string.Empty;
            public string password { get; set; } = string.Empty;
        }
        public class User
        {
            public int? id { get; set; }
            public string name { get; set; } = string.Empty;
            public string password { get; set; } = string.Empty;
        }

        [HttpPost("User")] //reg
        public async Task<IActionResult> createUser([FromBody] RegData data)
        {
            if (data == null)
                return BadRequest("Fill registration data");
            if (string.IsNullOrWhiteSpace(data.name) || string.IsNullOrWhiteSpace(data.password))
                return BadRequest("Name and password are required");

            var response = await _databaseHttpClient.PostAsJsonAsync("User", data);
            var content = await response.Content.ReadFromJsonAsync<User>();

            return StatusCode((int)response.StatusCode, content);
        }

        [HttpPost("Login")] //get JWT
        public async Task<IActionResult> loginUser([FromBody] User user)
        {
            if (user == null)
                return BadRequest("User data are required");
            if (string.IsNullOrWhiteSpace(user.name) || string.IsNullOrWhiteSpace(user.password) || user.id == null)
                return BadRequest("All fields are required");

            var token = Jwt_funcs.GenerateJwtToken(new Dictionary<string, object?>()
            {
                { "username", user.name },
                { "expiration", DateTime.UtcNow.AddHours(1) }
            });
            return Ok(new { token });
        }

        [HttpPost("checkToken")]
        public async Task<IActionResult> checkToken([FromBody] string token)
        {
            var tokenInfo = Jwt_funcs.GetTokenInfo(token);

            if (tokenInfo == null)
                return BadRequest("token not valid");
            else
                return Ok("token is valid");
        }

        [HttpGet("Task/{id}/status")]
        public async Task<IActionResult> getStatus(int id)
        {
            var response = await _databaseHttpClient.GetAsync($"Task/{id}/status");
            var content = await response.Content.ReadAsStringAsync();

            return StatusCode((int)response.StatusCode, content);
        }
    }
}
