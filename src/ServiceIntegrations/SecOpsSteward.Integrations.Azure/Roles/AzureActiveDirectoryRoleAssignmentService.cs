using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Graph.RBAC.Fluent;
using Microsoft.Azure.Management.Graph.RBAC.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SecOpsSteward.Plugins;
using SecOpsSteward.Plugins.Azure;
using SecOpsSteward.Shared;
using SecOpsSteward.Shared.Roles;

namespace SecOpsSteward.Integrations.Azure.Roles
{
    public class AzureActiveDirectoryRoleAssignmentService : AzurePlatformIntegrationBase, IRoleAssignmentService
    {
        private readonly string ROLE_DESCRIPTION_PREFIX = "Role to ";

        private readonly string ROLE_NAME_PREFIX = "SoSRole - ";

        public AzureActiveDirectoryRoleAssignmentService(
            ILogger<AzureActiveDirectoryRoleAssignmentService> logger,
            ChimeraServiceConfigurator configurator,
            AzureCurrentCredentialFactory platformFactory) : base(logger, configurator, platformFactory)
        {
        }

        public async Task<bool> ApplyScopedRoleToIdentity(ChimeraEntityIdentifier Entity, AssignableRole Role,
            string Scope)
        {
            return await CreateRoleAssignment(GetOid(Entity), Role, Scope);
        }

        public async Task<bool> RemoveScopedRoleFromIdentity(ChimeraEntityIdentifier Entity, AssignableRole Role,
            string Scope)
        {
            return await DropRoleAssignment(GetOid(Entity), Role, Scope);
        }

        public async Task<TokenOwner> ResolveUsername(string username)
        {
            var user = await _platformFactory.GetCredential().GetAzure().AccessManagement.ActiveDirectoryUsers
                .GetByNameAsync(username);
            return BuildRecord(user, false);
        }

        // ---

        public async Task<bool> HasAssignedPluginRole<TPlugin>(TPlugin plugin,
            IEnumerable<PluginRbacRequirements> requirements, string identity)
            where TPlugin : IPlugin
        {
            var azure = _platformFactory.GetCredential().GetAzure();

            var result = await Task.WhenAll(requirements.Select(async requirement =>
            {
                var azReqs = requirement as AzurePluginRbacRequirements;

                return (await _platformFactory.GetCredential().GetAzure()
                        .AccessManagement.RoleAssignments
                        .ListByScopeAsync(azReqs.Scope))
                    .Any(r => r.PrincipalId == identity && r.RoleDefinitionId == PluginRoleDefinitionId(plugin));
            }));

            return !result.Any(r => !r);
        }

        public async Task AssignPluginRole<TPlugin>(TPlugin plugin, IEnumerable<PluginRbacRequirements> requirements,
            string identity)
            where TPlugin : IPlugin
        {
            var azure = _platformFactory.GetCredential().GetAzure();

            await Task.WhenAll(requirements.Select(async requirement =>
            {
                var azReqs = requirements as AzurePluginRbacRequirements;

                // create if not exist
                await CreateRole(azure, plugin, azReqs.Actions, azReqs.DataActions);

                // assign
                await _platformFactory.GetCredential().GetAzure()
                    .AccessManagement.RoleAssignments
                    .Define(SdkContext.RandomGuid())
                    .ForObjectId(identity)
                    .WithRoleDefinition(PluginRoleDefinitionId(plugin))
                    .WithScope(azReqs.Scope)
                    .CreateAsync();
            }));
        }

        public async Task UnassignPluginRole<TPlugin>(TPlugin plugin, IEnumerable<PluginRbacRequirements> requirements,
            string identity)
            where TPlugin : IPlugin
        {
            var azure = _platformFactory.GetCredential().GetAzure();

            await Task.WhenAll(requirements.Select(async requirement =>
            {
                var azReqs = requirements as AzurePluginRbacRequirements;

                var role = (await azure.AccessManagement.RoleAssignments
                        .ListByScopeAsync(azReqs.Scope))
                    .First(r => r.PrincipalId == identity && r.RoleDefinitionId == PluginRoleDefinitionId(plugin));

                await azure.AccessManagement.RoleAssignments
                    .DeleteByIdAsync(role.Id);
            }));
        }

        private TokenOwnerWithRole BuildRecord(IActiveDirectoryUser user, bool isAdmin)
        {
            var userNames = new List<string>();
            userNames.Add(user.SignInName);
            userNames.Add(user.Mail);
            userNames.Add(user.MailNickname);
            userNames.Add(user.UserPrincipalName);
            userNames.AddRange((user.Inner.AdditionalProperties["otherMails"] as JArray).Select(v => (string) v));
            userNames.AddRange(user.Inner.SignInNames.Select(n => n.Value));
            userNames = userNames.Where(u => !string.IsNullOrEmpty(u)).Distinct().ToList();

            return new TokenOwnerWithRole
            {
                UserId = new ChimeraUserIdentifier(Guid.Parse(user.Id)),
                Name = user.Name,
                IsAdmin = isAdmin,
                Email = user.MailNickname,
                Aliases = userNames
            };
        }

        private string GetOid(ChimeraEntityIdentifier entity)
        {
            if (entity is ChimeraAgentIdentifier || entity is ChimeraUserIdentifier)
                return $"{entity.Id}";
            return string.Empty;
        }

        private async Task<bool> CreateRoleAssignment(string oid, AssignableRole role, string scope)
        {
            await Task.Yield();
            try
            {
                Logger.LogTrace("Adding role {role} to OID {oid} on scope {scope}", role, Guid.Parse(oid).ShortId(),
                    scope);
                await _platformFactory.GetCredential().GetAzure().AccessManagement.RoleAssignments
                    .Define(SdkContext.RandomGuid())
                    .ForObjectId(oid)
                    .WithRoleDefinition(await GetDefinitionId(scope, role))
                    .WithScope(scope)
                    .CreateAsync();
                return true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("already exists"))
                    return true;
                Logger.LogWarning("FAILED adding role {role} to OID {oid} on scope {scope}", role,
                    Guid.Parse(oid).ShortId(), scope);
                throw;
            }
        }

        protected async Task<bool> DropRoleAssignment(string oid, AssignableRole role, string scope)
        {
            try
            {
                var roleId = await GetDefinitionId(scope, role);
                var assignments = _platformFactory.GetCredential().GetAzure().AccessManagement.RoleAssignments
                    .ListByScope(scope);
                var target = assignments.FirstOrDefault(s => s.PrincipalId == oid && s.RoleDefinitionId == roleId);
                if (target != null)
                {
                    await _platformFactory.GetCredential().GetAzure().AccessManagement.RoleAssignments
                        .DeleteByIdAsync(target.Id);
                    return true;
                }
            }
            catch (Exception)
            {
                Logger.LogWarning("FAILED dropping role {role} for OID {oid} on scope {scope}", role, oid, scope);
                throw;
            }

            return false;
        }

        private async Task<string> GetDefinitionId(string scope, AssignableRole role)
        {
            return (await _platformFactory.GetCredential().GetAzure().AccessManagement.RoleDefinitions.Inner
                .GetAsync(scope, GetRoleId(role))).Id;
        }

        private string GetRoleId(AssignableRole role)
        {
            switch (role)
            {
                case AssignableRole.CanDoAllOnVault: return "00482a5a-887f-4fb3-b363-3b7fe8e74483"; // kv admin
                case AssignableRole.CanManagePermissionsOnVault:
                    return "18d7d88d-d35e-4fb5-a5c3-7773c20a72d9"; // user access admin
                case AssignableRole.CanCreateKeys: return "14b46e9e-c2b7-41b4-b07b-48a6ebf60603";

                case AssignableRole.CanReadSecrets: return "4633458b-17de-408a-b874-0445c86b69e6";
                case AssignableRole.CanReadWriteSecrets: return "b86a8fe4-44ce-4948-aee5-eccb2c155cd7";

                case AssignableRole.CanSendToQueue: return "69a216fc-b8fb-44d8-bc22-1f3c2cd27a39";
                case AssignableRole.CanReceiveFromQueue: return "4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0";

                case AssignableRole.CanSignDecryptKey: return _configurator["SignDecryptRole"];
                case AssignableRole.CanValidateEncryptKey: return _configurator["VerifyEncryptRole"];

                case AssignableRole.CanReadPackages: return "2a2b9908-6ea1-4ae2-8e65-a410df84e7d1";
                case AssignableRole.CanReadWritePackages: return "ba92f5b4-2d11-453d-a403-e96b0029c9fe";
            }

            throw new Exception("Role not indexed");
        }


        private Guid PluginRoleDefinitionGuid(IPlugin plugin)
        {
            return CreateCombinedGuid(plugin.GenerateId(), Guid.Parse(CfgSubscriptionId));
        }

        private string PluginRoleDefinitionId(IPlugin plugin)
        {
            return
                $"/subscriptions/{CfgSubscriptionId}/providers/Microsoft.Authorization/roleDefinitions/{PluginRoleDefinitionGuid(plugin)}";
        }

        private async Task CreateRole(
            IAzure azure,
            IPlugin plugin,
            string[] actions,
            string[] dataActions)
        {
            IRoleDefinition roleDefinition = null;
            try
            {
                roleDefinition =
                    await azure.AccessManagement.RoleDefinitions.GetByIdAsync(PluginRoleDefinitionId(plugin));
            }
            catch
            {
            }

            if (roleDefinition == null)
            {
                var roleName = ROLE_NAME_PREFIX + plugin.GetDescriptiveName();
                var roleDescription = ROLE_DESCRIPTION_PREFIX + plugin.GetDescriptiveDescription();

                _ = await azure.AccessManagement.RoleDefinitions
                    .Inner.CreateOrUpdateWithHttpMessagesAsync(
                        $"/subscriptions/{CfgSubscriptionId}",
                        PluginRoleDefinitionGuid(plugin).ToString(),
                        new RoleDefinitionInner(
                            PluginRoleDefinitionId(plugin),
                            roleType: "CustomRole",
                            name: PluginRoleDefinitionGuid(plugin).ToString(),
                            roleName: roleName,
                            type: "Microsoft.Authorization/roleDefinitions",
                            description: roleDescription,
                            permissions: new List<PermissionInner>
                            {
                                new(
                                    new List<string>(actions),
                                    dataActions: new List<string>(dataActions))
                            },
                            assignableScopes: new List<string>
                            {
                                $"/subscriptions/{CfgSubscriptionId}"
                            }
                        ));
            }
        }

        private async Task DestroyRole(IAzure azure, IPlugin plugin)
        {
            _ = await azure.AccessManagement.RoleDefinitions
                .Inner.DeleteWithHttpMessagesAsync($"/subscriptions/{CfgSubscriptionId}",
                    PluginRoleDefinitionId(plugin));
        }

        private static Guid CreateCombinedGuid(Guid guid1, Guid guid2)
        {
            const int BYTECOUNT = 16;
            var destByte = new byte[BYTECOUNT];
            var guid1Byte = guid1.ToByteArray();
            var guid2Byte = guid2.ToByteArray();

            for (var i = 0; i < BYTECOUNT; i++) destByte[i] = (byte) (guid1Byte[i] ^ guid2Byte[i]);
            return new Guid(destByte);
        }
    }
}