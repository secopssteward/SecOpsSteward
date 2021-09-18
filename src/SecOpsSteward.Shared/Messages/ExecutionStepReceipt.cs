using System;
using SecOpsSteward.Plugins;
using SecOpsSteward.Shared.Cryptography;
using SecOpsSteward.Shared.Cryptography.Extensions;

namespace SecOpsSteward.Shared.Messages
{
    /// <summary>
    ///     Possible results from executing a Step
    /// </summary>
    public enum ResultCodes
    {
        None,
        InvalidMessageSignature,
        Unauthorized,
        InvalidNonce,
        PackageVerificationFailed,
        RanPluginOk,
        RanPluginWithError,
        ConditionsNotMet
    }

    /// <summary>
    ///     Receipt for a single ExecutionStep, signed by the executing entity
    /// </summary>
    public class ExecutionStepReceipt : ISignableByOne, IEncryptable
    {
        public ExecutionStepReceipt()
        {
        }

        public ExecutionStepReceipt(Guid stepId)
        {
            StepId = stepId;
            ExecutionStarted = DateTimeOffset.UtcNow;
        }

        /// <summary>
        ///     Step ID
        /// </summary>
        public Guid StepId { get; set; }

        /// <summary>
        ///     Nonce to be used on next execution
        /// </summary>
        public string NewNonce { get; set; }

        /// <summary>
        ///     When the execution processing began
        /// </summary>
        public DateTimeOffset ExecutionStarted { get; set; }

        /// <summary>
        ///     When the execution processing ended
        /// </summary>
        public DateTimeOffset ExecutionEnded { get; set; }

        /// <summary>
        ///     The result code of the execution process
        /// </summary>
        public ResultCodes StepExecutionResult { get; set; }

        /// <summary>
        ///     Hash for the plugin result's content; this is signed in lieu of signing the actual content,
        ///     which will be later stripped from the receipt.
        /// </summary>
        public byte[] PluginResultHash { get; set; }

        /// <summary>
        ///     The full result from the plugin
        /// </summary>
        [NotSigned]
        public PluginOutputStructure PluginResult { get; set; } = new(string.Empty);

        /// <summary>
        ///     Signature of the entity which performed the execution
        /// </summary>
        public ChimeraEntitySignature Signature { get; set; } = new();

        public override string ToString()
        {
            return
                $"Step {StepId} - Ran from {ExecutionStarted} to {ExecutionEnded} ({(ExecutionEnded - ExecutionStarted).TotalSeconds}s) - Result: {StepExecutionResult} - {Signature}";
        }
    }
}