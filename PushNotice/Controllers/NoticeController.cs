using Microsoft.AspNetCore.Mvc;

namespace TestApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SendPushController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> SendPushNotice([FromBody] int personalNumber)
        {
            if (personalNumber != 0)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }
    }
}