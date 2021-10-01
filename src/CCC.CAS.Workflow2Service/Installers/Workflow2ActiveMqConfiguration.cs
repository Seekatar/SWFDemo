using CCC.CAS.API.Common.ServiceBus;

namespace CCC.CAS.Workflow2Service.Installers
{
    public class Workflow2ActiveMqConfiguration : ActiveMqConfiguration
    {
        public static string SaveWorkflow2Endpoint => "queue:SaveWorkflow2?durable=true";
        public static string Workflow2SavedEndpoint => "topic:Workflow2Saved?durable=true";
    }
}
