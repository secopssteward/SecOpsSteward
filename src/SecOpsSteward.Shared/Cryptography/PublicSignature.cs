using System;

namespace SecOpsSteward.Shared.Cryptography
{
    /// <summary>
    ///     A single signature on some signable entity from a public source
    /// </summary>
    public class PublicSignature
    {
        /// <summary>
        ///     Signature content
        /// </summary>
        public byte[] SignatureBytes { get; set; }

        /// <summary>
        ///     Signer who created the signature
        /// </summary>
        public string Signer { get; set; }

        /// <summary>
        ///     When the signature was generated
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        ///     If this object represents a completed signature (not necessarily valid)
        /// </summary>
        public bool IsSigned => SignatureBytes != null && SignatureBytes.Length > 0;

        /// <summary>
        ///     Get the metadata for a signature (all but the signature itself)
        /// </summary>
        /// <returns></returns>
        public PublicSignature GetSignatureMetadata()
        {
            return new PublicSignature()
            {
                Signer = Signer,
                Timestamp = Timestamp
            };
        }

        public override string ToString()
        {
            if (IsSigned)
                return $"Publicly signed by {Signer} on {Timestamp}. ({SignatureBytes.Length} bytes)";
            if (Signer != null)
                return $"Public signature not applied. Prospective signer is {Signer}.";
            return "Public signature not applied.";
        }
    }
}