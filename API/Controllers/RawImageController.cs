﻿using API.Models;
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
            var boundary = HeaderUtilities.RemoveQuotes(
             MediaTypeHeaderValue.Parse(Request.ContentType).Boundary
            ).Value;

            var reader = new MultipartReader(boundary, Request.Body);

            var section = await reader.ReadNextSectionAsync();

            var multipartSectionBytes = await BytesFromRequest(reader, section);

            var fileSectionBytes = multipartSectionBytes.FirstOrDefault(x => x.SectionType == MultipartSectionType.File);

            var imageInfo = Image.Identify(fileSectionBytes.Bytes);

            var hashedId = GetFileHash(imageInfo, fileSectionBytes.Bytes.Length);

            var uploadTask = UploadFile(fileSectionBytes.Bytes, hashedId);

            var textSectionBytes = multipartSectionBytes.Where(x => x.SectionType == MultipartSectionType.Text).ToList();

            Dictionary<string, string> metadata = new Dictionary<string, string>();

            foreach(var tsb in textSectionBytes)
            {
                metadata.Add(tsb.Name, Encoding.UTF8.GetString(tsb.Bytes));
            }

            var rawImage = new RawImage(hashedId, metadata);

            var cosmosTask = StoreCosmos(rawImage);

            Task.WaitAll(uploadTask, cosmosTask);

            return hashedId;
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

        private async Task<bool> StoreCosmos(RawImage rawImage)
        {
            await _rawImageRepository.CreateItemAsync(rawImage);
            return true;
        }

        private async Task<IEnumerable<MultipartSectionBytes>> BytesFromRequest(MultipartReader reader, MultipartSection? section)
        {
            List<MultipartSectionBytes> result = new List<MultipartSectionBytes>();

            while (section != null)
            {
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(
                 section.ContentDisposition, out var contentDisposition
                );

                if (hasContentDispositionHeader)
                {
                    if (contentDisposition.DispositionType.Equals("form-data"))
                    {
                        MultipartSectionType multipartSectionType = MultipartSectionType.Text;
                        if (!string.IsNullOrEmpty(contentDisposition.FileName.Value) || !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value))        
                        {
                            multipartSectionType = MultipartSectionType.File;
                        }

                        using (var memoryStream = new MemoryStream())
                        {
                            await section.Body.CopyToAsync(memoryStream);
                            result.Add(new MultipartSectionBytes() { SectionType = multipartSectionType, Bytes = memoryStream.ToArray(), Name = contentDisposition.Name.Value });
                        }
                    }
                }
                section = await reader.ReadNextSectionAsync();
            }
            return result;
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
