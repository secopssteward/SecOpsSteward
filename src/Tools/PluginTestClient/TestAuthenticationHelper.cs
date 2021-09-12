﻿using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using SecOpsSteward.Integrations.Azure.Roles;
using SecOpsSteward.Plugins.Azure;
using SecOpsSteward.Shared;
using SecOpsSteward.Shared.Roles;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace PluginTest.Client
{
    public static class TestAuthenticationHelper
    {
        public static async Task<Tuple<ServiceProvider, string>> GetTestHarnessServiceProvider(string tenantId, string subscriptionId, bool useDefault = false)
        {
            DefaultAzureCredential credential;
            if (useDefault)
                credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions()
                {
                    VisualStudioCodeTenantId = tenantId,
                    VisualStudioTenantId = tenantId,
                    InteractiveBrowserTenantId = tenantId,
                    SharedTokenCacheTenantId = tenantId
                });
            else
                credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions()
                {
                    ExcludeAzureCliCredential = true,
                    ExcludeEnvironmentCredential = true,
                    ExcludeAzurePowerShellCredential = true,
                    ExcludeInteractiveBrowserCredential = false,
                    InteractiveBrowserTenantId = tenantId,
                    ExcludeManagedIdentityCredential = true,
                    ExcludeSharedTokenCacheCredential = true,
                    ExcludeVisualStudioCodeCredential = true,
                    ExcludeVisualStudioCredential = true
                });

            var sc = new ServiceCollection();
            sc.AddScoped<ChimeraServiceConfigurator>(c => new ChimeraServiceConfigurator()); // not needed to be populated for this
            sc.AddScoped<AzureCurrentCredentialFactory>(s => new AzureCurrentCredentialFactory(s, tenantId, subscriptionId, false));
            sc.AddScoped<IRoleAssignmentService, AzureActiveDirectoryRoleAssignmentService>();
            var services = sc.BuildServiceProvider();
            services.GetRequiredService<AzureCurrentCredentialFactory>().RegisterManualCredentialHandle(credential, subscriptionId);
            _ = services.GetRequiredService<AzureCurrentCredentialFactory>().GetCredential().GetAzure(); // warmup

            return Tuple.Create(services, await GetOid(credential));
        }

        private static async Task<string> GetOid(TokenCredential credential)
        {
            var armToken = (await credential.GetTokenAsync(new TokenRequestContext(scopes: new[] { "https://management.azure.com/.default" }, parentRequestId: null), default)).Token;
            var jwt = new JwtSecurityToken(armToken);
            return jwt.Payload["oid"].ToString();
        }
    }
}
