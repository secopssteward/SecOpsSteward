using System;
using System.Collections.Generic;
using System.Linq;

namespace SecOpsSteward.Shared.Messages
{
    /// <summary>
    ///     Conditions required for a Package to be executed
    /// </summary>
    public class ExecutionStepConditions
    {
        /// <summary>
        ///     Earliest date/time the Package can be executed
        /// </summary>
        public DateTimeOffset ValidFrom { get; set; } = DateTimeOffset.MinValue;

        /// <summary>
        ///     Latest date/time the Package can be executed
        /// </summary>
        public DateTimeOffset ValidTo { get; set; } = DateTimeOffset.MaxValue;

        /// <summary>
        ///     Amount of time, in seconds, which can elapse between the receipt signing and this execution
        /// </summary>
        public int RequiredReceiptWindowSeconds { get; set; } = (int) TimeSpan.FromHours(2).TotalSeconds;

        /// <summary>
        ///     Receipts required to be present for this execution to progress
        /// </summary>
        public List<ExecutionStepReceipt> RequiredReceipts { get; set; } = new();

        /// <summary>
        ///     If the conditions are valid
        /// </summary>
        /// <param name="givenReceipts"></param>
        /// <returns></returns>
        public bool IsValid(List<ExecutionStepReceipt> givenReceipts)
        {
            if (ValidFrom > DateTime.UtcNow || ValidTo < DateTime.UtcNow)
                return false;

            if (RequiredReceipts.Any())
            {
                var allStart = RequiredReceipts.Min(r => r.ExecutionStarted);
                var allEnd = RequiredReceipts.Max(r => r.ExecutionEnded);
                if ((allEnd - allStart).TotalSeconds > RequiredReceiptWindowSeconds)
                    return false;

                if (givenReceipts.Count < RequiredReceipts.Count) return false;

                foreach (var reqd in RequiredReceipts)
                {
                    var match = givenReceipts.FirstOrDefault(r => r.StepId == reqd.StepId);
                    if (match == null) return false;

                    if (reqd.StepExecutionResult != ResultCodes.None &&
                        reqd.StepExecutionResult != match.StepExecutionResult)
                        return false;

                    if (reqd.Signature != null && reqd.Signature.Signer != null &&
                        reqd.Signature.Signer != match.Signature?.Signer)
                        return false;
                }
            }

            return true;
        }

        public override string ToString()
        {
            if (RequiredReceipts.Any())
                return
                    $"Valid from {ValidFrom} to {ValidTo}. Requires {RequiredReceipts.Count} receipts within {TimeSpan.FromSeconds(RequiredReceiptWindowSeconds)}: " +
                    string.Join(", ", RequiredReceipts.Select(rr => $"[{rr.StepId}:{rr.PluginResult?.ResultCode}]"));
            return $"Valid from {ValidFrom} to {ValidTo}.";
        }
    }
}