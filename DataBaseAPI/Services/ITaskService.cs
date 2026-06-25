using DataBaseAPI.Models;

namespace DataBaseAPI.Services;

public interface ITaskService
{
    Task<TaskTableModel> CreateTask(int userId, string text, string? emailData, string? phoneData, int? personalNumber);
    Task<TaskTableModel?> GetTask(int id);
    Task<TaskTableModel?> UpdateStatus(int id, string status);
    Task<string?> GetTaskStatus(int id);
    Task<List<TaskTableModel>> GetUserTasks(int userId);
    Task<List<ArchiveTaskTable>> GetUserArchiveTasks(int userId);
    Task<TaskTableModel?> DeleteFromMain(int id);
    Task<ArchiveTaskTable> AddToArchive(TaskTableModel task);
    Task<List<TaskTableModel>> GetExpiredTask();
}
