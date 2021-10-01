using CCC.CAS.Workflow2Messages.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using CCC.CAS.API.Common.ServiceBus;
using CCC.CAS.Workflow2Service.Interfaces;
using System.Web;
using CCC.CAS.API.Common.Logging;

namespace CCC.CAS.Workflow2Service.Consumers
{
    public class GetEchoConsumer : IConsumer<IGetEcho>
    {
        private readonly ILogger<GetEchoConsumer> _logger;
        private readonly IActivityService _workflow2Repository;

        public GetEchoConsumer(ILogger<GetEchoConsumer> logger, IActivityService activityService)
        {
            _logger = logger;
            _workflow2Repository = activityService;
        }

        public async Task Consume(ConsumeContext<IGetEcho> context)
        {
            if (context == null) { throw new ArgumentNullException(nameof(context)); }

            _logger.LogInformation(context.CorrelationId, "StartWorkflowConsumer: got {name}", context.Message.Name);

            // name shouldn't be anything too wacky since we pass it in the url's path
            await _workflow2Repository.StartWorkflow(context.Message.Name == "1" ? 1 : 2).ConfigureAwait(false);

            await context.RespondAsync<IGetEchoResponse>(new GetEchoResponse { })
                .ConfigureAwait(false);
        }

    }
}
