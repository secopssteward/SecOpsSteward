using System.Threading.Tasks;

namespace SecOpsSteward.Shared.Services
{
    /// <summary>
    ///     Executed when an Agent is created or removed
    /// </summary>
    public interface IHasAgentCreationActions : IChimeraIntegratedService
    {
        /// <summary>
        ///     Executed when an Agent is created
        /// </summary>
        /// <param name="agent">New agent</param>
        /// <returns></returns>
        Task OnAgentCreated(ChimeraAgentIdentifier agent);

        /// <summary>
        ///     Executed when an Agent is removed
        /// </summary>
        /// <param name="agent">Removed agent</param>
        /// <returns></returns>
        Task OnAgentRemoved(ChimeraAgentIdentifier agent);
    }
}