using CCC.CAS.Workflow2Messages.Models;
using MassTransit;
using System;

namespace CCC.CAS.Workflow2Messages.Messages
{
    public interface ISaveWorkflow2Response : CorrelatedBy<Guid>
    {
        Workflow2? Workflow2 { get;  }
    }

    public class SaveWorkflow2Response : ISaveWorkflow2Response
    {
        public Guid CorrelationId { get; } = NewId.NextGuid();
        public Workflow2? Workflow2 { get; set;  }
    }
}
