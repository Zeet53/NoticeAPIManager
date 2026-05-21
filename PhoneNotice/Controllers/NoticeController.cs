using Microsoft.AspNetCore.Mvc;

namespace PhoneNotice.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SendPhoneController : ControllerBase
    {
        [HttpPost]
        public IActionResult SendPhoneNotice([FromBody] SendRequest request)
        {
            if (!checkNumberValid(request.phoneNumber))
                return BadRequest("Invalid phone number");

            var msg = new Message
            {
                id = request.id,
                Text = request.text,
                sendData = request.phoneNumber
            };

            PhoneSender.Send(msg);

            return Ok("SMS отправлено");
        }

        private bool checkNumberValid(string number)
        {
            return number.Length == 11 && number.StartsWith('8');
        }
    }

    public class SendRequest
    {
        public int id { get; set; }
        public string text { get; set; } = string.Empty;
        public string phoneNumber { get; set; } = string.Empty;
    }
}
