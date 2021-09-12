using Microsoft.Extensions.DependencyInjection;
using SecOpsSteward.Shared.Configuration;
using SecOpsSteward.Shared.Cryptography;
using SecOpsSteward.Shared.DiscoveryWorkflow;
using SecOpsSteward.Shared.Messages;
using SecOpsSteward.Shared.NonceTracking;
using SecOpsSteward.Shared.Packaging;
using SecOpsSteward.Shared.Roles;
using System.Threading.Tasks;

namespace SecOpsSteward.Shared
{
    public static class ChimeraExtensions
    {
        /// <summary>
        /// Add Chimera support services to the DI container
        /// </summary>
        /// <param name="serviceCollection">DI container</param>
        /// <param name="config">Chimera configuration</param>
        public static void AddChimera(this IServiceCollection serviceCollection, ChimeraServiceConfigurator config)
        {
            serviceCollection.AddSingleton<ChimeraServiceConfigurator>(config);

            serviceCollection.AddScoped<ChimeraSystemOperationsService>();
            serviceCollection.AddTransient<WorkflowProcessorFactory>();

            // handles hosting shared packages so we're not hitting the repo constantly
            serviceCollection.AddScoped<PackageActionsService>();
            serviceCollection.AddScoped<DiscoverySequencerService>();

            // add blank tripwire handler, if none are defined elsewhere
            serviceCollection.AddSingleton<SecurityTripwire>();
            serviceCollection.AddSingleton<HandleSecurityTripwire>(new HandleSecurityTripwire(async (c, wf) => await Task.Yield()));
        }

        public static void AddChimeraDummyIntegrations(this IServiceCollection serviceCollection, bool useDummyReceiver = true)
        {
            serviceCollection.AddSingleton<IConfigurationProvider, DummyConfigurationService>();
            serviceCollection.AddSingleton<ICryptographicService, DummyCryptographicService>();
            serviceCollection.AddSingleton<IMessageTransitService, DummyMessageTransitService>();
            serviceCollection.AddSingleton<INonceTrackingService, LocalFileNonceTrackingService>();
            serviceCollection.AddSingleton<IPackageRepository, DummyStorageRepository>();
            serviceCollection.AddSingleton<IRoleAssignmentService, DummyRoleAssignmentService>();

            if (useDummyReceiver)
                DummyMessageTransitService.RunMessageServiceWithVirtualReceivers = true;
        }
    }
}
