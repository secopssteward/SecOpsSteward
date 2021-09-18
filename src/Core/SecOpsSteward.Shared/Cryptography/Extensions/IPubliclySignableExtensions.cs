using System;
using System.Security.Cryptography;
using System.Text;

namespace SecOpsSteward.Shared.Cryptography.Extensions
{
    /// <summary>
    ///     Extensions for signing and validating packages with a public package index key
    /// </summary>
    public static class IPubliclySignableExtensions
    {
        /// <summary>
        ///     Verify public signature
        /// </summary>
        /// <param name="signable">Object to verify</param>
        /// <param name="publicKey">Package index public key</param>
        /// <returns><c>TRUE</c> if signature is valid</returns>
        public static bool PubliclyVerify(this IPubliclySignable signable, byte[] publicKey)
        {
            var dataHash = signable.GetSignableHash();
            var signatureTest = signable.PublicSignature.GetSignatureMetadata();
            var signatureHash = ChimeraSharedHelpers.GetHash(signatureTest);
            var digest = ChimeraSharedHelpers.GetHash(dataHash, signatureHash);

            using (RSA rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(Encoding.ASCII.GetString(publicKey));
                return rsa.VerifyHash(digest, signable.PublicSignature.SignatureBytes, HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
            }
        }

        /// <summary>
        ///     Sign with a package index signing key
        /// </summary>
        /// <param name="signable">Object to sign</param>
        /// <param name="signingEntity">Name of signing entity</param>
        /// <param name="privateKey">Private key data</param>
        public static void PubliclySign(this IPubliclySignable signable, string signingEntity, byte[] privateKey)
        {
            var newSignature = new PublicSignature
            {
                Timestamp = DateTimeOffset.Now,
                Signer = signingEntity
            };

            var dataHash = signable.GetSignableHash();
            var signatureHash = ChimeraSharedHelpers.GetHash(newSignature);
            var digest = ChimeraSharedHelpers.GetHash(dataHash, signatureHash);

            using (RSA rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(Encoding.ASCII.GetString(privateKey));
                newSignature.SignatureBytes = rsa.SignHash(digest, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }

            signable.PublicSignature = newSignature;
        }
    }
}