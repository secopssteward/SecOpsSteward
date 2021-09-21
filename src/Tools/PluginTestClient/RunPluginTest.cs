using SecOpsSteward.Shared;
using SecOpsSteward.Shared.Packaging;
using SecOpsSteward.Shared.Packaging.Metadata;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace PluginTestClient
{
    public class RunPluginTestArgs : CommandSettings
    {
        [Description("Folder Path")]
        [CommandOption("-p|--path")]
        public string Path { get; set; }

        [Description("JSON Configuration")]
        [CommandOption("-c|--configuration")]
        public string Configuration { get; set; }

        [Description("Tenant ID")]
        [CommandOption("-t|--tenant")]
        public string TenantId { get; set; }

        [Description("Susbcription ID")]
        [CommandOption("-s|--subscription")]
        public string SubscriptionId { get; set; }
    }
    public class RunPluginTest : AsyncCommand<RunPluginTestArgs>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, RunPluginTestArgs settings)
        {
            if (settings.Path == null)
            {
                var prompt = new TextPrompt<string>("Plugin Path?");
                settings.Path = await prompt.ShowAsync(AnsiConsole.Console, CancellationToken.None);
            }

            if (settings.Configuration == null)
            {
                var prompt = new TextPrompt<string>("JSON Configuration?");
                settings.Configuration = await prompt.ShowAsync(AnsiConsole.Console, CancellationToken.None);
            }

            if (settings.SubscriptionId == null)
            {
                var prompt = new TextPrompt<string>("Subscription ID?");
                settings.SubscriptionId = await prompt.ShowAsync(AnsiConsole.Console, CancellationToken.None);
            }

            if (settings.TenantId == null)
            {
                var prompt = new TextPrompt<string>("Tenant ID?");
                settings.TenantId = await prompt.ShowAsync(AnsiConsole.Console, CancellationToken.None);
            }

            // ---

            PluginMetadata pluginMetadata;
            using (var pkg = ChimeraContainer.CreateFromFolder(settings.Path))
            {
                var metadata = pkg.GetMetadata();
                var s = new SelectionPrompt<PluginMetadata>();
                s.Title = "Select Plugin in Package";
                s.AddChoices(metadata.PluginsMetadata);
                pluginMetadata = await s.ShowAsync(AnsiConsole.Console, CancellationToken.None);
            }

            // ---

            AnsiConsole.MarkupLine("[green]The following parameters will be used for this test:[/]");
            AnsiConsole.MarkupLine("[white]" +
                $"Folder:\t\t{settings.Path}\n" +
                $"Tenant ID:\t{settings.TenantId}\n" +
                $"Subscription:\t{settings.SubscriptionId}\n" +
                $"Plugin:\t\t{pluginMetadata.Name} ({pluginMetadata.PluginId.PluginId})\n\n[/]");

            var configuration = ChimeraSharedHelpers.GetFromSerializedString<Dictionary<string, string>>(settings.Configuration);
            if (!configuration.ContainsKey("SubscriptionId"))
                configuration["SubscriptionId"] = settings.SubscriptionId;
            if (!configuration.ContainsKey("TenantId"))
                configuration["TenantId"] = settings.TenantId;

            var tbl = new Table();
            tbl.Title = new TableTitle("Configuration");
            tbl.AddColumns("Key", "Value");
            foreach (var item in configuration)
                tbl.AddRow(item.Key, item.Value);
            AnsiConsole.Render(tbl);

            // ---

            AnsiConsole.Render(new Rule("[red]Low-priv User Authentication[/]"));
            
            Console.ForegroundColor = ConsoleColor.Red;
            AnsiConsole.MarkupLine("\n\n[red]#####          A USER PROMPT WILL APPEAR MOMENTARILY!        #####[/]");
            AnsiConsole.MarkupLine("[red]#####  Ensure you log in with a dummy or unprivileged user!  #####[/]");
            AnsiConsole.MarkupLine("[white]" +
                "[red]###[/] This test application will perform the grant/revoke process \n" +
                "[red]###[/] around execution. That means the corresponding plugin role will be \n" +
                "[red]###[/] created and destroyed as well, allowing for testing various RBAC \n" +
                "[red]###[/] settings quickly.\n\n[/]");

            AnsiConsole.Render(new Rule("[red]Low-priv User Authentication[/]"));

            // this does the auth for us
            var testHarness = new PluginTestHarness(settings.TenantId, settings.SubscriptionId);

            AnsiConsole.MarkupLine("\n\n[white]Test Harness Ready, logged in as:[/]");
            AnsiConsole.MarkupLine($"[green]Manager:\t[/][yellow]{testHarness.ManagerUser}[/]");
            AnsiConsole.MarkupLine($"[green]User:\t\t[/][yellow]{testHarness.User}[/]\n\n\n");

            while (true)
            {
                try
                {
                    await testHarness.RunTest(settings.Path, pluginMetadata.PluginId, configuration);
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                }
            }
        }
    }
}