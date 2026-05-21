using Microsoft.AspNetCore.Mvc;

namespace EmailService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SendController : ControllerBase
    {
        private readonly EmailSender _emailSender;

        public SendController(EmailSender emailSender)
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

    public class SendRequest
    {
        public int id { get; set; }
        public string text { get; set; } = string.Empty;
        public string receiver { get; set; } = string.Empty;
    }
}
