using DataBaseAPI.Models;
using DataBaseAPI.Services;
using Microsoft.AspNetCore.Mvc;

//архивировать задачу
//настроить авто архивацию

namespace DataBaseAPI.Controllers
{
    [ApiController]
    [Route("")]
    public class DBController : ControllerBase
    {
        private readonly TaskService _taskService;
        private readonly UserService _userService;
        public DBController(IConfiguration configuration)
        {
            _taskService = new TaskService(configuration);
            _userService = new UserService(configuration);
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

                var createdTask = await _taskService.CreateTask(task.Text, task.EmailData, task.PhoneData, task.PersonalNumber);

                return CreatedAtAction(nameof(GetTask), new { id = createdTask.id }, createdTask);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("Task/{id}")]
        public async Task<ActionResult<TaskTableModel>> GetTask(int id)
        {
            var task = await _taskService.GetTask(id);

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

                var createdUser = await _userService.CreateUser(userModel.name, userModel.password);

                return CreatedAtAction(nameof(GetTask), new { id = createdUser.id }, createdUser);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("check_token")]
        public async Task<IActionResult> checkToken([FromBody] string token)
        {
            var tokenInfo = _userService.GetTokenInfo(token);

            if (tokenInfo == null)
                return BadRequest("token not valid");
            else
                return Ok("token is valid");
        }

        [HttpPost("Token")]
        public async Task<IActionResult> GetToken([FromBody] User userModel)
        {
            try
            {
                if (userModel == null)
                    return BadRequest("user data is null");

                var dbUser = await _userService.GetUser(userModel.id ?? 0, userModel.name, userModel.password);

                if (dbUser == null)
                    return Unauthorized("user not found or data is incorrect");

                var token = _userService.GenerateJwtToken(new Dictionary<string, object?>()
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
            var token = _userService.GenerateJwtToken(new Dictionary<string, object?>()
                    {
                        { "username", "12345" },
                        { "expiration", DateTime.Now.AddHours(-1) }
                    });

            var tokenInfo = _userService.GetTokenInfo(token);
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

                var task = await _taskService.UpdateStatus(model.id, model.status);
                if (task == null)
                    return NotFound("task not found");

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
            var status = await _taskService.GetTaskStatus(id);
            if (status == null)
                return NotFound("Task not found");

            return Ok(status);
        }

        [HttpDelete("User")]
        public async Task<IActionResult> DeleteUser([FromBody] User userModel)
        {
            try
            {
                if (userModel == null)
                    return BadRequest("user data is null");

                var deleted = await _userService.DeleteUser(userModel.id ?? 0, userModel.name, userModel.password);
                if (deleted == null)
                    return NotFound("user not found");

                return Ok("user deleted");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
