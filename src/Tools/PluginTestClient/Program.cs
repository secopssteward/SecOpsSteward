using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PluginTest.Client
{
    class Program
    {
        private static void DrawLogo()
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(@"
    @@@@@@@@  @@@@@@@@  @@@@@@@@     
    @@@  @@@  @@@  @@@  @@@  @@@     
    @@@  @@@  @@@  @@@  @@@  @@@     
    @@@       @@@  @@@  @@@         SecOps Steward
    @@@@@@@@  @@@  @@@  @@@@@@@@    Plugin Test Harness  
         @@@  @@@  @@@       @@@
    @@@  @@@  @@@  @@@  @@@  @@@
    @@@@ @@@  @@@  @@@  @@@ @@@@
      @@@@@@  @@@  @@@  @@@@@@@
          @@  @@@  @@@  @@
              @@@  @@@
                 @@
");
        }
        static async Task Main(string[] args)
        {
            // List of ops:
            // https://docs.microsoft.com/en-us/azure/role-based-access-control/resource-provider-operations

            // TODO: Console args

            string folder = @"C:\dev\SecOpsSteward\src\SecOpsSteward.Plugins.Azure.AppServices\bin\Debug\net5.0";
            string tenantId = "185bb12a-cb44-4d85-81fe-297ba9e6b4d1";
            string subscriptionId = "67c708de-facd-4332-a6da-c35b24f8db3a";

            var pluginIdStr = "76b7d0ce-1174-49ee-8b18-6ba0399d8ef6";
            var configuration = new Dictionary<string, string>()
            {
                { "Name", "testName" },
                { "Value", "testValue" },
                { "ResourceGroup", "chi-test" },
                { "FunctionAppName", "chitest" },
                { "SubscriptionId", subscriptionId },
            };

            // ---

            DrawLogo();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\nYou will be prompted to log in momentarily.");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("##### Ensure you log in with a dummy or unprivileged user! #####");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("This test application will perform the grant/revoke process ");
            Console.WriteLine("around execution. That means the corresponding plugin role will be ");
            Console.WriteLine("created and destroyed as well, allowing for testing various RBAC ");
            Console.WriteLine("settings quickly.\n\n");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("The following parameters will be used for this test:");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Folder:\t\t" + folder);
            Console.WriteLine("Tenant ID:\t" + tenantId);
            Console.WriteLine("Subscription:\t" + subscriptionId);
            Console.WriteLine("Plugin ID:\t" + pluginIdStr);
            Console.WriteLine("Configuration:");
            foreach (var item in configuration)
            {
                Console.WriteLine("   " + item.Key + "  =>  " + item.Value);
            }

            // this does the auth for us
            var testHarness = new PluginTestHarness(tenantId, subscriptionId);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Test Harness Ready, logged in as: ");

            Console.Write("Manager:\t");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(testHarness.ManagerUser);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("User:\t\t");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(testHarness.User + "\n\n\n");


            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                try
                {
                    await testHarness.RunTest(folder, Guid.Parse(pluginIdStr), configuration);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }
    }
}
