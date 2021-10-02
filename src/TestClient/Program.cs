using CCC.CAS.API.Common.Models;
using CCC.CAS.Workflow2Messages.Messages;
using CCC.CAS.Workflow2Messages.Models;
using System;
using System.Threading.Tasks;
using static System.Console;
using IntegrationTest;

namespace TestMessages
{
    /// <summary>
    /// test program to fire commands, events, and listen for events
    /// </summary>
    class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="showBusConfig">if set to <c>true</c> [show bus configuration].</param>
        /// <param name="clientCode">defaults to geico</param>
        /// <param name="profileId">defaults to a geico profileId</param>
        static async Task Main(bool showBusConfig = false, string clientCode = "geico", int profileId = 176453)
        {
            var sender = Sender.Initialize(showBusConfig);
            if (sender == null) { throw new Exception("Didn't get sender!"); }

            var identity = new CallerIdentity { ClientCode = clientCode, ClientProfileId = profileId, Username = "testClient" };

            WriteLine("This is a test client for the message-driven Workflow2 application.");
            await Task.Run(async () =>
            {
                var key = string.Empty;
                while (key != "Q")
                {
                    try
                    {
                        switch (key)
                        {
                            case "1":
                                {
                                    var gotIt = await sender.GetEcho(new GetEcho { Name = "1" }, identity);
                                    if (gotIt != null)
                                    {
                                        WriteLine($"Waited for echo! Got response '{gotIt.Parm.Message}'");
                                    }
                                    else
                                    {
                                        WriteLine("Waiting for GetEchoResponse published message timed out!");
                                    }
                                    break;
                                }
                            case "2":
                                {
                                    var gotIt = await sender.GetEcho(new GetEcho { Name = "2" }, identity);
                                    if (gotIt != null)
                                    {
                                        WriteLine($"Waited for echo! Got response '{gotIt.Parm.Message}'");
                                    }
                                    else
                                    {
                                        WriteLine("Waiting for GetEchoResponse published message timed out!");
                                    }
                                    break;
                                }
                        }
                    }
                    catch (Exception e)
                    {
                        WriteLine(e);
                    }
                    WriteLine("Press a key: TestScenario(1) TestScenario(2) (Q)uit");
                    key = ReadKey(true).KeyChar.ToString().ToUpperInvariant();
                    WriteLine($"Processing {key}");
                }
            });
        }
    }
}
