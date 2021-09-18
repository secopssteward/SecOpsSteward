using System;
using SecOpsSteward.Shared.Cryptography;
using SecOpsSteward.Shared.Cryptography.Extensions;

namespace SecOpsSteward.Shared.Messages
{
    /// <summary>
    ///     Receipt which indicates a completed Workflow
    /// </summary>
    public class WorkflowReceipt : ISignableByOne, IEncryptable
    {
        /// <summary>
        ///     Workflow ID
        /// </summary>
        public Guid WorkflowId { get; set; }

        /// <summary>
        ///     If the Workflow is complete; if not, this is an intermediate receipt
        /// </summary>
        public bool WorkflowComplete { get; set; }

        /// <summary>
        ///     Number of times this Workflow has been run
        /// </summary>
        public int WorkflowRunCount { get; set; }

        /// <summary>
        ///     Receipts provided by the various agents which processed this Workflow's steps
        /// </summary>
        public ExecutionStepReceiptCollection Receipts { get; set; }

        /// <summary>
        ///     New nonce to use on the next execution of this workflow
        /// </summary>
        public string NewNonce { get; set; }

        /// <summary>
        ///     Signature of the last agent running a step, which attests to this entire receipt
        /// </summary>
        public ChimeraEntitySignature Signature { get; set; }

        public override string ToString()
        {
            return $"Workflow {WorkflowId} receipt - " + (WorkflowComplete
                ? "Incomplete"
                : "Complete" + $" with {Receipts.Count} receipts - {Signature}");
        }

        /// <summary>
        ///     Clone this receipt without the signature
        /// </summary>
        /// <returns></returns>
        public WorkflowReceipt Clone()
        {
            var receipt = new WorkflowReceipt
            {
                WorkflowId = WorkflowId,
                WorkflowComplete = WorkflowComplete,
                WorkflowRunCount = WorkflowRunCount,
                Receipts = new ExecutionStepReceiptCollection(),
                NewNonce = NewNonce
            };
            receipt.Receipts.AddRange(Receipts);
            return receipt;
        }
    }
}