using System.Threading.Tasks;

namespace SecOpsSteward.Shared.Services
{
    /// <summary>
    ///     Executed when a user is enrolled or removed from the system
    /// </summary>
    public interface IHasUserEnrollmentActions : IChimeraIntegratedService
    {
        /// <summary>
        ///     Executed when a user is enrolled to the Chimera system
        /// </summary>
        /// <param name="user">User enrolled</param>
        /// <param name="userOid">User Object ID</param>
        /// <returns></returns>
        Task OnUserEnrolled(ChimeraUserIdentifier user);

        /// <summary>
        ///     Executed when a user is removed from the Chimera system
        /// </summary>
        /// <param name="user">User removed</param>
        /// <returns></returns>
        Task OnUserRemoved(ChimeraUserIdentifier user);
    }
}