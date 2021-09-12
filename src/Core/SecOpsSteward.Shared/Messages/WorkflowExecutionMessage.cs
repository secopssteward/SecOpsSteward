using SecOpsSteward.Shared.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SecOpsSteward.Shared.Messages
{
    /// <summary>
    /// Describes a Workflow which can be executed across many agents
    /// </summary>
    public class WorkflowExecutionMessage : ISignableByOne, IEncryptable
    {
        /// <summary>
        /// Workflow ID
        /// </summary>
        public Guid WorkflowId { get; set; }

        /// <summary>
        /// User who approved the workflow
        /// </summary>
        public ChimeraUserIdentifier UserId { get; set; }

        /// <summary>
        /// Steps to be executed as a part of this workflow
        /// </summary>
        public ExecutionStepCollection Steps { get; set; } = new ExecutionStepCollection();

        /// <summary>
        /// Receipts for Steps already executed as a part of this workflow
        /// </summary>
        [NotSigned] // receipts of steps already processed
        public ExecutionStepReceiptCollection Receipts { get; set; } = new ExecutionStepReceiptCollection();

        /// <summary>
        /// Next step to be processed (order is enforced by Conditions)
        /// </summary>
        [NotSigned]
        public Guid Next { get; set; } = Guid.Empty;

        /// <summary>
        /// Signature of the user who approved this workflow
        /// </summary>
        public ChimeraEntitySignature Signature { get; set; } = new ChimeraEntitySignature();

        /// <summary>
        /// Receipt of the previous run of this Workflow, if appropriate
        /// </summary>
        [NotSigned]
        public WorkflowReceipt LastRunReceipt { get; set; } = null;

        /// <summary>
        /// Conditions required to be met for this Workflow to begin
        /// </summary>
        public WorkflowExecutionConditions Conditions { get; set; } = new WorkflowExecutionConditions();

        /// <summary>
        /// Nonce to prevent replay attacks on this Workflow
        /// </summary>
        [NotSigned]
        public string Nonce { get; set; }

        public override string ToString()
        {
            return $"Workflow {WorkflowId} - Started by {UserId} - {Receipts.Count}/{Steps.Count} - {Signature}. (Nonce: {Nonce}) {Conditions}";
        }

        public WorkflowExecutionMessage() { }

        public WorkflowExecutionMessage(
            ChimeraUserIdentifier userId,
            ExecutionStepCollection steps)
        {
            this.WorkflowId = Guid.NewGuid();
            this.UserId = userId;
            this.Steps = steps;
        }

        /// <summary>
        /// Create workflow from previous run
        /// </summary>
        /// <param name="previousWorkflow">Previous workflow</param>
        /// <param name="previousReceipt">Last workflow receipt</param>
        /// <returns>New workflow</returns>
        public WorkflowExecutionMessage(
            WorkflowExecutionMessage previousWorkflow,
            WorkflowReceipt previousReceipt)
        {
            this.WorkflowId = previousWorkflow.WorkflowId;
            this.UserId = previousWorkflow.UserId;
            this.Steps.AddRange(previousWorkflow.Steps);
            this.Steps.ToList().ForEach(step =>
            {
                var match = previousReceipt.Receipts.FirstOrDefault(s => s.StepId == step.StepId);
                if (match != null)
                    step.Nonce = match.NewNonce;
            });
            this.LastRunReceipt = previousReceipt;
        }

        public IEnumerable<ExecutionStep> GetNextSteps()
        {
            if (!Receipts.Any()) return Steps.GetNextSteps(Guid.Empty);
            var lastReceipt = Receipts.Last();
            return Steps.GetNextSteps(lastReceipt.StepId, lastReceipt.PluginResult.ResultCode);
        }
    }
}
