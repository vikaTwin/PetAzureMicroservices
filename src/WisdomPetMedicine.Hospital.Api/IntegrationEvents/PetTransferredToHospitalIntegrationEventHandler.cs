using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using WisdomPetMedicine.Hospital.Api.Infrastructure;
using WisdomPetMedicine.Hospital.Api.IntegrationEvents;

namespace WisdomPetMedicine.Rescue.Api.IntegrationEvents
{
    public class PetTransferredToHospitalIntegrationEventHandler : BackgroundService
    {
        private readonly ServiceBusClient client;
        private readonly ServiceBusProcessor processor;

        private readonly IConfiguration configuration;
        private readonly ILogger<PetTransferredToHospitalIntegrationEventHandler> logger;
        private readonly IServiceScopeFactory serviceScopeFactory;

        public PetTransferredToHospitalIntegrationEventHandler(IConfiguration configuration, 
            ILogger<PetTransferredToHospitalIntegrationEventHandler> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            this.configuration = configuration;
            this.logger = logger;
            this.serviceScopeFactory = serviceScopeFactory;

            client = new ServiceBusClient(configuration["ServiceBus:ConnectionString"]);
            processor = client.CreateProcessor(configuration["ServiceBus:TopicName"],
                configuration["ServiceBus:SubscriptionName"]);
            processor.ProcessMessageAsync += Processor_ProcessMessageAsync;
            processor.ProcessErrorAsync += Processor_ProcessErrorAsync;
        }

        private Task Processor_ProcessErrorAsync(ProcessErrorEventArgs args)
        {
            logger?.LogError(args.Exception.ToString());
            return Task.CompletedTask;
        }

        private async Task Processor_ProcessMessageAsync(ProcessMessageEventArgs args)
        {
            var body = args.Message.Body.ToString();
            var theEvent = JsonConvert.DeserializeObject<PetTransferredToHospitalIntegrationEvent>(body);
            await args.CompleteMessageAsync(args.Message);

            using var scope = serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
            
            var existingPatient = await dbContext.PatientsMetadata.FindAsync(theEvent.Id);
            if (existingPatient == null)
            {
                dbContext.PatientsMetadata.Add(theEvent);
                await dbContext.SaveChangesAsync();
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await processor.StartProcessingAsync(stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await processor.StopProcessingAsync(cancellationToken);
        }

    }
}
