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
            string? id = null;

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
                            case "E":
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
                            case "S":
                                {
                                    var workflow2 = await sender.SaveWorkflow2(new SaveWorkflow2 { Workflow2 = new Workflow2 { Name = "ABC123" } }, identity);
                                    if (workflow2 != null)
                                    {
                                        id = workflow2.Id;
                                    }
                                    break;
                                }
                            case "D":
                                {
                                    if (id == null)
                                    {
                                        WriteLine("Must save before calling delete");
                                    }
                                    else
                                    {
                                        await sender.DeleteWorkflow2(new DeleteWorkflow2 { Id = id }, identity);
                                    }
                                    break;
                                }
                            case "P":
                                {
                                    await sender.PublishSaved();
                                    break;
                                }
                            case "G":
                                {
                                    if (id == null)
                                    {
                                        WriteLine("Must save before calling get");
                                    }
                                    else
                                    {
                                        var workflow2 = await sender.GetWorkflow2(new GetWorkflow2 { Id = id }, identity);
                                        if (workflow2 != null)
                                        {
                                            WriteLine($"Got workflow2 with name '{workflow2?.Name}'");
                                        }
                                        else
                                        {
                                            WriteLine($"Didn't get workflow2");

                                        }
                                    }
                                    break;
                                }
                        }
                    }
                    catch (Exception e)
                    {
                        WriteLine(e);
                    }
                    WriteLine("Press a key: (S)aveWorkflow2 (G)etWorkflow2 (D)eleteWorkflow2 (P)ublishWorkflow2Saved (E)choTest (Q)uit");
                    key = ReadKey(true).KeyChar.ToString().ToUpperInvariant();
                    WriteLine($"Processing {key}");
                }
            });
        }
    }
}
