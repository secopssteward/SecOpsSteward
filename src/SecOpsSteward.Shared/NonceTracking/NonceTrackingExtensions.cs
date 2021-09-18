using Microsoft.Extensions.DependencyInjection;

namespace SecOpsSteward.Shared.NonceTracking
{
    public static class NonceTrackingExtensions
    {
        public static void AddNoNonceTracking(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<INonceTrackingService, NoNonceTrackingService>();
        }

        public static void AddTempFileNonceTracking(this IServiceCollection serviceCollection, string path = null)
        {
            serviceCollection.AddSingleton<INonceTrackingService, LocalFileNonceTrackingService>(s =>
                new LocalFileNonceTrackingService(path));
        }
    }
}