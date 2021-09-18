using System;
using System.Collections.Generic;
using System.Linq;

namespace SecOpsSteward.Shared.Messages
{
    /// <summary>
    ///     Collection of ExecutionStep Receipts
    /// </summary>
    public class ExecutionStepReceiptCollection : List<ExecutionStepReceipt>
    {
        /// <summary>
        ///     Extract a set of updated nonces for processed steps
        /// </summary>
        /// <returns>Dictionary of updated nonces per-step</returns>
        public Dictionary<Guid, string> GetNewNoncesFromReceipt()
        {
            return this.ToDictionary(k => k.StepId, v => v.NewNonce);
        }
    }
}