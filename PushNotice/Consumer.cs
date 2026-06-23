using PushNotice.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace PushNotice;

public class Consumer : BackgroundService
{
    private readonly PushSender _pushSender;

    public Consumer(PushSender pushSender)
    {
        _pushSender = pushSender;
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

                await channel.QueueDeclareAsync("push_notifications", durable: true, exclusive: false, autoDelete: false);
                await channel.QueueDeclareAsync("status_updates", durable: true, exclusive: false, autoDelete: false);

                Console.WriteLine("[PushNotice] Consumer запущен, ожидание сообщений...");

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (model, args) =>
                {
                    var body = args.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<PushMessage>(json);
                    var taskId = message?.id ?? 0;

                    try
                    {
                        if (message != null)
                            _pushSender.Send(message);

                        await PublishStatus(channel, taskId, "sended");
                        await channel.BasicAckAsync(args.DeliveryTag, false);
                    }
                    catch
                    {
                        await PublishStatus(channel, taskId, "error");
                        await channel.BasicNackAsync(args.DeliveryTag, false, true);
                    }
                };

                await channel.BasicConsumeAsync("push_notifications", autoAck: false, consumer: consumer);

                await Task.Delay(Timeout.Infinite, stoppingToken);
                break;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine($"[PushNotice] RabbitMQ connection failed, retrying in 5s... {ex.Message}");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private static async Task PublishStatus(IChannel channel, int taskId, string status)
    {
        var statusMsg = JsonSerializer.SerializeToUtf8Bytes(new { taskId, status });
        await channel.BasicPublishAsync(exchange: "", routingKey: "status_updates", body: statusMsg);
    }
}
