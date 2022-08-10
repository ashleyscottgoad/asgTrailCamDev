using API.Models;
using Azure.Storage.Blobs;
using Common.Cosmos;
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
        private IRepository<RawImage> _rawImageRepository;

        public RawImageController(BlobServiceClient blobServiceClient, IRepository<RawImage> rawImageRepository)
        {
            _rawImageContainerClient = blobServiceClient.GetBlobContainerClient("rawimage");
            _rawImageRepository = rawImageRepository;
        }

        [HttpGet(Name = "GetRawImages")]
        public async Task<IEnumerable<RawImage>> Get()
        {
            return await _rawImageRepository.GetItemsAsync(x => x.id != null);
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

            var cosmosTask = StoreCosmos(guid);
            var uploadTask = UploadFile(reader, section, guid);
            Task.WaitAll(new Task[] { uploadTask, cosmosTask });

            return guid.ToString();
        }

        private async Task<bool> StoreCosmos(Guid guid)
        {
            RawImage image = new RawImage() { id = guid.ToString() };
            await _rawImageRepository.CreateItemAsync(image);
            return true;
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

                        using (var blobStream = new MemoryStream(bytes))
                        {
                            await blobClient.UploadAsync(blobStream);
                        }
                    }
                }
                section = await reader.ReadNextSectionAsync();
            }
            return true;
        }
    }
}
