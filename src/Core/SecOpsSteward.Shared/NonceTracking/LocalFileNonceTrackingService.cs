using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SecOpsSteward.Shared.NonceTracking
{
    public class LocalFileNonceTrackingService : INonceTrackingService
    {
        private const string NONCE_LOCKFILE = "sos-nonces.lock";
        private const string NONCE_FILE = "sos-nonces.dat";

        public static string NoncePath = Path.GetTempPath();

        public LocalFileNonceTrackingService(string path = null)
        {
            NoncePath = path;
            if (path == null) NoncePath = Path.GetTempPath();
        }

        public async Task<string> ValidateNonce(ChimeraEntityIdentifier agentId, Guid requestId, string nonce)
        {
            lock (NoncePath)
            {
                var expiry = DateTime.UtcNow.AddMinutes(1);
                while (File.Exists(Path.Combine(NoncePath, NONCE_LOCKFILE)))
                {
                    if (DateTime.UtcNow > expiry)
                        throw new Exception(
                            "Unable to lock nonce file! There may be too much traffic for local nonces.");
                    Thread.Sleep(100);
                }

                // lock file
                File.WriteAllBytes(Path.Combine(NoncePath, NONCE_LOCKFILE), new byte[] {0xff});

                // get nonce file
                var requestNonceFile = Path.Combine(NoncePath, NONCE_FILE);
                if (!File.Exists(requestNonceFile))
                    File.WriteAllText(requestNonceFile,
                        ChimeraSharedHelpers.SerializeToString(new TrackedNonceCollection()));
                var trackedNonces =
                    ChimeraSharedHelpers.GetFromSerializedString<TrackedNonceCollection>(
                        File.ReadAllText(requestNonceFile));

                // run cleanup of nonces, then regenerate
                trackedNonces.CleanupExpired();
                var newNonce = trackedNonces.ValidateRegenerate(agentId, requestId, nonce);
                File.WriteAllText(requestNonceFile, ChimeraSharedHelpers.SerializeToString(trackedNonces));

                // unlock file
                File.Delete(Path.Combine(NoncePath, NONCE_LOCKFILE));

                return newNonce;
            }
        }
    }
}