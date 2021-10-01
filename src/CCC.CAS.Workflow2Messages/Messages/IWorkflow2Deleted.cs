using MassTransit;
using System;

namespace CCC.CAS.Workflow2Messages.Messages
{
    public interface IWorkflow2Deleted : CorrelatedBy<Guid>
    {
        Guid Id { get; }
    }

    public class Workflow2Deleted : IWorkflow2Deleted
    {
        public Guid Id { get; set; } = Guid.Empty;

        public Guid CorrelationId { get; set; } = NewId.NextGuid();
    }
}
