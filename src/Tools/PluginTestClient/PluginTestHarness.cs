using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SecOpsSteward.Plugins;
using SecOpsSteward.Plugins.Azure;
using SecOpsSteward.Shared;
using SecOpsSteward.Shared.Packaging;
using SecOpsSteward.Shared.Roles;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PluginTest.Client
{
    public class PluginTestHarness
    {
        public string ManagerUser => _managerServiceProvider.Item2;
        public string User => _userServiceProvider.Item2;

        private readonly Tuple<ServiceProvider, string> _managerServiceProvider;
        private readonly Tuple<ServiceProvider, string> _userServiceProvider;

        public PluginTestHarness(string tenantId, string subscriptionId)
        {
            _managerServiceProvider = TestAuthenticationHelper.GetTestHarnessServiceProvider(tenantId, subscriptionId, true).Result;
            _userServiceProvider = TestAuthenticationHelper.GetTestHarnessServiceProvider(tenantId, subscriptionId, false).Result;
        }

        public async Task RunTest(
            string folder,
            Guid pluginIdGuid,
            Dictionary<string, string> configuration)
        {
            var pluginId = new ChimeraPackageIdentifier(pluginIdGuid);

            string path = "";
            // get test package
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Press ENTER to start test from new plugin build\n");
            Console.ReadLine();
            Console.WriteLine("*** Recreating ChimeraContainer from built code at " + folder);
            using (var pkg = ChimeraContainer.CreateFromFolder(folder))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("This container's source code hash is " +
                    BitConverter.ToString(pkg.GetPackageContentHash())
                                .Replace("-", "").Substring(0, 8));
                path = pkg.ExtractedPath;
                var plugin = pkg.Wrapper.GetPlugin(pluginId.Id);

                // get identically-configured plugin instances for manager and user
                var configSerialized = JsonConvert.SerializeObject(configuration);
                var userInstance = plugin.Emit(_userServiceProvider.Item1, configSerialized);

                var mgrRoleAssignment = _managerServiceProvider.Item1.GetRequiredService<IRoleAssignmentService>();

                var userOid = _userServiceProvider.Item2;

                // run GRANT as manager
                await RunAzureOperation("GRANT", async () => await mgrRoleAssignment.AssignPluginRole(userInstance, userInstance.RbacRequirements, userOid));

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("... sleeping for 10 seconds to allow RBAC to propagate ...");
                await Task.Delay(10000);

                // run EXECUTE as user
                await RunAzureOperation("EXECUTE", async () => await userInstance.Execute(new PluginOutputStructure(CommonResultCodes.Success)));

                // run REVOKE as manager
                await RunAzureOperation("REVOKE", async () => await mgrRoleAssignment.UnassignPluginRole(userInstance, userInstance.RbacRequirements, userOid));

                var roleName = "SoSRole - " + userInstance.GetDescriptiveName();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\n\nTest completed. Press ENTER to delete the role '" + roleName + "'.\n\n");
                Console.ReadLine();

                await RunAzureOperation("DROP ROLE [" + roleName + "]", async () =>
                {
                    var az = _managerServiceProvider.Item1.GetRequiredService<AzureCurrentCredentialFactory>().GetCredential().GetAzure();

                    var roleId = await az.AccessManagement.RoleDefinitions.GetByScopeAndRoleNameAsync("/subscriptions/" + az.SubscriptionId, roleName);
                    await az.AccessManagement.RoleDefinitions.Inner.DeleteWithHttpMessagesAsync("/subscriptions/" + az.SubscriptionId, roleId.Name);
                });
            }
        }

        private static async Task RunAzureOperation(string name, Func<Task> operation)
        {
            try
            {
                // attempt EXECUTE as user
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("*** Running " + name + new string('-', 80 - name.Length - 12));
                Console.ForegroundColor = ConsoleColor.White;
                await operation();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("!!! Success !!!");
            }

            catch (Microsoft.Azure.Management.AppService.Fluent.Models.DefaultErrorResponseException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("!!! Azure Failure !!! -> " + ex.Message);
                Console.WriteLine("!!! Error Message Body: " + JsonConvert.SerializeObject(ex.Response.Content));
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("!!! General Failure: " + ex.ToString());
            }
        }
    }
}
