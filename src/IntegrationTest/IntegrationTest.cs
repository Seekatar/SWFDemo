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
        //private string _workflow2Id = "123456";
        private string _name = "Fred";
        private readonly CallerIdentity _identity = new() { ClientCode = clientCode, ClientProfileId = profileId, Username = "testClient" };

        public Tests()
        {
            // not in Setup() since that gets called +1 times
            _sender = Sender.Initialize();

            // user _sender.Configuration to get test values that are environment-specific
        }

        [Test]
        public async Task GetEchoTest()
        {
            var echo = (await _sender!.GetEcho(new GetEcho { Name = _name }, _identity).ConfigureAwait(false))?.Parm;
            echo.ShouldNotBeNull();
            echo!.Message.ShouldNotBeEmpty();
            echo!.Name.ShouldBe(_name);
        }
    }
}