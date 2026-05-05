using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;

namespace TestApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SendController : ControllerBase
    {
        private readonly IConfiguration _config;
        public SendController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Mailing profile)
        {
            var result = await profile.sendMail(_config["MAIL_ADDRESS"], _config["MAIL_PASSWORD"]);

            if (result)
            {
                return Ok("Письмо успешно отправлено");
            }
            else
            {
                return StatusCode(500, "Ошибка при отправке письма");
            }
        }

        public class Mailing
        {
            public string Receiver { get; set; }
            public string Text { get; set; }

            public Mailing()
            {
                Receiver = "";
                Text = "";
            }

            public Mailing(string receivers, string text)
            {
                Receiver = receivers;
                Text = text;
            }

            public async Task<Boolean> sendMail(string address, string api_key)
            {
                try
                {
                    using var client = new SmtpClient("smtp.yandex.ru", 587);
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(address, api_key);

                    var message = new MailMessage();
                    message.From = new MailAddress(address);
                    message.To.Add(Receiver);

                    message.Subject = "Someone theme";
                    message.Body = Text;

                    await client.SendMailAsync(message);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
        }
    }
}