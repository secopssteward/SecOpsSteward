using System.Threading.Tasks;

namespace SecOpsSteward.Shared.Cryptography.Extensions
{
    /// <summary>
    ///     Service providing cryptographic operations
    /// </summary>
    public interface ICryptographicService
    {
        /// <summary>
        ///     Sign a digest of data as a given entity
        /// </summary>
        /// <param name="signer">Entity signing the digest</param>
        /// <param name="digest">Digest of data to sign</param>
        /// <returns>Signature bytes</returns>
        Task<byte[]> Sign(ChimeraEntityIdentifier signer, byte[] digest);

        /// <summary>
        ///     Verify a given signature against the digest it signs
        /// </summary>
        /// <param name="signer">Entity signing the digest</param>
        /// <param name="signature">Given signature to test</param>
        /// <param name="digest">Digest of data being signed</param>
        /// <returns><c>TRUE</c> if the signature is valid</returns>
        Task<bool> Verify(ChimeraEntityIdentifier signer, byte[] signature, byte[] digest);

        /// <summary>
        ///     Wrap a symmetric encryption key with an Entity's asymmetric key
        /// </summary>
        /// <param name="wrappingKey">Entity wrapping the key</param>
        /// <param name="keyToWrap">Key being wrapped</param>
        /// <returns>Wrapped key</returns>
        Task<byte[]> WrapKey(ChimeraEntityIdentifier wrappingKey, byte[] keyToWrap);

        /// <summary>
        ///     Unwrap a symmetric encryption key with an Entity's asymmetric key
        /// </summary>
        /// <param name="wrappingKey">Entity who wrapped the key</param>
        /// <param name="keyToUnwrap">Key being unwrapped</param>
        /// <returns>Unwrapped key</returns>
        Task<byte[]> UnwrapKey(ChimeraEntityIdentifier wrappingKey, byte[] keyToUnwrap);

        /// <summary>
        ///     Encrypt data with an asymmetric key
        /// </summary>
        /// <param name="key">Entity which can decrypt the data</param>
        /// <param name="data">Data to encrypt</param>
        /// <returns>Encrypted data</returns>
        Task<byte[]> Encrypt(ChimeraEntityIdentifier key, byte[] data);

        /// <summary>
        ///     Decrypt data with an asymmetric key
        /// </summary>
        /// <param name="key">Entity which can decrypt the data</param>
        /// <param name="ciphertext">Ciphertext to decrypt</param>
        /// <returns>Decrypted data</returns>
        Task<byte[]> Decrypt(ChimeraEntityIdentifier key, byte[] ciphertext);
    }
}