namespace SecOpsSteward.Shared.Services
{
    /// <summary>
    ///     Denotes a Chimera integrated service which has tasks to execute
    /// </summary>
    public interface IChimeraIntegratedService
    {
        /// <summary>
        ///     Priority to use when organizing all system services (lower is earlier execution)
        /// </summary>
        int ServicePriority { get; }
    }
}