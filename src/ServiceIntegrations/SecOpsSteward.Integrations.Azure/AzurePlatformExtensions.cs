using Microsoft.Extensions.DependencyInjection;
using SecOpsSteward.Integrations.Azure.Configuration;
using SecOpsSteward.Integrations.Azure.Cryptography;
using SecOpsSteward.Integrations.Azure.Messaging;
using SecOpsSteward.Integrations.Azure.Roles;
using SecOpsSteward.Integrations.Azure.Storage;
using SecOpsSteward.Shared.Configuration;
using SecOpsSteward.Shared.Cryptography.Extensions;
using SecOpsSteward.Shared.Messages;
using SecOpsSteward.Shared.Packaging;
using SecOpsSteward.Shared.Roles;
using SecOpsSteward.Shared.Services;

namespace SecOpsSteward.Integrations.Azure
{
    public static class AzurePlatformExtensions
    {
        public static void AddAzurePlatformIntegrations(this IServiceCollection services)
        {
            services.AddScoped<IConfigurationProvider, AzureKeyVaultConfigurationService>();
            services.AddScoped<ICryptographicService, AzureKeyVaultCryptographicService>();
            services.AddScoped<IMessageTransitService, ServiceBusMessageTransitService>();
            services.AddScoped<IRoleAssignmentService, AzureActiveDirectoryRoleAssignmentService>();
            services.AddScoped<IPackageRepository, WebPackageManager>();

            // Below have agent or user actions when those operations are performed
            services.AddScoped<IChimeraIntegratedService, AzureKeyVaultConfigurationService>();
            services.AddScoped<IChimeraIntegratedService, AzureKeyVaultCryptographicService>();
            services.AddScoped<IChimeraIntegratedService, ServiceBusMessageTransitService>();
            services.AddScoped<IChimeraIntegratedService, WebPackageManager>();
        }
    }
}