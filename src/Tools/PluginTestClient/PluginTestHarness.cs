using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SecOpsSteward.Plugins;
using SecOpsSteward.Plugins.Azure;
using SecOpsSteward.Shared;
using SecOpsSteward.Shared.Packaging;
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

        public async Task RunTest(
            string folder,
            ChimeraPackageIdentifier pluginId,
            Dictionary<string, string> configuration)
        {
            var path = "";
            // get test package
            AnsiConsole.MarkupLine("[cyan]Press ENTER to start test from new plugin build[/]\n");
            Console.ReadLine();

            AnsiConsole.MarkupLine($"[cyan]*** Recreating ChimeraContainer from built code at {folder}[/]");
            using (var pkg = ChimeraContainer.CreateFromFolder(folder))
            {
                AnsiConsole.MarkupLine("[yellow]This container's source code hash is " +
                    BitConverter.ToString(pkg.GetPackageContentHash()).Replace("-", "").Substring(0, 8) + "[/]");

                path = pkg.ExtractedPath;
                var plugin = pkg.Wrapper.GetPlugin(pluginId.Id);

                // get identically-configured plugin instances for manager and user
                var configSerialized = JsonConvert.SerializeObject(configuration);
                var userInstance = plugin.Emit(_userServiceProvider.Item1, configSerialized);

                var mgrRoleAssignment = _managerServiceProvider.Item1.GetRequiredService<IRoleAssignmentService>();

                var userOid = _userServiceProvider.Item2;

                // run GRANT as manager
                await RunAzureOperation("GRANT",
                    async () => await mgrRoleAssignment.AssignPluginRole(userInstance, userInstance.RbacRequirements,
                        userOid));

                AnsiConsole.MarkupLine("[cyan]... sleeping for 10 seconds to allow RBAC to propagate ...[/]");
                await Task.Delay(10000);

                // run EXECUTE as user
                await RunAzureOperation("EXECUTE",
                    async () => await userInstance.Execute(new PluginOutputStructure(CommonResultCodes.Success)));

                // run REVOKE as manager
                await RunAzureOperation("REVOKE",
                    async () => await mgrRoleAssignment.UnassignPluginRole(userInstance, userInstance.RbacRequirements,
                        userOid));

                var roleName = "SoSRole - " + userInstance.GetDescriptiveName();

                AnsiConsole.MarkupLine($"[cyan]\n\nTest completed. Press ENTER to delete the role [green]{roleName}[/].[/]\n\n");
                Console.ReadLine();

                await RunAzureOperation("DROP ROLE [" + roleName + "]", async () =>
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
        }

        private static async Task RunAzureOperation(string name, Func<Task> operation)
        {
            try
            {
                AnsiConsole.MarkupLine($"[cyan]*** Running {name + new string('-', 80 - name.Length - 12)}[/]");
                AnsiConsole.Foreground = Color.White;
                await operation();
                AnsiConsole.ResetColors();
                AnsiConsole.MarkupLine("[green]!!! Success !!![/]");
            }

            catch (DefaultErrorResponseException ex)
            {
                AnsiConsole.MarkupLine("[red]!!! Azure Failure !!![/]");
                AnsiConsole.WriteException(ex);
                AnsiConsole.MarkupLine("[red]!!! Error Message Body: {JsonConvert.SerializeObject(ex.Response.Content)}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]!!! General Failure !!![/]");
                AnsiConsole.WriteException(ex);
            }
        }
    }
}