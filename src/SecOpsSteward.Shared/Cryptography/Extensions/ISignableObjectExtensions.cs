using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecOpsSteward.Shared.Cryptography.Extensions
{
    /// <summary>
    ///     Extensions to apply signatures to objects
    /// </summary>
    public static class ISignableObjectExtensions
    {
        /// <summary>
        ///     Verify the object against the included signature
        /// </summary>
        /// <param name="signable">Signable object</param>
        /// <param name="signatureService">Signature service to process verify request</param>
        /// <returns><c>TRUE</c> if the object has been unmodified since its signature, otherwise <c>FALSE</c></returns>
        public static async Task<bool> Verify(this ISignableByOne signable, ICryptographicService signatureService)
        {
            var dataHash = signable.GetSignableHash();
            var signatureTest = signable.Signature.GetSignatureMetadata();
            var signatureHash = ChimeraSharedHelpers.GetHash(signatureTest);
            var digest = ChimeraSharedHelpers.GetHash(dataHash, signatureHash);
            return await signatureService.Verify(signable.Signature.Signer, signable.Signature.SignatureBytes, digest);
        }

        /// <summary>
        ///     Verify the object against the included signatures
        /// </summary>
        /// <param name="signable">Signable object</param>
        /// <param name="signatureService">Signature service to process verify request</param>
        /// <returns><c>TRUE</c> if the object has been unmodified since its signature, otherwise <c>FALSE</c></returns>
        public static async Task<Dictionary<ChimeraEntitySignature, bool>> Verify(this ISignableByMany signable,
            ICryptographicService signatureService)
        {
            var results = new Dictionary<ChimeraEntitySignature, bool>();

            var baseHash = signable.GetSignableHash(); // data base hash (without signatures)
            var signatureHashes = new List<ChimeraEntitySignature>();
            foreach (var signature in signable.Signatures)
            {
                // don't include the signature itself in the check
                signatureHashes.Add(signature.GetSignatureMetadata());
                var hashesSoFar = new List<byte[]>();
                hashesSoFar.Add(baseHash);
                hashesSoFar.AddRange(signatureHashes.Select(h => ChimeraSharedHelpers.GetHash(h)));
                var objectDigest = ChimeraSharedHelpers.GetHash(hashesSoFar);

                // check the digest plus signatures so far
                var result = await signatureService.Verify(signature.Signer, signature.SignatureBytes, objectDigest);
                results.Add(signature, result);

                // this should never underflow, we should always have at least one signature
                signatureHashes.RemoveAt(signatureHashes.Count - 1);

                // add the full signature (incl. the signature bytes themselves) to the list
                signatureHashes.Add(signature);
            }

            return results;
        }

        /// <summary>
        ///     Sign an object
        /// </summary>
        /// <param name="signable">Signable object</param>
        /// <param name="signatureService">Signature service to process signing request</param>
        /// <param name="signingEntity">User signing the object's state</param>
        /// <param name="signatureDisplay">Display for signer's information</param>
        /// <returns></returns>
        public static async Task Sign(this ISignableByOne signable, ICryptographicService signatureService,
            ChimeraEntityIdentifier signingEntity, string signatureDisplay)
        {
            var newSignature = new ChimeraEntitySignature
            {
                Timestamp = DateTimeOffset.Now,
                Signer = signingEntity,
                SignerDisplay = signatureDisplay
            };

            var dataHash = signable.GetSignableHash();
            var signatureHash = ChimeraSharedHelpers.GetHash(newSignature);
            var digest = ChimeraSharedHelpers.GetHash(dataHash, signatureHash);
            newSignature.SignatureBytes = await signatureService.Sign(signingEntity, digest);

            signable.Signature = newSignature;
        }

        /// <summary>
        ///     Sign an object
        /// </summary>
        /// <param name="signable">Signable object</param>
        /// <param name="signatureService">Signature service to process signing request</param>
        /// <param name="signingEntity">User signing the object's state</param>
        /// <param name="signatureDisplay">Display for signer's information</param>
        /// <returns></returns>
        public static async Task Sign(this ISignableByMany signable, ICryptographicService signatureService,
            ChimeraEntityIdentifier signingEntity, string signatureDisplay)
        {
            var newSignature = new ChimeraEntitySignature
            {
                Timestamp = DateTimeOffset.Now,
                Signer = signingEntity,
                SignerDisplay = signatureDisplay
            };

            var allHashes = new List<byte[]>();
            allHashes.Add(signable.GetSignableHash()); // data
            allHashes.AddRange(signable.Signatures.Select(s => ChimeraSharedHelpers.GetHash(s))); // all signatures
            allHashes.Add(ChimeraSharedHelpers.GetHash(newSignature)); // new sig prototype
            var completeDigest = ChimeraSharedHelpers.GetHash(allHashes);

            var signature = await signatureService.Sign(signingEntity, completeDigest);
            newSignature.SignatureBytes = signature;
            signable.Signatures.Add(newSignature);
        }

        internal static byte[] GetSignableHash(this ISignable signable)
        {
            var messageSignedContent = string.Empty;
            foreach (var prop in signable.GetType().GetProperties()
                .Where(p => p.Name != nameof(ISignableByOne.Signature))
                .Where(p => p.Name != nameof(ISignableByMany.Signatures))
                .Where(p => p.Name != nameof(IPubliclySignable.PublicSignature))
                .Where(p => p.GetCustomAttributes(typeof(NotSignedAttribute), true).Length == 0))
                messageSignedContent += ChimeraSharedHelpers.SerializeToString(prop.GetValue(signable));

            return ChimeraSharedHelpers.GetHash(messageSignedContent);
        }
    }
}