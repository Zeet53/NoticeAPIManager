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

        [HttpPost("Task")]
        public async Task<IActionResult> CreateNewTask([FromBody] CreateTaskRequest task)
        {
            try
            {
                if (task == null)
                    return BadRequest("Task data is null");

                if (string.IsNullOrWhiteSpace(task.Text))
                    return BadRequest("Text field is required");

                var createdTask = await _taskService.CreateTask(task.UserId, task.Text, task.EmailData, task.PhoneData, task.PersonalNumber);

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

        [HttpPost("User/exists")]
        public async Task<IActionResult> CheckUserExists([FromBody] UserCheckRequest userModel)
        {
            try
            {
                if (userModel == null || userModel.id == null)
                    return BadRequest("id, name and password are required");

                if (string.IsNullOrWhiteSpace(userModel.name) || string.IsNullOrWhiteSpace(userModel.password))
                    return BadRequest("id, name and password are required");

                var exists = await _userService.UserExists(
                    userModel.id.Value, userModel.name, userModel.password);

                return Ok(new { exists });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
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

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok();
        }

        [HttpPut("Task")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest model)
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

        [HttpGet("notifications/{userId}")]
        public async Task<IActionResult> GetUserNotifications(int userId)
        {
            try
            {
                var tasks = await _taskService.GetUserTasks(userId);
                var archive = await _taskService.GetUserArchiveTasks(userId);

                return Ok(new { tasks, archive });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("User")]
        public async Task<IActionResult> DeleteUser([FromBody] UserCheckRequest userModel)
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
