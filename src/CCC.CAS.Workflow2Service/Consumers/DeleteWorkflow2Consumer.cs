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
    public class DeleteWorkflow2Consumer : IConsumer<IDeleteWorkflow2>
    {
        private readonly ILogger<DeleteWorkflow2Consumer> _logger;
        private readonly IWorkflow2Repository _workflow2Repository;

        public DeleteWorkflow2Consumer(ILogger<DeleteWorkflow2Consumer> logger, IWorkflow2Repository Workflow2Repository)
        {
            _logger = logger;
            _workflow2Repository = Workflow2Repository;
        }

        public async Task Consume(ConsumeContext<IDeleteWorkflow2> context)
        {
            if (string.IsNullOrEmpty(context?.Message.Id)) { throw new ArgumentException("context.Message.Id should not be null"); }

            await _workflow2Repository.DeleteWorkflow2Async(context.GetIdentity(), context.Message.Id ).ConfigureAwait(false);
            await context.Publish<IWorkflow2Deleted>(new
            {
                context.Message.Id,
                context.CorrelationId
            }
            ).ConfigureAwait(false);

            _logger.LogInformation(context.CorrelationId, "DeleteWorkflow2Consumer: deleted {id}", context.Message.Id);
        }
    }
}
