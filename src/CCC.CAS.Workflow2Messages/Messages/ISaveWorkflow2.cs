using CCC.CAS.API.Common.ServiceBus;
using CCC.CAS.Workflow2Messages.Models;
using MassTransit;
using System;

namespace CCC.CAS.Workflow2Messages.Messages
{
    public interface ISaveWorkflow2 : ICommandMessage
    {
        Workflow2? Workflow2 { get; }

        // the consumer class name will be '<queue name>Consumer', e.g. SaveWorkflow2Consumer
        static Uri QueueUri = new Uri("queue:SaveWorkflow2?durable=true");
    }

    public class SaveWorkflow2 : ISaveWorkflow2
    {
        public Guid CorrelationId { get; } = NewId.NextGuid();
        public Uri MessageQueueUri => ISaveWorkflow2.QueueUri;

        public Workflow2? Workflow2 { get; set; }
    }
}
