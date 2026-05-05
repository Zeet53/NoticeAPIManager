using Microsoft.AspNetCore.Mvc;

namespace PhoneNotice.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SendPushController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> SendPushNotice([FromBody] string phoneNumber)
        {
            if (checkNumberValid(phoneNumber))
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }

        private bool checkNumberValid(string number)
        {
            if(number.Length == 11 && number.StartsWith('8'))
            {
                return true;
            }
            return false;
        }
    }
}