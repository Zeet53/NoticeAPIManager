using DataBaseAPI.Models;
using LinqToDB;
using LinqToDB.Async;

namespace DataBaseAPI.Services;

public class TaskService
{
    private readonly AppDataConnection _db;
    private readonly IConfiguration _configuration;

    public TaskService(IConfiguration configuration)
    {
        _db = new AppDataConnection();
        _configuration = configuration;
    }

    public async Task<TaskTableModel> CreateTask(string text, string? emailData, string? phoneData, int? personalNumber)
    {
        var taskModel = new TaskTableModel
        {
            text = text,
            email_data = emailData,
            phone_data = phoneData,
            personal_number = personalNumber,
            created_time = DateTime.UtcNow,
            updated_time = DateTime.UtcNow,
            status = "accepted"
        };

        var insertId = Convert.ToInt32(await _db.InsertWithIdentityAsync(taskModel));
        return await _db.MailTasks.FirstOrDefaultAsync(t => t.id == insertId);
    }

    public async Task<TaskTableModel?> GetTask(int id)
    {
        return await _db.MailTasks.FirstOrDefaultAsync(t => t.id == id);
    }

    public async Task<TaskTableModel?> UpdateStatus(int id, string status)
    {
        var task = await _db.MailTasks.FirstOrDefaultAsync(t => t.id == id);
        if (task == null) return null;

        task.status = status.ToLower();
        task.updated_time = DateTime.UtcNow;
        await _db.UpdateAsync(task);
        return task;
    }

    public async Task<string?> GetTaskStatus(int id)
    {
        var task = await _db.MailTasks.FirstOrDefaultAsync(t => t.id == id);
        return task?.status;
    }

    public async Task<TaskTableModel?> DeleteFromMain(int id)
    {
        var task = await _db.MailTasks.FirstOrDefaultAsync(t => t.id == id);
        if (task == null) return null;

        await _db.DeleteAsync(task);
        return task;
    }

    public async Task<ArchiveTaskTable> AddToArchive(TaskTableModel task)
    {
        var archiveTask = new ArchiveTaskTable()
        {
            id = task.id,
            text = task.text,
            email_data = task.email_data,
            phone_data = task.phone_data,
            personal_number = task.personal_number
        };

        var insertId = Convert.ToInt32(await _db.InsertWithIdentityAsync(archiveTask));
        return await _db.ArchiveTasks.FirstOrDefaultAsync(t => t.id == insertId);
    }

    public async Task<List<TaskTableModel>> getExpiredTask()
    {
        var expiredDays = _configuration.GetValue<int>("Expired_days");
        var cutoffDate = DateTime.UtcNow.AddDays(-expiredDays);

        return await _db.MailTasks
            .Where(t => t.status != "accepted" && t.updated_time < cutoffDate)
            .ToListAsync();
    }
}
