using CCC.CAS.Workflow2Messages.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using CCC.CAS.API.Common.ServiceBus;
using CCC.CAS.Workflow2Service.Interfaces;
using CCC.CAS.API.Common.Logging;

namespace CCC.CAS.Workflow2Service.Consumers
{
    public class SaveWorkflow2Consumer : IConsumer<ISaveWorkflow2>
    {
        private readonly ILogger<SaveWorkflow2Consumer> _logger;
        private readonly IWorkflow2Repository _workflow2Repository;

        public SaveWorkflow2Consumer(ILogger<SaveWorkflow2Consumer> logger, IWorkflow2Repository Workflow2Repository)
        {
            _logger = logger;
            _workflow2Repository = Workflow2Repository;
        }

        public async Task Consume(ConsumeContext<ISaveWorkflow2> context)
        {
            if (string.IsNullOrEmpty(context?.Message.Workflow2?.Name)) { throw new ArgumentException("context.Message.Workflow2.Name should not be null"); }

            var item = await _workflow2Repository.SaveWorkflow2Async(context.GetIdentity(), context.Message.Workflow2, context.CorrelationId ).ConfigureAwait(false);


            if (item != null)
            {
                _logger.LogInformation(context.CorrelationId, "SaveWorkflow2Consumer: saved {name}", context.Message.Workflow2.Name);

                await context.RespondAsync<ISaveWorkflow2Response>(new SaveWorkflow2Response() { Workflow2 = item }).ConfigureAwait(false);
                await context.Publish<IWorkflow2Saved>(new
                {
                    item.Id,
                    item.Name,
                    context.CorrelationId
                }
                ).ConfigureAwait(false);
            }
            else
            {
                await context.RespondAsync<ISaveWorkflow2Response>(new SaveWorkflow2Response()).ConfigureAwait(false);
                _logger.LogError(context.CorrelationId, "Error saving Workflow2. Could publish SaveFailed event");
            }
        }
    }
}
