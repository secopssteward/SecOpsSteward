using Microsoft.Extensions.Logging;

namespace SecOpsSteward.Shared.Services
{
    /// <summary>
    /// Denotes a Chimera integrated service which has tasks to execute
    /// </summary>
    public interface IChimeraIntegratedService
    {
        /// <summary>
        /// Priority to use when organizing all system services (lower is earlier execution)
        /// </summary>
        int ServicePriority { get; }
    }

    public abstract class ChimeraSupportServiceBase<T>
    {
        // TODO: I think this can be removed if the below properties are backed into the consuming classes.
        // ..... IChimeraIntegratedService already acts as a tracking mechanism for anything which hooks to the SystemOperationsService
        protected ILogger<T> Logger { get; }
        protected ChimeraServiceConfigurator Config { get; } = new ChimeraServiceConfigurator();

        protected ChimeraSupportServiceBase(
            ILogger<T> logger,
            ChimeraServiceConfigurator configurator)
        {
            Logger = logger;
            Config = configurator;
        }
    }
}
