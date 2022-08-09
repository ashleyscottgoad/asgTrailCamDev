using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RawImageController : Controller
    {
        [HttpGet(Name = "GetImages")]
        public IEnumerable<RawImage> Get()
        {
            var list = new List<RawImage>();

            list.Add(new RawImage() { Id = Guid.NewGuid(), Location = new Microsoft.Azure.Cosmos.Spatial.Point(-77.52, 37.33) { } });

            return list;
        }
    }
}
