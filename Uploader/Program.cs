using System.Net.Http.Headers;

class Program
{
    string filePath = @"D:\OneDrive - Clever Devices, Ltd\Pictures\trailcam\08072022\pictures";
    string devApiBaseAddress = "https://localhost:7015";
    string prodApiBaseAddress = "https://asgtrailcamdev.azurewebsites.net/";
    bool isProduction = true;

    public static void Main()

    {
        Program p = new Program();
        p.Upload();
    }

    HttpClient GetClient()
    {
        var client = new HttpClient();
        
        if(isProduction)
        {
            client.BaseAddress = new Uri(prodApiBaseAddress);
        }
        else
        {
            client.BaseAddress = new Uri(devApiBaseAddress);
        }
        
        return client;
    }

    void Delete()
    {
        var result = GetClient().DeleteAsync(GetRequestUri()).Result;
    }

    string GetRequestUri()
    {
        return "/RawImage";
    }

    void Upload(int limit = int.MaxValue)
    {
        var files = Directory.GetFiles(filePath);

        int qty = 0;

        foreach (var f in files)
        {
            if (qty > limit) return;
            Console.WriteLine($"file={f}");
            var f0 = Path.GetFileName(f);
            var creationTime = File.GetCreationTime(f);
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
            content.Add(new StringContent(f0), "filename");
            content.Add(new StringContent(creationTime.ToString("yyyyMMddHHmmss")), "creationTime");

            var result = GetClient().PostAsync(GetRequestUri(), content).Result;
            Console.WriteLine($"result={result}, qty={qty}");
            qty++;
        }
    }
}

