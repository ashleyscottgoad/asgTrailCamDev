using Azure.Storage.Blobs;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

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

        [HttpGet(Name = "GetRawImages")]
        public IEnumerable<RawImage> Get()
        {
            var list = new List<RawImage>();
            list.Add(new RawImage() { Id = Guid.NewGuid(), Location = new Microsoft.Azure.Cosmos.Spatial.Point(-77.52, 37.33) { } });
            return list;
        }
        
        [ActionName("Index")]
        [HttpPost]
        public async Task<string> Upload()
        {
            var guid = Guid.NewGuid();

            var boundary = HeaderUtilities.RemoveQuotes(
             MediaTypeHeaderValue.Parse(Request.ContentType).Boundary
            ).Value;

            var reader = new MultipartReader(boundary, Request.Body);

            var section = await reader.ReadNextSectionAsync();

            string response = string.Empty;
            await UploadFile(reader, section, guid);
                
            return guid.ToString();
        }

        private async Task<bool> UploadFile(MultipartReader reader, MultipartSection? section, Guid id)
        {
            while (section != null)
            {
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(
                 section.ContentDisposition, out var contentDisposition
                );

                if (hasContentDispositionHeader)
                {
                    if (contentDisposition.DispositionType.Equals("form-data") &&
                    (!string.IsNullOrEmpty(contentDisposition.FileName.Value) ||
                    !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value)))
                    {
                        var blobClient = _rawImageContainerClient.GetBlobClient(id.ToString());
                        byte[] bytes;
                        using (var memoryStream = new MemoryStream())
                        {
                            await section.Body.CopyToAsync(memoryStream);
                            bytes = memoryStream.ToArray();
                        }
                        
                        using(var blobStream = new MemoryStream(bytes))
                        {
                            await blobClient.UploadAsync(blobStream);
                        }
                    }
                }
                section = await reader.ReadNextSectionAsync();
            }
            return true;
        }

        /*
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
        */
    }
}
