using CCC.CAS.API.Common.Models;
using CCC.CAS.Workflow2Messages.Messages;
using CCC.CAS.Workflow2Messages.Models;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shouldly;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CCC.CAS.Workflow2Service.Consumers;
using CCC.CAS.Workflow2Service.Interfaces;

namespace UnitTest.ServiceBus
{
    public class Tests
    {
        private InMemoryTestHarness? _harness;
        private Mock<IWorkflow2Repository>? _mockRepo;
        private ConsumerTestHarness<SaveWorkflow2Consumer>? _consumer;
        private ConsumerTestHarness<GetWorkflow2Consumer>? _getConsumer;

        [SetUp]
        public async Task Setup()
        {
            _harness = new InMemoryTestHarness();
            _mockRepo = new Mock<IWorkflow2Repository>();
            _mockRepo.Setup(o => o.SaveWorkflow2Async(It.IsAny<CallerIdentity>(), It.IsAny<Workflow2>(), It.IsAny<Guid?>())).Returns(Task.FromResult<Workflow2?>(new Workflow2 { Name = "test", Id = NewId.NextGuid().ToString() }));
            _mockRepo.Setup(o => o.GetWorkflow2Async(It.IsAny<CallerIdentity>(), It.IsAny<string>())).Returns(Task.FromResult<Workflow2?>(new Workflow2 { Name = NewId.NextGuid().ToString() }));

            _consumer = _harness.Consumer(() => new SaveWorkflow2Consumer(new Mock<ILogger<SaveWorkflow2Consumer>>().Object, _mockRepo.Object));
            _getConsumer = _harness.Consumer(() => new GetWorkflow2Consumer(new Mock<ILogger<GetWorkflow2Consumer>>().Object, _mockRepo.Object));
            _harness.TestTimeout = TimeSpan.FromSeconds(2);
            await _harness.Start();
        }

        [TearDown]
        public async Task Teardown()
        {
            await _harness!.Stop();
        }

        [Test]
        public async Task TestSendSaveCommand()
        {
            _harness!.TestTimeout = TimeSpan.FromSeconds(2);
            await _harness.InputQueueSendEndpoint.Send(new SaveWorkflow2 { Workflow2 = new Workflow2 { Name = "test" } });
            var consumed = _harness.Consumed.Select<ISaveWorkflow2>().FirstOrDefault();
            _harness.TestTimeout = TimeSpan.FromSeconds(.1);
            if (await _harness.Published.Any(o => o.MessageType == typeof(Fault)))
            {
                var faults = _harness.Published.Select<Fault>().ToList();
                foreach (var f in faults)
                {
                    Debug.WriteLine(JsonSerializer.Serialize(f.Context.Message, new JsonSerializerOptions { WriteIndented = true }));
                }
            }
            (await _harness.Published.Any(o => o.MessageType == typeof(Fault))).ShouldBeFalse();
            consumed.ShouldNotBeNull();

            // did the endpoint consume the message
            (await _harness.Consumed.Any(o => o.MessageType == typeof(ISaveWorkflow2))).ShouldBeTrue();
            _harness.Consumed.Select<ISaveWorkflow2>().Any().ShouldBeTrue();

            // did the actual consumer consume the message
            _consumer!.Consumed.Select<ISaveWorkflow2>().Any().ShouldBeTrue();

            // did the consumer publish the event
            _harness.Published.Select<IWorkflow2Saved>().Any().ShouldBeTrue();
            (await _harness.Published.Any(o => o.MessageType == typeof(IWorkflow2Saved))).ShouldBeTrue();

            // did we call save?
            _mockRepo!.Verify(o => o.SaveWorkflow2Async(It.IsAny<CallerIdentity>(), It.IsAny<Workflow2>(), It.IsAny<Guid?>()), Times.Once());

        }

        [Test]
        public async Task TestSendSaveCommandFault()
        {
            await _harness!.InputQueueSendEndpoint.Send(new SaveWorkflow2()); // no name will throw

            // did the endpoint consume the message
            _harness.Consumed.Select<ISaveWorkflow2>().Any().ShouldBeTrue();

            // did the actual consumer consume the message
            _consumer!.Consumed.Select<ISaveWorkflow2>().Any().ShouldBeTrue();

            // did the consumer publish the event
            // hangs since didn't publish? _harness.Published.Select<Workflow2Saved>().Any().ShouldBeTrue();
            // but this works.
            (await _harness.Published.Any(o => o.MessageType == typeof(IWorkflow2Saved))).ShouldBeFalse();

            // did the consumer throw
            _harness.Published.Select<Fault<ISaveWorkflow2>>().Any().ShouldBeTrue();
        }

        [Test]
        public async Task TestGetWorkflow2()
        {
            _harness!.TestTimeout = TimeSpan.FromSeconds(2);
            await _harness.InputQueueSendEndpoint.Send<IGetWorkflow2>(new
            {
                Id = "AE000001",
                CorrelationId = NewId.NextGuid()
            }
            );

            _getConsumer!.Consumed.Select<IGetWorkflow2>().Any().ShouldBeTrue();
            var called = _harness.Consumed.Select<IGetWorkflow2>().Any();
            called.ShouldBeTrue();
            _mockRepo!.Verify(o => o.GetWorkflow2Async(It.IsAny<CallerIdentity>(), It.IsAny<string>()), Times.Once());

            (await _harness.Published.Any(o => o.MessageType == typeof(IGetWorkflow2Response))).ShouldBeTrue();
        }
    }
}
