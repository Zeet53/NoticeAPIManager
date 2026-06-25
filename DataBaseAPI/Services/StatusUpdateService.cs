using System.Text;
using System.Text.Json;

namespace DataBaseAPI.Services;

public class StatusUpdateService : IStatusUpdateService
{
    private readonly HttpClient _httpClient;

    public StatusUpdateService(IConfiguration configuration)
    {
        var baseUrl = configuration.GetValue<string>("SelfUrl") ?? "http://localhost:5201";
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(2);
    }

    public async Task UpdateStatus(int taskId, string status)
    {
        var payload = JsonSerializer.Serialize(new { id = taskId, status });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync("Task", content);

        Console.WriteLine($"[StatusUpdate] taskId={taskId}, status={status}, result={response.StatusCode}");
    }
}
