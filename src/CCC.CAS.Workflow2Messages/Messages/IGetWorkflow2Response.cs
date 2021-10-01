using CCC.CAS.Workflow2Messages.Models;
using MassTransit;
using System;

namespace CCC.CAS.Workflow2Messages.Messages
{
    public interface IGetWorkflow2Response : CorrelatedBy<Guid>
    {
        Workflow2? Workflow2 { get;  }
    }

    public class GetWorkflow2Response : IGetWorkflow2Response
    {
        public Guid CorrelationId { get; } = NewId.NextGuid();
        public Workflow2? Workflow2 { get; set;  }
    }
}
