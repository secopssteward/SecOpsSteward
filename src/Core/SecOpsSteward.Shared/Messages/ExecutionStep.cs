using System;
using SecOpsSteward.Shared.Cryptography;
using SecOpsSteward.Shared.Cryptography.Extensions;

namespace SecOpsSteward.Shared.Messages
{
    /// <summary>
    ///     Describes a single step in a workflow which executes a package
    /// </summary>
    public class ExecutionStep : ISignableByOne, IEncryptable
    {
        /// <summary>
        ///     Unique identifier of step
        /// </summary>
        public Guid StepId { get; set; } = Guid.NewGuid();

        /// <summary>
        ///     Step which precedes this step in sequence
        /// </summary>
        public Guid ParentStepId { get; set; } = Guid.Empty;

        /// <summary>
        ///     Result code from the previous step which leads to this one
        /// </summary>
        public string ParentStepResultCode { get; set; }

        /// <summary>
        ///     Nonce to prevent unauthorized message replay
        /// </summary>
        [NotSigned]
        public string Nonce { get; set; }

        /// <summary>
        ///     Identity which will execute the package
        /// </summary>
        public ChimeraEntityIdentifier RunningEntity { get; set; }

        /// <summary>
        ///     Package ID to execute
        /// </summary>
        public ChimeraPackageIdentifier PackageId { get; set; }

        /// <summary>
        ///     Expected content signature of the Package
        /// </summary>
        public byte[] PackageSignature { get; set; }

        /// <summary>
        ///     Arguments passed to the plugin in the Package
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        ///     Conditions which must be met prior to execution
        /// </summary>
        public ExecutionStepConditions Conditions { get; set; }

        /// <summary>
        ///     Entity who signed this step
        /// </summary>
        public ChimeraEntitySignature Signature { get; set; } = new();

        public override string ToString()
        {
            return $"Step {StepId} - {RunningEntity} executes {PackageId} - {Signature}";
        }
    }
}