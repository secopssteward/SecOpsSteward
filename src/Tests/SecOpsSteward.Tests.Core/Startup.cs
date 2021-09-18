using Microsoft.Extensions.DependencyInjection;
using SecOpsSteward.Shared;
using SecOpsSteward.Shared.Configuration;
using SecOpsSteward.Shared.Cryptography;
using SecOpsSteward.Shared.Cryptography.Extensions;
using SecOpsSteward.Shared.NonceTracking;
using SecOpsSteward.Shared.Packaging;

namespace SecOpsSteward.Tests.Core
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ChimeraServiceConfigurator>();
            services.AddTransient<IConfigurationProvider, DummyConfigurationService>();
            services.AddTransient<INonceTrackingService, LocalFileNonceTrackingService>();
            services.AddTransient<ICryptographicService, DummyCryptographicService>();
            services.AddTransient<IPackageRepository, DummyStorageRepository>();
        }
    }
}