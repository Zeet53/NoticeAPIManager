namespace DataBaseAPI.Services;

public interface IStatusUpdateService
{
    Task UpdateStatus(int taskId, string status);
}
