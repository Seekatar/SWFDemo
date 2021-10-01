using CCC.CAS.API.Common.Models;
using CCC.CAS.Workflow2Messages.Messages;
using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using CCC.CAS.Workflow2Messages.Models;

namespace IntegrationTest
{
    public class Tests
    {
        const string clientCode = "TestClient";
        const int profileId = 9999;
        private Sender? _sender;
        private string _workflow2Id = "123456";
        private string _name = "Fred";
        private readonly CallerIdentity _identity = new() { ClientCode = clientCode, ClientProfileId = profileId, Username = "testClient" };

        public Tests()
        {
            // not in Setup() since that gets called +1 times
            _sender = Sender.Initialize();

            // user _sender.Configuration to get test values that are environment-specific
        }

        [Test]
        public async Task GetWorkflow2Test()
        {
            var newWorkflow2 = await _sender!.SaveWorkflow2(new SaveWorkflow2 { Workflow2 = new Workflow2 { Name = _name, Description = "From test" } }, _identity).ConfigureAwait(false);
            newWorkflow2.ShouldNotBeNull();
            var workflow2 = (await _sender!.GetWorkflow2(new GetWorkflow2 { Id = newWorkflow2!.Id }, _identity).ConfigureAwait(false));
            workflow2.ShouldNotBeNull();
            workflow2!.Id.ShouldBe(newWorkflow2.Id);
            workflow2.Name.ShouldBe(_name);
        }

        [Test]
        public async Task GetEchoTest()
        {
            var echo = (await _sender!.GetEcho(new GetEcho { Name = _name }, _identity).ConfigureAwait(false))?.Parm;
            echo.ShouldNotBeNull();
            echo!.Message.ShouldNotBeEmpty();
            echo!.Name.ShouldBe(_name);
        }

        [Test]
        public async Task SaveWorkflow2Test()
        {
            var workflow2 = await _sender!.SaveWorkflow2(new SaveWorkflow2 { Workflow2 = new Workflow2 { Name = _name, Description = "From test" } }, _identity).ConfigureAwait(false);
            workflow2.ShouldNotBeNull();
            workflow2!.Id.ShouldNotBeNull();
            workflow2.Name.ShouldBe(_name);

            IntegrationTestWorkflow2SavedConsumer.WaitForMessage(TimeSpan.FromSeconds(3)).ShouldBeTrue();
        }

        [Test]
        public async Task DeleteWorkflow2Test()
        {
            await _sender!.DeleteWorkflow2(new DeleteWorkflow2 { Id = _workflow2Id }, _identity).ConfigureAwait(false);

            IntegrationTestWorkflow2DeletedConsumer.WaitForMessage(TimeSpan.FromSeconds(3)).ShouldBeTrue();
        }
    }
}