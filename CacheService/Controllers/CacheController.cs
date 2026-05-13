using CacheService.Models;
using CacheService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CacheService.Controllers
{
    [ApiController]
    [Route("")]
    public class CacheController : ControllerBase
    {
        private readonly RedisService _redis;

        public CacheController(RedisService redis)
        {
            _redis = redis;
        }

        private const string TaskPrefix = "Task:";
        private const string ArchivePrefix = "Archive:";
        private const string UserPrefix = "User:";

        [HttpPost("Task")]
        public async Task<IActionResult> SetTask([FromBody] TaskModel task)
        {
            await _redis.SetAsync($"{TaskPrefix}{task.id}", task);
            Console.WriteLine($"Успешно сохранено - {TaskPrefix}{task.id}");
            return Ok();
        }

        [HttpGet("Task/{id}")]
        public async Task<IActionResult> GetTask(int id)
        {
            var result = await _redis.GetAsync<TaskModel>($"{TaskPrefix}{id}");
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpDelete("Task/{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var result = await _redis.DeleteAsync($"{TaskPrefix}{id}");
            if (result) return Ok();
            else return NotFound();
        }



        [HttpPost("Archive")]
        public async Task<IActionResult> SetArchive([FromBody] ArchiveModel archive)
        {
            await _redis.SetAsync($"{ArchivePrefix}{archive.id}", archive);
            return Ok();
        }

        [HttpGet("Archive/{id}")]
        public async Task<IActionResult> GetArchive(int id)
        {
            var result = await _redis.GetAsync<ArchiveModel>($"{ArchivePrefix}{id}");
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpDelete("Archive/{id}")]
        public async Task<IActionResult> DeleteArchive(int id)
        {
            var result = await _redis.DeleteAsync($"{ArchivePrefix}{id}");
            if (result) return Ok();
            else return NotFound();
        }



        [HttpPost("User")]
        public async Task<IActionResult> SetUser([FromBody] UserModel user)
        {
            await _redis.SetAsync($"{UserPrefix}{user.id}", user);
            return Ok();
        }

        [HttpGet("User/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var result = await _redis.GetAsync<UserModel>($"{UserPrefix}{id}");
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpDelete("User/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result = await _redis.DeleteAsync($"{UserPrefix}{id}");
            if (result) return Ok();
            else return NotFound();
        }
    }
}