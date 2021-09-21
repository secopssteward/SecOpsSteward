using Spectre.Console;
using Spectre.Console.Cli;
using System.Threading.Tasks;

namespace PluginTestClient
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            // List of ops:
            // https://docs.microsoft.com/en-us/azure/role-based-access-control/resource-provider-operations

            DrawLogo();

            var app = new CommandApp<RunPluginTest>();
            return await app.RunAsync(args);
        }

        private static void DrawLogo()
        {
            AnsiConsole.MarkupLine(@"[blue]
    @@@@@@@@  @@@@@@@@  @@@@@@@@     
    @@@  @@@  @@@  @@@  @@@  @@@     
    @@@  @@@  @@@  @@@  @@@  @@@     
    @@@       @@@  @@@  @@@         [white]SecOps Steward[/]
    @@@@@@@@  @@@  @@@  @@@@@@@@    [green]Plugin Test Harness[/]
         @@@  @@@  @@@       @@@
    @@@  @@@  @@@  @@@  @@@  @@@
    @@@@ @@@  @@@  @@@  @@@ @@@@
      @@@@@@  @@@  @@@  @@@@@@@
          @@  @@@  @@@  @@
              @@@  @@@
                 @@
[/]");
        }
    }
}