using CCC.CAS.Workflow2Messages.Messages;
using CCC.CAS.Workflow2Messages.Models;
using MassTransit;
using System.Threading.Tasks;
using static System.Console;
using static CCC.CAS.API.Common.ServiceBus.MessageHelpers;
using CCC.CAS.API.Common.ServiceBus;
using CCC.CAS.API.Common.Models;
using System;
using MassTransit.Definition;
using System.Threading;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using CCC.CAS.API.AspNetCommon.Extensions;
using MassTransit.ActiveMqTransport;
using System.Text.Json;
using GreenPipes;

namespace IntegrationTest
{
    /// <summary>
    /// Set the prefetch count to increase throughput
    /// </summary>
    public class TestWorkflow2SavedConsumerDefinition : ConsumerDefinition<IntegrationTestWorkflow2SavedConsumer>
    {
        public TestWorkflow2SavedConsumerDefinition()
        {
            Endpoint(e => e.PrefetchCount = 5);
        }
    }

    /// <summary>
    /// Class for consuming the broadcast saved event
    /// </summary>
    /// <remarks>must be public to autoregister by convention, and name should be unique to avoid colliding with other consumers</remarks>
    public class IntegrationTestWorkflow2SavedConsumer : IConsumer<IWorkflow2Saved>
    {
        static int i = 1;
        static readonly int limit = Math.Max(Environment.ProcessorCount * 2, 16);
        static readonly AutoResetEvent _event = new(false);

        public static bool WaitForMessage(TimeSpan timeout)
        {
            return _event.WaitOne(timeout);
        }

        public async Task Consume(ConsumeContext<IWorkflow2Saved> context)
        {
            _event.Set();
            await Out.WriteLineAsync($">>> {nameof(IntegrationTestWorkflow2SavedConsumer)} {i++} - saved: Name = {context.Message.Name} Id: {context.Message.Id}");

            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

            await Out.WriteLineAsync($"<<< {nameof(IntegrationTestWorkflow2SavedConsumer)} {--i} - saved: Name = {context.Message.Name} Id: {context.Message.Id}");
        }
    }

    /// <summary>
    /// Class for consuming the broadcast deleted event
    /// </summary>
    /// <remarks>must be public to autoregister by convention, and name should be unique to avoid colliding with other consumers</remarks>
    public class IntegrationTestWorkflow2DeletedConsumer : IConsumer<IWorkflow2Deleted>
    {
        static readonly AutoResetEvent _event = new(false);

        public static bool WaitForMessage(TimeSpan timeout)
        {
            return _event.WaitOne(timeout);
        }

        public async Task Consume(ConsumeContext<IWorkflow2Deleted> context)
        {
            _event.Set();
            await Out.WriteLineAsync($">>> {nameof(IntegrationTestWorkflow2DeletedConsumer)} - deleted");
        }
    }

    /// <summary>
    /// class to send commands and events, broken out to show DI working.
    /// </summary>
    public class Sender : IDisposable
    {
        private readonly IBusControl _busControl;
        static private IConfigurationRoot? _configuration;
        static private ServiceProvider? _serviceWorkflow2;
        static object _lock = new();
        static private Sender? _me;
        private static IBusControl? _bus;

        public Sender(IBusControl busControl)
        {
            _busControl = busControl;
        }

        public static Sender Initialize(bool showBusConfig = false)
        {
            lock (_lock)
            {
                if (_me == null)
                {
                    _configuration = new ConfigurationBuilder()
                              .AddSharedSettings()
                              .AddJsonFile("appsettings.json", true, true)
                              .AddEnvironmentVariables()
                              .Build();

                    IServiceCollection serviceCollection = new ServiceCollection();
                    _serviceWorkflow2 = ConfigureServices(_configuration, serviceCollection);
                    if (_serviceWorkflow2 == null) { throw new Exception("Didn't get service provider!"); }

                    _me = _serviceWorkflow2.GetService<Sender>();
                    if (_me == null) { throw new Exception("Didn't get sender!"); }

                    _bus = _serviceWorkflow2.GetRequiredService<IBusControl>();
                    _bus.Start();

                    if (showBusConfig)
                    {
                        var result = _bus.GetProbeResult();
                        WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
                        WriteLine();
                    }
                }
            }

            return _me;
        }

        private static ServiceProvider? ConfigureServices(IConfiguration configuration, IServiceCollection serviceCollection)
        {
            var config = configuration.GetSection("ActiveMq").Get<ActiveMqConfiguration>();
            if (config == null)
            {
                WriteLine("Didn't get ActiveMq config from JSON or env");
                return null;
            }

            serviceCollection.AddMassTransit(svcColl =>
            {
                svcColl.AddConsumers(Assembly.GetExecutingAssembly());

                svcColl.AddBus(provider => Bus.Factory.CreateUsingActiveMq(cfg =>
                {
                    cfg.Host(config.Host, h =>
                    {
                        h.Username(config.Username);
                        h.Password(config.Password);

                    });
                    cfg.Durable = true;
                    cfg.ConfigureEndpoints(provider); // convention-based registration
                }));
            });

            serviceCollection.AddSingleton<Sender>();

            serviceCollection.AddSingleton<IHostedService, BusService>();

            return serviceCollection.BuildServiceProvider();
        }

        // send a command without waiting for a response
        // see below for other calls the get a response
        public async Task<Workflow2?> SaveWorkflow2(ISaveWorkflow2 message, CallerIdentity identity)
        {
            return (await _busControl.RequestResponse<ISaveWorkflow2, ISaveWorkflow2Response>(message, identity).ConfigureAwait(false))?.Workflow2;
        }

        // Example of publishing an event, this is just a test. Usually Publish is done within a Consumer
        public async Task PublishSaved()
        {
            var sendEndpoint = await AwaitNullable(_busControl?.GetPublishSendEndpoint<IWorkflow2Saved>());
            if (sendEndpoint != null)
            {
                await sendEndpoint.Send(new Workflow2Saved
                {
                    CorrelationId = NewId.NextGuid(),
                    Id = NewId.NextGuid(),
                    Name = NewId.NextGuid().ToString()
                });
            }
            WriteLine("Published saved event");
        }

        // Example of Request/Response call where consumer sends response to this caller
        public async Task<Workflow2?> GetWorkflow2(IGetWorkflow2 getIt, CallerIdentity identity)
        {
            return (await _busControl.RequestResponse<IGetWorkflow2, IGetWorkflow2Response>(getIt, identity).ConfigureAwait(false))?.Workflow2;
        }

        // Another example of Request/Response call where consumer sends response to this caller
        public async Task<EchoResponse?> GetEcho(GetEcho getEcho, CallerIdentity identity)
        {
            return (await _busControl.RequestResponse<IGetEcho, IGetEchoResponse>(getEcho, identity).ConfigureAwait(false))?.Echo;
        }

        public async Task DeleteWorkflow2(DeleteWorkflow2 message, CallerIdentity identity)
        {
            var sendEndpoint = await AwaitNullable(_busControl?.GetSendEndpoint(IDeleteWorkflow2.QueueUri));
            if (sendEndpoint != null)
            {

                await sendEndpoint.Send(
                        message,
                        context => context.Headers.SetFromIdentity(identity)).ConfigureAwait(false);
            }
            WriteLine("Sent delete");
        }

        public void Dispose()
        {
            _bus?.Stop();
            _serviceWorkflow2?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
