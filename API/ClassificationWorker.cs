using API.Models;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Common.Cosmos;
using Microsoft.ML;
using static Common.Models.ClassificationModel;

namespace API
{
    public class ClassificationWorker : BackgroundService
    {
        private readonly ILogger<ClassificationWorker> _logger;
        private ServiceBusProcessor _serviceBusClassificationProcessor;
        private IRepository<RawImage> _rawImageRepository;
        private BlobContainerClient _rawImageContainerClient;
        private BlobContainerClient _classificationModelContainerClient;
        private MLContext _mlContext;
        private DataViewSchema _predictionPipelineSchema;
        private ITransformer _predictionPipeline;

        public ClassificationWorker(
            ILogger<ClassificationWorker> logger, 
            ServiceBusClient serviceBusClient, 
            IRepository<RawImage> rawImageRepository, 
            BlobServiceClient blobServiceClient, 
            MLContext mlContext)
        {
            _rawImageContainerClient = blobServiceClient.GetBlobContainerClient("rawimage");
            _rawImageRepository = rawImageRepository;
            _logger = logger;
            _serviceBusClassificationProcessor = serviceBusClient.CreateProcessor("imagesuploaded", "imagesuploaded_classificationservice");
            _serviceBusClassificationProcessor.ProcessMessageAsync += ServiceBusClassificationMessageHandler;
            _serviceBusClassificationProcessor.ProcessErrorAsync += ServiceBusClassificationErrorHandler;
            _classificationModelContainerClient = blobServiceClient.GetBlobContainerClient("classificationmodel");
            _mlContext = mlContext;
            LoadClassificationModel();
        }

        private async Task<RawImage> RetrieveCosmos(string hashedId)
        {
            return await _rawImageRepository.GetItemAsync(hashedId);
        }

        private async Task<RawImage> SaveSuggestedClassification(string hashedId, SuggestedClassification suggestedClassification)
        {
            return await _rawImageRepository.UpdateItemAsync(hashedId, x=>
            {
                x.SuggestedClassification = suggestedClassification;
            });
        }

        private Task ServiceBusClassificationErrorHandler(ProcessErrorEventArgs arg)
        {
            _logger.LogError(arg.Exception.ToString());
            return Task.CompletedTask;
        }

        private async Task ServiceBusClassificationMessageHandler(ProcessMessageEventArgs arg)
        {
            string hashedId = arg.Message.Body.ToString();
            _logger.LogInformation($"Received: {hashedId} from subscription");

            var imageAttributes = await RetrieveCosmos(hashedId);

            if (imageAttributes.SuggestedClassification != null || imageAttributes.Metadata.ContainsKey("classification"))
            {
                _logger.LogWarning($"ServiceBusMessageHandler::Id={hashedId} does not need classifying.");
            }
            else
            {
                var imageBinary = DownloadFile(hashedId).Result;
                var modelInput = new ModelInput() { ImageSource = imageBinary, Label = String.Empty };

                PredictionEngine<ModelInput, ModelOutput> predictionEngine =
                        _mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(_predictionPipeline);

                ModelOutput modelOutput = new ModelOutput();

                predictionEngine.Predict(modelInput, ref modelOutput);

                var suggested = new SuggestedClassification();
                suggested.Label = modelOutput.PredictedLabel;
                suggested.Score = modelOutput.Score.Max();

                await SaveSuggestedClassification(hashedId, suggested);
            }

            // complete the message. messages is deleted from the subscription. 
            await arg.CompleteMessageAsync(arg.Message);
        }

        private void LoadClassificationModel()
        {
            var modelClient = _classificationModelContainerClient.GetBlobClient("ClassificationModel.zip");

            using(var stream = new MemoryStream())
            {
                modelClient.DownloadTo(stream);
                _predictionPipeline = _mlContext.Model.Load(stream, out _predictionPipelineSchema);
            }
        }

        private async Task<byte[]> DownloadFile(string hashedId)
        {
            var blobClient = _rawImageContainerClient.GetBlobClient(hashedId);

            using (var blobStream = new MemoryStream())
            {
                var res = await blobClient.DownloadToAsync(blobStream);
                return blobStream.ToArray();
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _serviceBusClassificationProcessor.StartProcessingAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}