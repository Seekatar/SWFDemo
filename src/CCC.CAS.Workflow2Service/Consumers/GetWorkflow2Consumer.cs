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
    public class GetWorkflow2Consumer : IConsumer<IGetWorkflow2>
    {
        private readonly ILogger<GetWorkflow2Consumer> _logger;
        private readonly IWorkflow2Repository _workflow2Repository;

        public GetWorkflow2Consumer(ILogger<GetWorkflow2Consumer> logger, IWorkflow2Repository Workflow2Repository)
        {
            _logger = logger;
            _workflow2Repository = Workflow2Repository;
        }

        public async Task Consume(ConsumeContext<IGetWorkflow2> context)
        {
            if (context == null) { throw new ArgumentNullException(nameof(context)); }

            var result = await _workflow2Repository.GetWorkflow2Async(context.GetIdentity(), context.Message.Id).ConfigureAwait(false);

            _logger.LogInformation(context.CorrelationId, "GetWorkflow2Consumer: got {name}", result?.Name ?? "<not found>");

            await context.RespondAsync<IGetWorkflow2Response>(new GetWorkflow2Response { Workflow2 = result })
                .ConfigureAwait(false);
        }
    }
}
