using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EmailService;

public class Consumer : BackgroundService
{
    private readonly EmailSender _emailSender;

    public Consumer(EmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost", UserName = "admin", Password = "admin" };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync("email_notifications", durable: true, exclusive: false, autoDelete: false);
        await channel.QueueDeclareAsync("status_updates", durable: true, exclusive: false, autoDelete: false);

        Console.WriteLine("[EmailService] Consumer запущен, ожидание сообщений...");

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, args) =>
        {
            try
            {
                var body = args.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<EmailMessage>(json);

                if (message != null)
                {
                    await _emailSender.Send(message);
                    await PublishStatus(channel, message.id, "sended");
                }

                await channel.BasicAckAsync(args.DeliveryTag, false);
            }
            catch
            {
                var body = args.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<EmailMessage>(json);

                if (message != null)
                    await PublishStatus(channel, message.id, "error");

                await channel.BasicNackAsync(args.DeliveryTag, false, true);
            }
        };

        await channel.BasicConsumeAsync("email_notifications", autoAck: false, consumer: consumer);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private static async Task PublishStatus(IChannel channel, int taskId, string status)
    {
        var statusMsg = JsonSerializer.SerializeToUtf8Bytes(new { taskId, status });
        await channel.BasicPublishAsync(exchange: "", routingKey: "status_updates", body: statusMsg);
    }
}
