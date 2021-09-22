using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Rest.Azure;
using Newtonsoft.Json;
using SecOpsSteward.Plugins;
using SecOpsSteward.Plugins.Azure;
using SecOpsSteward.Shared;
using SecOpsSteward.Shared.Packaging;
using SecOpsSteward.Shared.Packaging.Wrappers;
using SecOpsSteward.Shared.Roles;
using Spectre.Console;

namespace PluginTestClient
{
    public class PluginTestHarness
    {
        private readonly Tuple<ServiceProvider, string> _managerServiceProvider;
        private readonly Tuple<ServiceProvider, string> _userServiceProvider;

        public PluginTestHarness(string tenantId, string subscriptionId)
        {
            _managerServiceProvider = TestAuthenticationHelper
                .GetTestHarnessServiceProvider(tenantId, subscriptionId, true).Result;
            _userServiceProvider =
                TestAuthenticationHelper.GetTestHarnessServiceProvider(tenantId, subscriptionId).Result;
        }

        public string ManagerUser => _managerServiceProvider.Item2;
        public string User => _userServiceProvider.Item2;

        private async Task Grant(IPlugin userInstance)
        {
            var mgrRoleAssignment = _managerServiceProvider.Item1.GetRequiredService<IRoleAssignmentService>();
            var userOid = _userServiceProvider.Item2;
            var roleName = "SoSRole - " + userInstance.GetDescriptiveName();

            await RunAzureOperation("GRANT",
                async () =>
                {
                    try { await mgrRoleAssignment.AssignPluginRole(userInstance, userInstance.RbacRequirements, userOid); }
                    catch (CloudException e)
                    {
                        if (e.Message.Contains("assignment already exists"))
                        {
                            AnsiConsole.Markup("[yellow]Looks like the assignment is already there. Probably a broken run. Fixing...[/]");
                            await Drop(userInstance);
                            await Grant(userInstance);
                        }
                    }
                });
        }

        private async Task Execute(IPlugin userInstance)
        {
            await RunAzureOperation("EXECUTE",
                async () => await userInstance.Execute(new PluginOutputStructure(CommonResultCodes.Success)));
        }

        private async Task Revoke(IPlugin userInstance, IRoleAssignmentService mgrRoleAssignment)
        {
            var userOid = _userServiceProvider.Item2;
            await RunAzureOperation("REVOKE",
                async () => await mgrRoleAssignment.UnassignPluginRole(userInstance, userInstance.RbacRequirements,
                    userOid));
        }

        private async Task Drop(IPlugin userInstance)
        {
            var roleName = "SoSRole - " + userInstance.GetDescriptiveName();
            await RunAzureOperation("DROP ROLE " + roleName, async () =>
            {
                var az = _managerServiceProvider.Item1.GetRequiredService<AzureCurrentCredentialFactory>()
                    .GetCredential().GetAzure();

                var roleId =
                    await az.AccessManagement.RoleDefinitions.GetByScopeAndRoleNameAsync(
                        "/subscriptions/" + az.SubscriptionId, roleName);
                await az.AccessManagement.RoleDefinitions.Inner.DeleteWithHttpMessagesAsync(
                    "/subscriptions/" + az.SubscriptionId, roleId.Name);
            });
        }

        public async Task RunTest(
            string folder,
            ChimeraPackageIdentifier pluginId,
            Dictionary<string, object> configuration)
        {
            var configSerialized = JsonConvert.SerializeObject(configuration);

            // get test package
            AnsiConsole.Render(new Rule("[red]Press ENTER to start test from new plugin build[/]"));
            Console.ReadLine();

            AnsiConsole.MarkupLine($"[cyan]*** Recreating ChimeraContainer from built code at {folder}[/]");
            using (var pkg = ChimeraContainer.CreateFromFolder(folder))
            {
                AnsiConsole.MarkupLine("[yellow]This container's source code hash is " +
                    BitConverter.ToString(pkg.GetPackageContentHash()).Replace("-", "").Substring(0, 8) + "[/]");

                var path = pkg.ExtractedPath;
                var plugin = pkg.Wrapper.GetPlugin(pluginId.Id);
                var pluginInstance = plugin.Emit(_userServiceProvider.Item1, configSerialized);
                var roleName = "SoSRole - " + pluginInstance.GetDescriptiveName();

                AnsiConsole.MarkupLine("\n[green]RBAC Requirements:[/]");
                foreach (var req in pluginInstance.RbacRequirements.Cast<AzurePluginRbacRequirements>())
                {
                    AnsiConsole.MarkupLine("[yellow]* [/]" + req.Scope);
                    foreach (var item in req.Actions)
                        AnsiConsole.MarkupLine("\t[green]+ [/]" + item);
                    foreach (var item in req.DataActions)
                        AnsiConsole.MarkupLine("\t[blue]% [/]" + item);
                }
                AnsiConsole.MarkupLine("\n");

                // get identically-configured plugin instances for manager and user
                var mgrRoleAssignment = _managerServiceProvider.Item1.GetRequiredService<IRoleAssignmentService>();
                //var userOid = _userServiceProvider.Item2;

                await Grant(pluginInstance);

                AnsiConsole.Markup("[cyan]... sleeping for 10 seconds to allow RBAC to propagate [/]");
                for (var i = 1; i <= 10; i++)
                {
                    var G = (255 - (255 / i)).ToString("X2");
                    AnsiConsole.Markup($"[#00{G}00].[/] ");
                    await Task.Delay(1000);
                }
                AnsiConsole.WriteLine();

                // run EXECUTE as user
                await Execute(pluginInstance);

                // run REVOKE as manager
                await Revoke(pluginInstance, mgrRoleAssignment);

                AnsiConsole.MarkupLine($"[yellow]\n\nTest completed. Press ENTER to delete the role [green]{roleName}[/].[/]\n\n");
                Console.ReadLine();

                await Drop(pluginInstance);
            }
        }

        private static async Task RunAzureOperation(string name, Func<Task> operation)
        {
            try
            {
                AnsiConsole.Render(new Rule("[cyan]Running " + name + "[/]"));
                AnsiConsole.Foreground = Color.White;
                await operation();
                AnsiConsole.ResetColors();
                AnsiConsole.MarkupLine("\t[black on green]\t!!! Success !!!\t[/]");
            }

            catch (DefaultErrorResponseException ex)
            {
                AnsiConsole.MarkupLine("\t[black on red]\t!!! Azure Failure !!!\t[/]");
                AnsiConsole.WriteException(ex);
                AnsiConsole.MarkupLine("[red]!!! Error Message Body: {JsonConvert.SerializeObject(ex.Response.Content)}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("\t[black on red]\t!!! General Failure !!!\t[/]");
                AnsiConsole.WriteException(ex);
            }
        }
    }
}