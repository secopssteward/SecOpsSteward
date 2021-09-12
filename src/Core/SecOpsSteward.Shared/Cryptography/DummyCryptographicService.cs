using Microsoft.Extensions.Logging;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SecOpsSteward.Shared.Cryptography
{
    public class DummyCryptographicService : ICryptographicService
    {
        public static byte[] IV = { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14 };

        // TODO: Add a way to bind signing/verification to correct entities

        private readonly ILogger<DummyCryptographicService> _logger;

        public DummyCryptographicService(ILogger<DummyCryptographicService> logger) => _logger = logger;


        public async Task<byte[]> Decrypt(ChimeraEntityIdentifier key, byte[] ciphertext)
        {
            await Task.Yield();
            _logger.LogTrace($"Decrypting ciphertext for {key}");
            return Decrypt(ciphertext, key.Id.ToByteArray(), IV);
        }

        public async Task<byte[]> Encrypt(ChimeraEntityIdentifier key, byte[] data)
        {
            await Task.Yield();
            _logger.LogTrace($"Encrypting ciphertext for {key}");
            return Encrypt(data, key.Id.ToByteArray(), IV);
        }

        public async Task<byte[]> Sign(ChimeraEntityIdentifier signer, byte[] digest)
        {
            await Task.Yield();
            _logger.LogTrace($"Signing digest for {signer}");
            return signer.Id.ToByteArray();
        }

        public async Task<byte[]> UnwrapKey(ChimeraEntityIdentifier wrappingKey, byte[] keyToUnwrap)
        {
            await Task.Yield();
            _logger.LogTrace($"Unwrapping key for {wrappingKey}");
            return keyToUnwrap;
        }

        public async Task<bool> Verify(ChimeraEntityIdentifier signer, byte[] signature, byte[] digest)
        {
            await Task.Yield();
            _logger.LogTrace($"Verifying signature for {signer}");
            return true;
        }

        public async Task<byte[]> WrapKey(ChimeraEntityIdentifier wrappingKey, byte[] keyToWrap)
        {
            await Task.Yield();
            _logger.LogTrace($"Wrapping key for {wrappingKey}");
            return keyToWrap;
        }

        private byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 128;
                aes.BlockSize = 128;
                aes.Padding = PaddingMode.Zeros;

                aes.Key = key;
                aes.IV = iv;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    return PerformCryptography(data, encryptor);
                }
            }
        }

        private byte[] Decrypt(byte[] data, byte[] key, byte[] iv)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 128;
                aes.BlockSize = 128;
                aes.Padding = PaddingMode.Zeros;

                aes.Key = key;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    return PerformCryptography(data, decryptor);
                }
            }
        }

        private byte[] PerformCryptography(byte[] data, ICryptoTransform cryptoTransform)
        {
            using (var ms = new MemoryStream())
            using (var cryptoStream = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write))
            {
                cryptoStream.Write(data, 0, data.Length);
                cryptoStream.FlushFinalBlock();

                return ms.ToArray();
            }
        }
    }
}
