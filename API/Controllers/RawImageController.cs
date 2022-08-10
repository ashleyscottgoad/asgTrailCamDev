using API.Models;
using Azure.Storage.Blobs;
using Common.Cosmos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

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

            var bytes = await BytesFromRequest(reader, section);

            var imageInfo = Image.Identify(bytes);

            var hashedId = GetFileHash(imageInfo, bytes.Length);

            var uploadTask = UploadFile(bytes, hashedId);

            var cosmosTask = StoreCosmos(imageInfo, hashedId);

            Task.WaitAll(uploadTask, cosmosTask);

            return guid.ToString();
        }

        private string GetFileHash(IImageInfo imageInfo, int length)
        {
            var exifProfile = imageInfo.Metadata?.ExifProfile;
            var sDateTimeOriginal = exifProfile.Values.FirstOrDefault(x => x.Tag == ExifTag.DateTimeOriginal);
            DateTime dateTime = DateTime.MinValue;
            if(sDateTimeOriginal != null)
            {
                bool success = DateTime.TryParseExact(sDateTimeOriginal.ToString(), "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime);
            }
            string source = dateTime.ToString() + length.ToString();

            using(SHA256 sha = SHA256.Create())
            {
                return GetHash(sha, source);
            }
        }

        private static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        private async Task<bool> StoreCosmos(IImageInfo imageInfo, string id)
        {
            RawImage image = new RawImage() { id = id };
            await _rawImageRepository.CreateItemAsync(image);
            return true;
        }

        private async Task<byte[]> BytesFromRequest(MultipartReader reader, MultipartSection? section)
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
                        using (var memoryStream = new MemoryStream())
                        {
                            await section.Body.CopyToAsync(memoryStream);
                            return memoryStream.ToArray();
                        }
                    }
                }
                section = await reader.ReadNextSectionAsync();
            }
            return null;
        }

        private async Task<bool> UploadFile(byte[] bytes, string hashedId)
        {
            var blobClient = _rawImageContainerClient.GetBlobClient(hashedId);

            using (var blobStream = new MemoryStream(bytes))
            {
                var res = await blobClient.UploadAsync(blobStream);
                return true;
            }
        }
    }
}
