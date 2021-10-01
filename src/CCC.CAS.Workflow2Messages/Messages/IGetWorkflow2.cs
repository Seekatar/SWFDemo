using CCC.CAS.API.Common.ServiceBus;
using MassTransit;
using System;

namespace CCC.CAS.Workflow2Messages.Messages
{
    public interface IGetWorkflow2 : ICommandMessage
    {
        // the consumer class name will be '<queue name>Consumer', e.g. GetWorkflow2Consumer
        public static Uri QueueUri => new Uri("queue:GetWorkflow2?durable=true");

        string Id { get; }
    }

    public class GetWorkflow2 : IGetWorkflow2
    {
        public Guid CorrelationId { get; } = NewId.NextGuid();
        public Uri MessageQueueUri => IGetWorkflow2.QueueUri;

        public string Id { get; set; } = "";
    }
}
