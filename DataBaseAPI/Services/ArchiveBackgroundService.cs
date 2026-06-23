namespace DataBaseAPI.Services;

public class ArchiveBackgroundService : BackgroundService
{
    private readonly IConfiguration _configuration;

    public ArchiveBackgroundService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var taskService = new TaskService(_configuration);
                var expiredTasks = await taskService.GetExpiredTask();

                foreach (var task in expiredTasks)
                {
                    await taskService.AddToArchive(task);
                    await taskService.DeleteFromMain(task.id);
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
