using System;
using System.Collections.Generic;
using System.Linq;

namespace SecOpsSteward.Shared.NonceTracking
{
    public partial class TrackedNonceCollection
    {
        private List<TrackedNonce> Nonces { get; } = new();

        public string ValidateRegenerate(ChimeraEntityIdentifier agentId, Guid requestId, string nonce)
        {
            var target = Nonces.FirstOrDefault(n => n.RequestId == requestId);
            if (target == null)
            {
                // never before seen, create new
                var newNonce = new TrackedNonce(agentId, requestId);
                Nonces.Add(newNonce);
                return newNonce.ExpectedNonce;
            }

            if (target.IsValid(nonce))
            {
                // seen, nonce valid
                target.Regenerate();
                return target.ExpectedNonce;
            }

            return string.Empty;
        }

        public void CleanupExpired()
        {
            Nonces.RemoveAll(n => n.IsExpired);
        }
    }
}