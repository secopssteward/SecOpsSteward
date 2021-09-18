using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecOpsSteward.Data;
using SecOpsSteward.Integrations.Azure;
using SecOpsSteward.Plugins.Azure;
using SecOpsSteward.Shared;
using SecOpsSteward.Shared.NonceTracking;

namespace SecOpsSteward.Coordinator.AzureFunctions
{
    public class Program
    {
        internal static ChimeraAgentIdentifier AgentId =>
            new(Guid.Parse(Environment.GetEnvironmentVariable("AgentId")));

        internal static string AgentDescription => Environment.GetEnvironmentVariable("AgentName");

        internal static bool IgnoreUserPermissionRestrictions =>
            Environment.GetEnvironmentVariable("IgnoreUserPermissionRestrictions") != null
                ? bool.Parse(Environment.GetEnvironmentVariable("IgnoreUserPermissionRestrictions"))
                : false;

        internal static IServiceProvider Services { get; private set; }
        protected static IConfiguration Configuration { get; set; }

        public static void Main()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(services =>
                {
                    var config = new ChimeraServiceConfigurator(new Dictionary<string, string>
                    {
                        {"SubscriptionId", Configuration.GetSection("Chimera")["SubscriptionId"]},
                        {"VaultName", Configuration.GetSection("Chimera")["VaultName"]},
                        {"PackageRepoAccount", Configuration.GetSection("Chimera")["PackageRepoAccount"]},
                        {"PackageRepoContainer", Configuration.GetSection("Chimera")["PackageRepoContainer"]},
                        {"ServiceBusNamespace", Configuration.GetSection("Chimera")["ServiceBusNamespace"]}
                    });
                    services.AddSingleton(config);

                    services.RegisterCurrentCredentialFactory(
                        Configuration.GetSection("AzureAd")["TenantId"],
                        config["SubscriptionId"], true);

                    services.AddSingleton<INonceTrackingService, LocalFileNonceTrackingService>();

                    // All service-driven integrations
                    services.AddAzurePlatformIntegrations();

                    // Chimera core
                    services.AddChimera(config);

                    // Message processing (as user)
                    services.AddScoped<WorkflowMessageProcessorService>();
                })
                .Build();

            host.Run();
        }
    }
}