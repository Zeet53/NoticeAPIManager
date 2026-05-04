using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;

//Функции: отправить письмо
// xbdzzriivyxxxnwt

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


        // GET: api/test
        [HttpGet]
        public IActionResult GetAll()
        {
            // Здесь должна быть логика получения всех элементов
            return Ok(new { message = "Get all items" });
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
            public List<string> Receivers;
            public string Text;

            public Mailing()
            {
                Receivers = new List<string>();
                Text = "";
            }

            public Mailing(List<string> receivers, string text)
            {
                Receivers = receivers;
                Text = text;
            }

            public async Task<Boolean> sendMail(string address, string api_key)
            {
                try
                {
                    Console.WriteLine("vars:");
                    Console.WriteLine(address, api_key);

                    using var client = new SmtpClient("smtp.yandex.ru", 587);
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(address, api_key);

                    var message = new MailMessage();
                    message.From = new MailAddress(address);
                    foreach (var receiver in Receivers)
                    {
                        message.To.Add(receiver);
                    }

                    message.Subject = "Theme";
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