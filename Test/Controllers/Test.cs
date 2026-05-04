using Microsoft.AspNetCore.Mvc;

namespace TestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        // GET: api/test
        [HttpGet]
        public IActionResult GetAll()
        {
            // Здесь должна быть логика получения всех элементов
            return Ok(new { message = "Get all items" });
        }
    }
}