using CCC.CAS.API.Common.ServiceBus;
using MassTransit;
using System;

namespace CCC.CAS.Workflow2Messages.Messages
{
    public interface IDeleteWorkflow2 : ICommandMessage
    {
        // the consumer class name will be '<queue name>Consumer', e.g. DeleteWorkflow2Consumer
        public static Uri QueueUri => new Uri("queue:DeleteWorkflow2?durable=true");

        string Id { get; }
    }

    public class DeleteWorkflow2 : IDeleteWorkflow2
    {
        public Guid CorrelationId { get; } = NewId.NextGuid();
        public Uri MessageQueueUri => IDeleteWorkflow2.QueueUri;

        public string Id { get; set; } = "";
    }
}
