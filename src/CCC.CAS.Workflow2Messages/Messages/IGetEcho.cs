using CCC.CAS.API.Common.ServiceBus;
using MassTransit;
using System;

namespace CCC.CAS.Workflow2Messages.Messages
{
    public interface IGetEcho : ICommandMessage
    {
        // the consumer class name will be '<queue name>Consumer', e.g. GetEchoConsumer
        public static Uri QueueUri => new Uri("queue:GetEcho?durable=true");

        string Name { get; }
    }

    public class GetEcho : IGetEcho
    {
        public Guid CorrelationId { get; } = NewId.NextGuid();
        public Uri MessageQueueUri => IGetEcho.QueueUri;

        public string Name { get; set; } = "";
    }
}
