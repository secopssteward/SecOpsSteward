using System;

namespace SecOpsSteward.Shared.NonceTracking
{
    public class TrackedNonce
    {
        public const int NONCE_LENGTH = 64;
        public static TimeSpan NONCE_VALID_TIME = TimeSpan.FromDays(7);

        public ChimeraEntityIdentifier AgentId { get; set; }
        public Guid RequestId { get; set; }
        public string ExpectedNonce { get; set; }
        public DateTimeOffset Expiry { get; set; }

        public TrackedNonce() { }
        public TrackedNonce(ChimeraEntityIdentifier agentId, Guid requestId)
        {
            AgentId = agentId;
            RequestId = requestId;
            Regenerate();
        }

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
            else return nonce.Equals(ExpectedNonce);
        }
    }
}
