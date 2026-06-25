namespace DataBaseAPI.Services;

public class ArchiveBackgroundService : BackgroundService
{
    private readonly ITaskService _taskService;

    public ArchiveBackgroundService(ITaskService taskService)
    {
        _taskService = taskService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var expiredTasks = await _taskService.GetExpiredTask();

                foreach (var task in expiredTasks)
                {
                    await _taskService.AddToArchive(task);
                    await _taskService.DeleteFromMain(task.id);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Archive error: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
