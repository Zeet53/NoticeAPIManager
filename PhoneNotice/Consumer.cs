using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace PhoneNotice;

public class Consumer : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = "localhost", UserName = "admin", Password = "admin" };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync("phone_notifications", durable: true, exclusive: false, autoDelete: false);
        await channel.QueueDeclareAsync("status_updates", durable: true, exclusive: false, autoDelete: false);

        Console.WriteLine("[PhoneNotice] Consumer запущен, ожидание сообщений...");

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, args) =>
        {
            var body = args.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var message = JsonSerializer.Deserialize<Message>(json);
            var taskId = message?.id ?? 0;

            try
            {
                if (message != null)
                    PhoneSender.Send(message);

                await PublishStatus(channel, taskId, "sended");
                await channel.BasicAckAsync(args.DeliveryTag, false);
            }
            catch
            {
                await PublishStatus(channel, taskId, "error");
                await channel.BasicNackAsync(args.DeliveryTag, false, true);
            }
        };

        await channel.BasicConsumeAsync("phone_notifications", autoAck: false, consumer: consumer);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private static async Task PublishStatus(IChannel channel, int taskId, string status)
    {
        var statusMsg = JsonSerializer.SerializeToUtf8Bytes(new { taskId, status });
        await channel.BasicPublishAsync(exchange: "", routingKey: "status_updates", body: statusMsg);
    }
}
