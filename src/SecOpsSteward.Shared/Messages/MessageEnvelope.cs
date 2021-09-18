using System;
using SecOpsSteward.Shared.Cryptography;

namespace SecOpsSteward.Shared.Messages
{
    /// <summary>
    ///     An envelope which contains an encrypted message
    /// </summary>
    public class EncryptedMessageEnvelope
    {
        public EncryptedMessageEnvelope()
        {
        }

        public EncryptedMessageEnvelope(Guid threadId, EncryptedObject payload) : this(payload)
        {
            ThreadId = threadId;
        }

        public EncryptedMessageEnvelope(EncryptedObject payload)
        {
            Payload = payload;
        }

        /// <summary>
        ///     Thread associated with the message
        /// </summary>
        public Guid ThreadId { get; set; } = Guid.NewGuid();

        /// <summary>
        ///     Recipient of the message
        /// </summary>
        public ChimeraEntityIdentifier Recipient => Payload.RecipientId;

        /// <summary>
        ///     Expected CLR type of the decrypted message
        /// </summary>
        public string MessageType => Payload.MessageType;

        /// <summary>
        ///     Encrypted message payload
        /// </summary>
        public EncryptedObject Payload { get; set; }

        public override string ToString()
        {
            return $"Encrypted envelope {ThreadId} addressed to {Recipient} - Expected type {MessageType}";
        }
    }
}