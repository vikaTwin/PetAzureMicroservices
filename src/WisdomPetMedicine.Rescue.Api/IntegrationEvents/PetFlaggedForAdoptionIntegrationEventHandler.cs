using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using WisdomPetMedicine.Rescue.Api.Infrastructure;
using WisdomPetMedicine.Rescue.Domain.Entities;
using WisdomPetMedicine.Rescue.Domain.Repositories;
using WisdomPetMedicine.Rescue.Domain.ValueObjects;

namespace WisdomPetMedicine.Rescue.Api.IntegrationEvents
{
    public class PetFlaggedForAdoptionIntegrationEventHandler : BackgroundService
    {
        private readonly ServiceBusClient client;
        private readonly ServiceBusProcessor processor;
        private readonly ILogger<PetFlaggedForAdoptionIntegrationEventHandler> logger;
        private readonly IServiceScopeFactory serviceScopeFactory;

        public PetFlaggedForAdoptionIntegrationEventHandler(IConfiguration configuration, 
            ILogger<PetFlaggedForAdoptionIntegrationEventHandler> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
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
            var theEvent = JsonConvert.DeserializeObject<PetFlaggedForAdoptionIntegrationEvents>(body);
            await args.CompleteMessageAsync(args.Message);

            using var scope = serviceScopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IRescueRepository>();
            var dbContext = scope.ServiceProvider.GetRequiredService<RescueDbContext>();
            dbContext.RescuedAnimalsMetadata.Add(theEvent);

            var rescuedAnimal = new RescuedAnimal(RescuedAnimalId.Create(theEvent.Id));
            await repo.AddRescuedAnimalAsync(rescuedAnimal);
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
