using System;
using System.Collections.Generic;
using System.Linq;

namespace SecOpsSteward.Shared.NonceTracking
{
    public class TrackedNonceCollection
    {
        public List<TrackedNonce> Nonces { get; set; } = new List<TrackedNonce>();
        public TrackedNonce GetById(Guid requestId) =>
            Nonces.FirstOrDefault(n => n.RequestId == requestId);

        public string ValidateRegenerate(ChimeraEntityIdentifier agentId, Guid requestId, string nonce)
        {
            var target = GetById(requestId);
            if (target == null)
            {
                // never before seen, create new
                var newNonce = new TrackedNonce(agentId, requestId);
                Nonces.Add(newNonce);
                return newNonce.ExpectedNonce;
            }
            else if (target.IsValid(nonce))
            {
                // seen, nonce valid
                target.Regenerate();
                return target.ExpectedNonce;
            }
            else
                // seen, invalid nonce
                return string.Empty;
        }

        public void CleanupExpired() => Nonces.RemoveAll(n => n.IsExpired);
    }
}
