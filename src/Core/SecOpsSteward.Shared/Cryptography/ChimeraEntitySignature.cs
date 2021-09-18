using System;

namespace SecOpsSteward.Shared.Cryptography
{
    /// <summary>
    ///     A single signature on some signable entity
    /// </summary>
    public class ChimeraEntitySignature
    {
        /// <summary>
        ///     Signature content
        /// </summary>
        public byte[] SignatureBytes { get; set; }

        /// <summary>
        ///     Signer who created the signature
        /// </summary>
        public ChimeraEntityIdentifier Signer { get; set; }

        /// <summary>
        ///     Some display text to identify the signer
        /// </summary>
        public string SignerDisplay { get; set; }

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
        public ChimeraEntitySignature GetSignatureMetadata()
        {
            return new ChimeraEntitySignature()
            {
                Signer = Signer,
                SignerDisplay = SignerDisplay,
                Timestamp = Timestamp
            };
        }

        public override string ToString()
        {
            if (IsSigned)
                return $"Signed by {Signer} ({SignerDisplay}) on {Timestamp}. ({SignatureBytes.Length} bytes)";
            if (Signer != null)
                return $"Signature not applied. Prospective signer is {Signer}.";
            return "Signature not applied.";
        }
    }
}