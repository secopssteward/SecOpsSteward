using System;

namespace SecOpsSteward.Shared.NonceTracking
{
    public partial class TrackedNonceCollection
    {
        private class TrackedNonce
        {
            public const int NONCE_LENGTH = 64;
            public static readonly TimeSpan NONCE_VALID_TIME = TimeSpan.FromDays(7);

            public TrackedNonce()
            {
            }

            public TrackedNonce(ChimeraEntityIdentifier agentId, Guid requestId)
            {
                AgentId = agentId;
                RequestId = requestId;
                Regenerate();
            }

            public ChimeraEntityIdentifier AgentId { get; }
            public Guid RequestId { get; }
            public string ExpectedNonce { get; set; }
            public DateTimeOffset Expiry { get; set; }

            public bool IsExpired => Expiry < DateTimeOffset.UtcNow;

            public void Regenerate()
            {
                ExpectedNonce = ChimeraSharedHelpers.RandomString(NONCE_LENGTH);
                Expiry = DateTimeOffset.UtcNow + NONCE_VALID_TIME;
            }

            public bool IsValid(string nonce)
            {
                if (string.IsNullOrEmpty(ExpectedNonce) &&
                    string.IsNullOrEmpty(nonce))
                    return true;
                return nonce.Equals(ExpectedNonce);
            }
        }
    }
}