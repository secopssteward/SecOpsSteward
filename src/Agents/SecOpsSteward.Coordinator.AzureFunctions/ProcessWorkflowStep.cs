using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SecOpsSteward.Shared;
using SecOpsSteward.Shared.DiscoveryWorkflow;
using SecOpsSteward.Shared.Messages;

namespace SecOpsSteward.Coordinator.AzureFunctions
{
    public class ProcessWorkflowStep
    {
        private readonly ILogger<ProcessWorkflowStep> _logger;
        private readonly WorkflowProcessorFactory _workflowProcessorFactory;

        public ProcessWorkflowStep(
            WorkflowProcessorFactory workflowProcessorFactory,
            ILogger<ProcessWorkflowStep> logger)
        {
            _workflowProcessorFactory = workflowProcessorFactory;
            _logger = logger;
        }

        [Function("ProcessWorkflowStep")]
        public async Task Run([ServiceBusTrigger("agent-%AgentId%", Connection = "SOSBus")] byte[] message)
        {
            var envelope = ChimeraSharedHelpers.GetFromSerializedBytes<EncryptedMessageEnvelope>(message);
            _logger.LogInformation("Received new envelope for processing addressed to {0}, ThreadID is {1}",
                envelope.Recipient, envelope.ThreadId);

            var wfProcessor = _workflowProcessorFactory.GetWorkflowProcessor(
                Program.AgentId,
                Program.AgentDescription,
                envelope,
                Program.IgnoreUserPermissionRestrictions);
            await wfProcessor.Run();

            _logger.LogInformation("Processing complete");
        }
    }
}