using Microsoft.AspNetCore.Mvc;

namespace SmartHome.Controllers
{
    public class ApiController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
