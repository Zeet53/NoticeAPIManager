using PhoneNotice.Models;
using Microsoft.AspNetCore.Mvc;

namespace PhoneNotice.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SendPhoneController : ControllerBase
    {
        private readonly IPhoneSender _phoneSender;

        public SendPhoneController(IPhoneSender phoneSender)
        {
            _phoneSender = phoneSender;
        }

        [HttpPost]
        public IActionResult SendPhoneNotice([FromBody] SendRequest request)
        {
            if (!CheckNumberValid(request.phoneNumber))
                return BadRequest("Invalid phone number");

            var msg = new PhoneMessage
            {
                id = request.id,
                Text = request.text,
                sendData = request.phoneNumber
            };

            _phoneSender.Send(msg);

            return Ok("SMS отправлено");
        }

        private bool CheckNumberValid(string number)
        {
            return number.Length == 11 && number.StartsWith('8');
        }
    }

}
