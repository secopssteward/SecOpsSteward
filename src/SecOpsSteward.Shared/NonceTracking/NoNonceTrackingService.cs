using System;
using System.Threading.Tasks;

namespace SecOpsSteward.Shared.NonceTracking
{
    public class NoNonceTrackingService : INonceTrackingService
    {
        public Task<string> ValidateNonce(ChimeraEntityIdentifier agentId, Guid requestId, string nonce)
        {
            return Task.FromResult(ChimeraSharedHelpers.RandomString(12));
        }
    }
}