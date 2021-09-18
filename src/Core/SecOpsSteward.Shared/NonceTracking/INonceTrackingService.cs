using System;
using System.Threading.Tasks;

namespace SecOpsSteward.Shared.NonceTracking
{
    /// <summary>
    ///     Tracks nonces to prevent replay attacks on messages
    /// </summary>
    public interface INonceTrackingService
    {
        /// <summary>
        ///     Validates a nonce
        /// </summary>
        /// <param name="nonce">Nonce to validate</param>
        /// <param name="agentId">Agent ID</param>
        /// <param name="requestId">Request ID</param>
        /// <returns></returns>
        Task<string> ValidateNonce(ChimeraEntityIdentifier agentId, Guid requestId, string nonce);
    }
}