using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SecOpsSteward.Shared.Cryptography.Extensions
{
    /// <summary>
    ///     Denotes an object in the system which can be encrypted
    /// </summary>
    public interface IEncryptable
    {
    }

    /// <summary>
    ///     Extensions to encrypt arbitrary objects
    /// </summary>
    public static class IEncryptableObjectExtensions
    {
        /// <summary>
        ///     Encrypt some object with a given key
        /// </summary>
        /// <typeparam name="TEncryptable">Type of object to encrypt</typeparam>
        /// <param name="encryptable">Object instance</param>
        /// <param name="service">Cryptographic provider service</param>
        /// <param name="key">Key to encrypt with</param>
        /// <returns></returns>
        public static async Task<EncryptedObject> Encrypt<TEncryptable>(this TEncryptable encryptable,
            ICryptographicService service, ChimeraEntityIdentifier key)
            where TEncryptable : IEncryptable
        {
            var objectBytes = ChimeraSharedHelpers.SerializeToBytes(encryptable);
            var newKey = GetAes().Key;
            var wrappedKey = await service.WrapKey(key, newKey);
            var encryptedBytes = EncryptAes(newKey, objectBytes);

            return new EncryptedObject
            {
                RecipientId = key,
                MessageType = encryptable.GetType().Name,
                EncryptedEnvelope = encryptedBytes,
                WrappedKey = wrappedKey
            };
        }

        /// <summary>
        ///     Decrypt some object based on the recipient's key
        /// </summary>
        /// <typeparam name="TObject">Type of object expected after decryption</typeparam>
        /// <param name="encrypted">Encrypted object instance</param>
        /// <param name="service">Cryptographic provider service</param>
        /// <returns></returns>
        public static async Task<TObject> Decrypt<TObject>(this EncryptedObject encrypted,
            ICryptographicService service)
            where TObject : IEncryptable
        {
            var unwrappedKey = await service.UnwrapKey(encrypted.RecipientId, encrypted.WrappedKey);
            var decryptedBytes = DecryptAes(unwrappedKey, encrypted.EncryptedEnvelope);
            if (typeof(TObject).Name != encrypted.MessageType) throw new TypeAccessException("Invalid message type");
            return ChimeraSharedHelpers.GetFromSerializedBytes<TObject>(decryptedBytes);
        }

        #region Symmetric Cryptography (Wrapped)

        private static AesManaged GetAes()
        {
            return new()
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            };
        }

        private static byte[] EncryptAes(byte[] key, byte[] plainText)
        {
            using (var aes = GetAes())
            {
                aes.Key = key;
                // IV size is AES block size in bytes (128 bits/8)
                // IV is appended to ciphertext based on presumptively known IV block size
                var encryptor = aes.CreateEncryptor();
                var result = PerformCryptography(encryptor, plainText);

                var iv = aes.IV;
                // add space for IV
                Array.Resize(ref result, result.Length + iv.Length);
                // copy IV to end of result
                Array.Copy(aes.IV, 0, result, result.Length - iv.Length, iv.Length);
                return result;
            }
        }

        private static byte[] DecryptAes(byte[] key, byte[] cipherText)
        {
            using (var aes = GetAes())
            {
                aes.Key = key;

                // iv is embedded in ciphertext as last (blocksize/8) bytes
                // AES fixed block size is 128bits/8 = 16
                var iv = new byte[aes.BlockSize / 8];
                Array.Copy(cipherText, cipherText.Length - iv.Length, iv, 0, iv.Length);
                Array.Resize(ref cipherText, cipherText.Length - iv.Length);
                aes.IV = iv;

                var decryptor = aes.CreateDecryptor();
                return PerformCryptography(decryptor, cipherText);
            }
        }

        private static byte[] PerformCryptography(ICryptoTransform cryptoTransform, byte[] data)
        {
            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Write);
            cryptoStream.Write(data, 0, data.Length);
            if (!cryptoStream.HasFlushedFinalBlock)
                cryptoStream.FlushFinalBlock();
            cryptoStream.Flush();
            return memoryStream.ToArray();
        }

        #endregion
    }
}