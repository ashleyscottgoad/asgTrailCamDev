using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class RawImageController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
