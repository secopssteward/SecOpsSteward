namespace SecOpsSteward.Shared.Cryptography
{
    /// <summary>
    ///     Some object in the system which is encrypted and directed to some entity
    /// </summary>
    public class EncryptedObject
    {
        /// <summary>
        ///     ID of Recipient receiving this envelope
        /// </summary>
        public ChimeraEntityIdentifier RecipientId { get; set; }

        /// <summary>
        ///     Wrapped key used to decrypt the message
        /// </summary>
        public byte[] WrappedKey { get; set; }

        /// <summary>
        ///     Encrypted signed message content
        /// </summary>
        public byte[] EncryptedEnvelope { get; set; }

        /// <summary>
        ///     Expected message type when extracted from Envelope
        /// </summary>
        public string MessageType { get; set; }
    }
}