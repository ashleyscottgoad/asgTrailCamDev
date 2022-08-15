using Azure.Messaging.ServiceBus;

namespace ClassificationWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private ServiceBusProcessor _serviceBusProcessor;

        public Worker(ILogger<Worker> logger, ServiceBusClient serviceBusClient)
        {
            _logger = logger;
            _serviceBusProcessor = serviceBusClient.CreateProcessor("imagesuploaded", "imagesuploaded_classificationservice");
            _serviceBusProcessor.ProcessMessageAsync += ServiceBusMessageHandler;
            _serviceBusProcessor.ProcessErrorAsync += ServiceBusErrorHandler;
        }

        private Task ServiceBusErrorHandler(ProcessErrorEventArgs arg)
        {
            _logger.LogError(arg.Exception.ToString());
            return Task.CompletedTask;
        }

        private Task ServiceBusMessageHandler(ProcessMessageEventArgs arg)
        {
            string body = arg.Message.Body.ToString();
            _logger.LogInformation($"Received: {body} from subscription");

            // complete the message. messages is deleted from the subscription. 
            return arg.CompleteMessageAsync(arg.Message);
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