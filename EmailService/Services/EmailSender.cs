using EmailService.Models;
using System.Net;
using System.Net.Mail;

namespace EmailService;

public class EmailSender : IEmailSender
{
    private readonly string _address;
    private readonly string _password;

    public EmailSender(IConfiguration config)
    {
        _address = config["MAIL_ADDRESS"] ?? throw new InvalidOperationException("MAIL_ADDRESS not configured");
        _password = config["MAIL_PASSWORD"] ?? throw new InvalidOperationException("MAIL_PASSWORD not configured");
    }

    public async Task Send(EmailMessage msg)
    {
        try
        {
            using var client = new SmtpClient("smtp.yandex.ru", 587);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(_address, _password);

            var message = new MailMessage();
            message.From = new MailAddress(_address);
            message.To.Add(msg.sendData);
            message.Subject = "Someone theme";
            message.Body = msg.Text;

            await client.SendMailAsync(message);

            Console.WriteLine($"[EmailService] Письмо отправлено: id={msg.id}, на адрес={msg.sendData}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService] Ошибка отправки письма id={msg.id}: {ex.Message}");
        }
    }
}

