using Microsoft.Azure.Cosmos.Spatial;

namespace API.Controllers
{
    public class RawImage
    {
        public Guid Id { get; set; }

        public Point? Location { get; set; }
    }
}