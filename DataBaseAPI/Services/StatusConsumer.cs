using DataBaseAPI.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace DataBaseAPI.Services;

public class StatusConsumer : BackgroundService
{
    private readonly StatusUpdateService _statusUpdateService;

    public StatusConsumer(StatusUpdateService statusUpdateService)
    {
        _statusUpdateService = statusUpdateService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost", UserName = "admin", Password = "admin" };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = await factory.CreateConnectionAsync();
                await using var channel = await connection.CreateChannelAsync();

                await channel.QueueDeclareAsync("status_updates", durable: true, exclusive: false, autoDelete: false);

                Console.WriteLine("[StatusConsumer] Запущен, ожидание обновлений статуса...");

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (model, args) =>
                {
                    try
                    {
                        var body = args.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);
                        var statusMsg = JsonSerializer.Deserialize<StatusMessage>(json);

                        if (statusMsg != null)
                            await _statusUpdateService.UpdateStatus(statusMsg.taskId, statusMsg.status);

                        await channel.BasicAckAsync(args.DeliveryTag, false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[StatusConsumer] Ошибка: {ex.Message}");
                        await channel.BasicNackAsync(args.DeliveryTag, false, true);
                    }
                };

                await channel.BasicConsumeAsync("status_updates", autoAck: false, consumer: consumer);

                await Task.Delay(Timeout.Infinite, stoppingToken);
                break;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine($"[StatusConsumer] RabbitMQ connection failed, retrying in 5s... {ex.Message}");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}

