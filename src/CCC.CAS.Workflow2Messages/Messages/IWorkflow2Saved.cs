using MassTransit;
using System;

namespace CCC.CAS.Workflow2Messages.Messages
{
    public interface IWorkflow2Saved : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        string Name { get; }
    }

    public class Workflow2Saved : IWorkflow2Saved
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string Name { get; set; } = "";

        public Guid CorrelationId { get; set; } = NewId.NextGuid();
    }
}
