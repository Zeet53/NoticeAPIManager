using Microsoft.AspNetCore.Mvc;

namespace PushNotice.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SendPushController : ControllerBase
    {
        [HttpPost]
        public IActionResult SendPushNotice([FromBody] SendRequest request)
        {
            if (request.personalNumber <= 0)
                return BadRequest("Invalid personal number");

            var msg = new PushMessage
            {
                id = request.id,
                Text = request.text,
                sendData = request.personalNumber.ToString()
            };

            PushSender.Send(msg);

            return Ok("Push-уведомление отправлено");
        }
    }

    public class SendRequest
    {
        public int id { get; set; }
        public string text { get; set; } = string.Empty;
        public int personalNumber { get; set; }
    }
}
