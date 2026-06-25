using DataBaseAPI.Models;
using LinqToDB;
using LinqToDB.Async;

namespace DataBaseAPI.Services;

public class TaskService : ITaskService
{
    private readonly AppDataConnection _db;
    private readonly IConfiguration _configuration;
    private readonly IRedisCacheService _cache;

    public TaskService(IConfiguration configuration, IRedisCacheService cache)
    {
        _db = new AppDataConnection();
        _configuration = configuration;
        _cache = cache;
    }

    public async Task<TaskTableModel> CreateTask(int userId, string text, string? emailData, string? phoneData, int? personalNumber)
    {
        var taskModel = new TaskTableModel
        {
            user_id = userId,
            text = text,
            email_data = emailData,
            phone_data = phoneData,
            personal_number = personalNumber,
            created_time = DateTime.UtcNow,
            updated_time = DateTime.UtcNow,
            status = "accepted"
        };

        var insertId = Convert.ToInt32(await _db.InsertWithIdentityAsync(taskModel));
        var createdTask = await _db.MailTasks.FirstOrDefaultAsync(t => t.id == insertId);

        await _cache.SetAsync($"Task:{createdTask.id}", createdTask);

        return createdTask;
    }

    public async Task<TaskTableModel?> GetTask(int id)
    {
        var cached = await _cache.GetAsync<TaskTableModel>($"Task:{id}");
        if (cached != null)
            return cached;

        var task = await _db.MailTasks.FirstOrDefaultAsync(t => t.id == id);

        if (task != null)
            await _cache.SetAsync($"Task:{task.id}", task);

        return task;
    }

    public async Task<TaskTableModel?> UpdateStatus(int id, string status)
    {
        var task = await _db.MailTasks.FirstOrDefaultAsync(t => t.id == id);
        if (task == null) return null;

        task.status = status.ToLower();
        task.updated_time = DateTime.UtcNow;
        await _db.UpdateAsync(task);

        await _cache.DeleteAsync($"Task:{id}");

        return task;
    }

    public async Task<string?> GetTaskStatus(int id)
    {
        var task = await _db.MailTasks.FirstOrDefaultAsync(t => t.id == id);
        return task?.status;
    }

    public async Task<List<TaskTableModel>> GetUserTasks(int userId)
    {
        return await _db.MailTasks
            .Where(t => t.user_id == userId)
            .OrderByDescending(t => t.updated_time)
            .ToListAsync();
    }

    public async Task<List<ArchiveTaskTable>> GetUserArchiveTasks(int userId)
    {
        return await _db.ArchiveTasks
            .Where(t => t.user_id == userId)
            .OrderByDescending(t => t.archiving_time)
            .ToListAsync();
    }

    public async Task<TaskTableModel?> DeleteFromMain(int id)
    {
        var task = await _db.MailTasks.FirstOrDefaultAsync(t => t.id == id);
        if (task == null) return null;

        await _db.DeleteAsync(task);

        await _cache.DeleteAsync($"Task:{id}");

        return task;
    }

    public async Task<ArchiveTaskTable> AddToArchive(TaskTableModel task)
    {
        var archiveTask = new ArchiveTaskTable()
        {
            id = task.id,
            user_id = task.user_id,
            text = task.text,
            email_data = task.email_data,
            phone_data = task.phone_data,
            personal_number = task.personal_number
        };

        var insertId = Convert.ToInt32(await _db.InsertWithIdentityAsync(archiveTask));
        var created = await _db.ArchiveTasks.FirstOrDefaultAsync(t => t.id == insertId);

        await _cache.SetAsync($"Archive:{created.id}", created);

        return created;
    }

    public async Task<List<TaskTableModel>> GetExpiredTask()
    {
        var expiredDays = _configuration.GetValue<int>("Expired_days");
        var cutoffDate = DateTime.UtcNow.AddDays(-expiredDays);

        return await _db.MailTasks
            .Where(t => t.status != "accepted" && t.updated_time < cutoffDate)
            .ToListAsync();
    }
}
