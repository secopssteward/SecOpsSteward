using Microsoft.Extensions.DependencyInjection;
using SecOpsSteward.Shared.Configuration;
using SecOpsSteward.Shared.Cryptography;
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
            services.AddScoped<IConfigurationProvider, Configuration.AzureKeyVaultConfigurationService>();
            services.AddScoped<ICryptographicService, Cryptography.AzureKeyVaultCryptographicService>();
            services.AddScoped<IMessageTransitService, Messaging.ServiceBusMessageTransitService>();
            services.AddScoped<IRoleAssignmentService, Roles.AzureActiveDirectoryRoleAssignmentService>();
            services.AddScoped<IPackageRepository, Storage.WebPackageManager>();

            // Below have agent or user actions when those operations are performed
            services.AddScoped<IChimeraIntegratedService, Configuration.AzureKeyVaultConfigurationService>();
            services.AddScoped<IChimeraIntegratedService, Cryptography.AzureKeyVaultCryptographicService>();
            services.AddScoped<IChimeraIntegratedService, Messaging.ServiceBusMessageTransitService>();
            services.AddScoped<IChimeraIntegratedService, Storage.WebPackageManager>();
        }
    }
}
