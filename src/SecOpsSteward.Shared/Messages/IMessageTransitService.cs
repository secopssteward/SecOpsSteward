using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SecOpsSteward.Shared.Cryptography;

namespace SecOpsSteward.Shared.Messages
{
    /// <summary>
    ///     Actions which can be taken on a received message
    /// </summary>
    [Flags]
    public enum MessageActions
    {
        Unknown = 0,
        Complete = 1,
        Abandon = 2,
        Defer = 4
    }

    /// <summary>
    ///     Handles message transit to and from Agents
    /// </summary>
    public interface IMessageTransitService
    {
        /// <summary>
        ///     Queue encrypted signed envelope to the proper location based on envelope's Receiver ID
        /// </summary>
        /// <param name="envelope">Envelope to enqueue</param>
        /// <returns></returns>
        Task Enqueue(EncryptedMessageEnvelope envelope);

        /// <summary>
        ///     Dequeue an envelope from an entity's message queue
        /// </summary>
        /// <param name="entityId">Entity ID</param>
        /// <param name="action">Action to take with message</param>
        /// <returns></returns>
        Task Dequeue(ChimeraEntityIdentifier entityId, Func<EncryptedMessageEnvelope, Task<MessageActions>> action);
    }

    public static class MessageTransitServiceExtensions
    {
        public static Task Enqueue(this IMessageTransitService transitService, EncryptedObject encrypted)
        {
            var envelope = new EncryptedMessageEnvelope(encrypted);
            return transitService.Enqueue(envelope);
        }

        public static async Task DequeuePoll(this IMessageTransitService transitService, ChimeraEntityIdentifier entity,
            Func<EncryptedMessageEnvelope, CancellationTokenSource, Task<MessageActions>> action, TimeSpan timeout)
        {
            var cancellation = new CancellationTokenSource();
            var endTime = DateTimeOffset.UtcNow + timeout;
            while (DateTimeOffset.UtcNow < endTime && !cancellation.IsCancellationRequested)
            {
                await transitService.Dequeue(entity, async envelope =>
                {
                    if (envelope == null) return MessageActions.Unknown;
                    return await action(envelope, cancellation);
                });
                await Task.Delay(500); // 0.5s delay between polls
            }
        }

        public static async Task DequeuePoll(this IMessageTransitService transitService, ChimeraEntityIdentifier entity,
            Func<EncryptedMessageEnvelope, CancellationTokenSource, Task<MessageActions>> action, TimeSpan timeout,
            params Guid[] envelopeIds)
        {
            var envelopesFound = new List<Guid>();
            await transitService.DequeuePoll(entity, async (envelope, cancellation) =>
            {
                if (!envelopeIds.Contains(envelope.ThreadId))
                    return MessageActions.Defer;
                return await action(envelope, cancellation);
            }, timeout);
        }

        public static async Task DequeuePoll(this IMessageTransitService transitService, ChimeraEntityIdentifier entity,
            Func<EncryptedMessageEnvelope, CancellationTokenSource, MessageActions> action, TimeSpan timeout,
            params Guid[] envelopeIds)
        {
            await transitService.DequeuePoll(entity, (envelope, cancellation) =>
            {
                return Task.FromResult(action(envelope, cancellation)); // Action->Func wrapper
            }, timeout);
        }
    }
}