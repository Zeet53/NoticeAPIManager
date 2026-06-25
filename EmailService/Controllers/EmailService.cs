using EmailService.Models;
using Microsoft.AspNetCore.Mvc;

namespace EmailService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SendController : ControllerBase
    {
        private readonly IEmailSender _emailSender;

        public SendController(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SendRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.receiver) || !request.receiver.Contains('@'))
                return BadRequest("Invalid email");

            var msg = new EmailMessage
            {
                id = request.id,
                Text = request.text,
                sendData = request.receiver
            };

            await _emailSender.Send(msg);

            return Ok("Письмо отправлено");
        }
    }

}
