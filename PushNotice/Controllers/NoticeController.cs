using PushNotice.Models;
using Microsoft.AspNetCore.Mvc;

namespace PushNotice.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SendPushController : ControllerBase
    {
        private readonly IPushSender _pushSender;

        public SendPushController(IPushSender pushSender)
        {
            _pushSender = pushSender;
        }

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

            _pushSender.Send(msg);

            return Ok("Push-уведомление отправлено");
        }
    }

}
