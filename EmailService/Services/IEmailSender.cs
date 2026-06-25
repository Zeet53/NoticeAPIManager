using EmailService.Models;

namespace EmailService;

public interface IEmailSender
{
    Task Send(EmailMessage msg);
}
