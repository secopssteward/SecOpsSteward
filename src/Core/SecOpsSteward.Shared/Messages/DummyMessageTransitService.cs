using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SecOpsSteward.Shared.DiscoveryWorkflow;

namespace SecOpsSteward.Shared.Messages
{
    public class DummyMessageTransitService : IMessageTransitService
    {
        private readonly ILogger<DummyMessageTransitService> _logger;
        private readonly WorkflowProcessorFactory _workflowProcessorFactory;

        public DummyMessageTransitService(ILogger<DummyMessageTransitService> logger,
            WorkflowProcessorFactory workflowProcessorFactory)
        {
            _workflowProcessorFactory = workflowProcessorFactory;
            _logger = logger;
        }

        public static bool RunMessageServiceWithVirtualReceivers { get; set; } = false;

        public static Dictionary<ChimeraEntityIdentifier, List<EncryptedMessageEnvelope>> Queues { get; set; } = new();
        public static Dictionary<ChimeraEntityIdentifier, Task> VirtualProcessors { get; set; } = new();

        public async Task Dequeue(ChimeraEntityIdentifier entityId,
            Func<EncryptedMessageEnvelope, Task<MessageActions>> action)
        {
            if (!Queues.ContainsKey(entityId))
                Queues[entityId] = new List<EncryptedMessageEnvelope>();
            var queue = Queues[entityId];
            if (queue.Any())
            {
                var message = queue.First();
                queue.RemoveAt(0);

                await action(message);
            }
        }

        public async Task Enqueue(EncryptedMessageEnvelope envelope)
        {
            await Task.Yield();
            if (!Queues.ContainsKey(envelope.Recipient))
                Queues[envelope.Recipient] = new List<EncryptedMessageEnvelope>();
            Queues[envelope.Recipient].Add(envelope);

            if (RunMessageServiceWithVirtualReceivers &&
                envelope.Recipient.Type == ChimeraEntityIdentifier.EntityType.Agent)
            {
                _logger.LogTrace($"Enqueueing {envelope.MessageType} message for {envelope.Recipient}");
                if (!VirtualProcessors.ContainsKey(envelope.Recipient))
                    VirtualProcessors.Add(
                        envelope.Recipient,
                        Task.Factory.StartNew(() => RunVirtualProcessor(envelope.Recipient.Id),
                            CancellationToken.None,
                            TaskCreationOptions.LongRunning,
                            TaskScheduler.Default));
            }
        }


        private async Task RunVirtualProcessor(ChimeraAgentIdentifier agentId)
        {
            // equivalent to execution loop thread
            _logger.LogInformation("Spinning up virtual agent processing thread for " + agentId.ShortId);
            while (true)
                await Dequeue(agentId, async envelope =>
                {
                    _logger.LogInformation("Successful dequeue for virtual agent " + agentId.ShortId);
                    _workflowProcessorFactory.GetWorkflowProcessor(agentId, "Virtual agent " + agentId.ShortId,
                        envelope);
                    await Task.Delay(5000);
                    return MessageActions.Complete;
                });
        }

        public string GetQueueName(ChimeraEntityIdentifier entity)
        {
            return entity.Type switch
            {
                ChimeraEntityIdentifier.EntityType.Agent => $"agent-{entity.Id}",
                ChimeraEntityIdentifier.EntityType.User => $"user-{entity.Id}",
                _ => throw new Exception("Invalid identifier for messaging!")
            };
        }
    }
}