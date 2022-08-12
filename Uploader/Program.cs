using System.Net.Http.Headers;

class Program
{
    public static void Main()

    {

        using var client = new HttpClient();
        string apiBaseAddress = "https://localhost:7015";
        apiBaseAddress = "https://asgtrailcamdev.azurewebsites.net/";
        client.BaseAddress = new Uri(apiBaseAddress);
        var requestUri = "/RawImage";
        var files = Directory.GetFiles(@"D:\OneDrive - Clever Devices, Ltd\Pictures\trailcam\08072022\pictures");

        int qty = 0;

        foreach(var f in files)
        {
            Console.WriteLine($"file={f}");
            var f0 = Path.GetFileName(f);
            var classification = f0.Split("_".ToCharArray())[0];

            using var content = new MultipartFormDataContent();

            var fileContent = new StreamContent(new FileStream(f, FileMode.Open));

            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");

            fileContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")

            {
                Name = "File",
                FileName = f0
            };

            content.Add(fileContent);

            content.Add(new StringContent(classification), "classification");

            var result = client.PostAsync(requestUri, content).Result;
            Console.WriteLine($"result={result}");
            qty++;

            if (qty > 2) return;
        }
    }
}

