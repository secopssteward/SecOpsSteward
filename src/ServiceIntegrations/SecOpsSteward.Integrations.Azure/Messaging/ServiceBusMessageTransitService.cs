﻿using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using SecOpsSteward.Plugins.Azure;
using SecOpsSteward.Shared;
using SecOpsSteward.Shared.Messages;
using SecOpsSteward.Shared.Roles;
using SecOpsSteward.Shared.Services;
using System;
using System.Threading.Tasks;

namespace SecOpsSteward.Integrations.Azure.Messaging
{
    public class ServiceBusMessageTransitService : AzurePlatformIntegrationBase, IMessageTransitService, IHasUserEnrollmentActions, IHasAgentCreationActions
    {
        public int ServicePriority => 30;

        public ServiceBusMessageTransitService(
            ILogger<ServiceBusMessageTransitService> logger,
            ChimeraServiceConfigurator configurator,
            IRoleAssignmentService roleAssignment,
            AzureCurrentCredentialFactory platformFactory) : base(logger, configurator, roleAssignment, platformFactory) { }

        private string CfgServiceBusNamespace => _configurator["ServiceBusNamespace"];

        public async Task Dequeue(ChimeraEntityIdentifier entityId, Func<EncryptedMessageEnvelope, Task<MessageActions>> action)
        {
            var queueName = GetQueueName(entityId);
            var sbc = new ServiceBusClient($"{CfgServiceBusNamespace}.servicebus.windows.net", _platformFactory.GetCredential().Credential);
            var receiver = sbc.CreateReceiver(queueName);

            var msg = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2));
            if (msg == null)
                return;
            else
            {
                var message = ChimeraSharedHelpers.GetFromSerializedBytes<EncryptedMessageEnvelope>(msg.Body.ToArray());
                var result = await action(message);

                switch (result)
                {
                    case MessageActions.Unknown:
                        break;
                    case MessageActions.Complete:
                        await receiver.CompleteMessageAsync(msg);
                        break;
                    case MessageActions.Abandon:
                        await receiver.AbandonMessageAsync(msg);
                        break;
                    case MessageActions.Defer:
                        await receiver.DeferMessageAsync(msg);
                        break;
                }
            }
        }

        public async Task Enqueue(EncryptedMessageEnvelope envelope)
        {
            var queueName = GetQueueName(envelope.Recipient);
            var sbc = new ServiceBusClient($"{CfgServiceBusNamespace}.servicebus.windows.net", _platformFactory.GetCredential().Credential);
            await sbc.CreateSender(queueName).SendMessageAsync(GetServiceQueueMessage(envelope));
        }

        // ---

        public Task OnUserEnrolled(ChimeraUserIdentifier user) => AddQueueFor(user);

        public Task OnUserRemoved(ChimeraUserIdentifier user) => DropQueueFor(user);

        public async Task OnAgentCreated(ChimeraAgentIdentifier agent)
        {
            await AddQueueFor(agent);

            // agent can send to anything (next step agents, user receipts)
            await _roleAssignment.ApplyScopedRoleToIdentity(agent, AssignableRole.CanSendToQueue, ServiceBusScope);
        }

        public Task OnAgentRemoved(ChimeraAgentIdentifier agent) => DropQueueFor(agent);

        public async Task AddQueueFor(ChimeraEntityIdentifier entity)
        {

            Logger.LogTrace("Starting creation of entity service bus queue");
            var sb = await _platformFactory.GetCredential().GetAzure().ServiceBusNamespaces.GetByResourceGroupAsync(
                CfgResourceGroup, CfgServiceBusNamespace);

            try
            {
                var thisQueue = await sb.Queues.GetByNameAsync(GetQueueName(entity));
                Logger.LogTrace("Entity service bus queue already exists");
            }
            catch
            {
                await sb.Update()
                        .WithNewQueue(GetQueueName(entity), 1024)
                        .ApplyAsync();
                Logger.LogTrace("Entity service bus queue created");
            }

            await _roleAssignment.ApplyScopedRoleToIdentity(entity, AssignableRole.CanReceiveFromQueue, GetQueueScope(GetQueueName(entity)));
        }

        public async Task DropQueueFor(ChimeraEntityIdentifier entity)
        {
            var sb = await _platformFactory.GetCredential().GetAzure().ServiceBusNamespaces.GetByResourceGroupAsync(
                CfgResourceGroup, CfgServiceBusNamespace);
            await sb.Update()
                .WithoutQueue(GetQueueName(entity))
                .ApplyAsync();
        }

        // ---

        protected string ServiceBusScope => $"{BaseScope}/providers/Microsoft.ServiceBus/namespaces/{CfgServiceBusNamespace}";

        protected string GetQueueScope(string queueName) => $"{ServiceBusScope}/queues/{queueName}";

        public string GetQueueName(ChimeraEntityIdentifier entity) =>
            entity.Type switch
            {
                ChimeraEntityIdentifier.EntityType.Agent => $"agent-{entity.Id}",
                ChimeraEntityIdentifier.EntityType.User => $"user-{entity.Id}",
                _ => throw new Exception("Invalid identifier for messaging!"),
            };

        protected ServiceBusMessage GetServiceQueueMessage(EncryptedMessageEnvelope envelope) =>
            new ServiceBusMessage(ChimeraSharedHelpers.SerializeToBytes(envelope));
    }
}
