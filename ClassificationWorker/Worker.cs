using API.Models;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Common.Cosmos;
using Microsoft.ML;
using static ClassificationTrainer.ClassificationModel;

namespace ClassificationWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private ServiceBusProcessor _serviceBusProcessor;
        private IRepository<RawImage> _rawImageRepository;
        private BlobContainerClient _rawImageContainerClient;
        private BlobContainerClient _classificationModelContainerClient;
        private MLContext _mlContext;
        private DataViewSchema _predictionPipelineSchema;
        private ITransformer _predictionPipeline;
        private PredictionEngine<ModelInput, ModelOutput> _predictionEngine;

        public Worker(ILogger<Worker> logger, ServiceBusClient serviceBusClient, IRepository<RawImage> rawImageRepository, BlobServiceClient blobServiceClient, MLContext mlContext)
        {
            _rawImageContainerClient = blobServiceClient.GetBlobContainerClient("rawimage");
            _rawImageRepository = rawImageRepository;
            _logger = logger;
            _serviceBusProcessor = serviceBusClient.CreateProcessor("imagesuploaded", "imagesuploaded_classificationservice");
            _serviceBusProcessor.ProcessMessageAsync += ServiceBusMessageHandler;
            _serviceBusProcessor.ProcessErrorAsync += ServiceBusErrorHandler;
            _mlContext = mlContext;
            _classificationModelContainerClient = blobServiceClient.GetBlobContainerClient("classificationmodel");
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

        private Task ServiceBusErrorHandler(ProcessErrorEventArgs arg)
        {
            _logger.LogError(arg.Exception.ToString());
            return Task.CompletedTask;
        }

        private async Task ServiceBusMessageHandler(ProcessMessageEventArgs arg)
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
                var imageBinary = await DownloadFile(hashedId);
                var modelInput = new ModelInput() { ImageSource = imageBinary, Label = String.Empty };
                var modelOutput = _predictionEngine.Predict(modelInput);

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

            _predictionEngine = _mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(_predictionPipeline);
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
            await _serviceBusProcessor.StartProcessingAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}