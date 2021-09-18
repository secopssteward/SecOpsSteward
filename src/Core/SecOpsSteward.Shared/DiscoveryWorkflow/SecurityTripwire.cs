using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace SecOpsSteward.Shared.DiscoveryWorkflow
{
    /// <summary>
    ///     Delegate to fire when a security tripwire is found to be tripped
    /// </summary>
    /// <param name="condition">Condition causing tripwire to fire</param>
    /// <param name="workflow">Workflow being processed</param>
    public delegate void HandleSecurityTripwire(SecurityTripwireConditions condition, ActiveWorkflow workflow);

    /// <summary>
    ///     Conditions which can cause a security tripwire to fire
    /// </summary>
    public enum SecurityTripwireConditions
    {
        Unknown,

        /// <summary>
        ///     Workflow message signature is not valid
        /// </summary>
        WorkflowMessageSignatureInvalid,

        /// <summary>
        ///     Workflow nonce is invalid
        /// </summary>
        WorkflowNonceCollision,

        /// <summary>
        ///     Last-run receipt given with Workflow does not have a valid signature
        /// </summary>
        WorkflowLastRunReceiptSignatureInvalid,

        /// <summary>
        ///     Execution step does not have a valid signature
        /// </summary>
        ExecutionStepSignatureInvalid,

        /// <summary>
        ///     The user is not authorized to execute a Package
        /// </summary>
        UserNotAuthorized,

        /// <summary>
        ///     Conditions required to execute a Workflow or Step were not met
        /// </summary>
        ConditionsNotMetForExecution,

        /// <summary>
        ///     Given Package content hash does not match actual Package content hash
        /// </summary>
        PackageContentHashMismatch,

        /// <summary>
        ///     One or more signatures on a Package are not valid
        /// </summary>
        PackageSignaturesInvalid,

        /// <summary>
        ///     Execution of the plugin resulted in an Exception
        /// </summary>
        PluginExecutionFailed,

        /// <summary>
        ///     One or more receipts provided did not have valid signatures
        /// </summary>
        ProvidedReceiptsSignaturesInvalid,

        /// <summary>
        ///     Mismatch between recipient the message was delivered to, and who it was intended for
        /// </summary>
        RecipientMismatch,

        /// <summary>
        ///     Previous receipt signer does not match the entity ID who was supposed to execute it
        /// </summary>
        PreviousSignerSignatureMismatch,

        /// <summary>
        ///     Content hash of previous step's result did not match
        /// </summary>
        PreviousResultHashMismatch,

        /// <summary>
        ///     Package load failed for a reason other than hash/signature error
        /// </summary>
        PackageLoadFailed
    }

    /// <summary>
    ///     Handles security events
    /// </summary>
    public class SecurityTripwire
    {
        private readonly IEnumerable<HandleSecurityTripwire> _handlers;
        private readonly ILogger<SecurityTripwire> _logger;

        /// <summary>
        ///     Handles security events
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="handlers"></param>
        public SecurityTripwire(
            ILogger<SecurityTripwire> logger,
            IEnumerable<HandleSecurityTripwire> handlers)
        {
            _logger = logger;
            _handlers = handlers;
        }

        /// <summary>
        ///     Fire a tripwire as a result of a security problem
        /// </summary>
        /// <param name="condition">Condition which fired tripwire</param>
        /// <param name="workflow">Workflow being processed</param>
        public void HandleTripwire(SecurityTripwireConditions condition, ActiveWorkflow workflow)
        {
            if (_handlers.Count() > 1)
                _logger.LogTrace("Handling tripwire for condition {condition}", condition);
            else
                _logger.LogWarning(
                    "There are no registered tripwire handlers, but one was activated for condition {condition}",
                    condition);

            foreach (var handler in _handlers)
                try
                {
                    handler(condition, workflow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Tripwire handler crashed while processing an incoming tripwire!");
                }
        }
    }
}