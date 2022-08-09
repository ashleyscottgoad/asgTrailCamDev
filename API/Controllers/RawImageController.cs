using Azure.Storage.Blobs;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RawImageController : Controller
    {
        private BlobContainerClient _rawImageContainerClient;

        public RawImageController(BlobServiceClient blobServiceClient)
        {
            _rawImageContainerClient = blobServiceClient.GetBlobContainerClient("rawimage");
        }

        /*
        [HttpGet(Name = "GetImages")]
        public IEnumerable<RawImage> Get()
        {
            var list = new List<RawImage>();
            list.Add(new RawImage() { Id = Guid.NewGuid(), Location = new Microsoft.Azure.Cosmos.Spatial.Point(-77.52, 37.33) { } });
            return list;
        }
        */

        [HttpGet(Name = "GenerateTestImage")]
        public async Task<RawImage> GenerateTestImage()
        {
            var image = new RawImage() { Id = Guid.NewGuid(), Location = new Microsoft.Azure.Cosmos.Spatial.Point(-77.52, 37.33) { } };
            var bytes = JsonSerializer.SerializeToUtf8Bytes(image);

            var blobClient = _rawImageContainerClient.GetBlobClient(image.Id.ToString());

            using (var stream = new MemoryStream(bytes))
            {
                await blobClient.UploadAsync(stream);
            }

            return image;
        }
    }
}
