using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SecOpsSteward.Plugins;
using SecOpsSteward.Shared.Configuration;
using SecOpsSteward.Shared.Cryptography;
using SecOpsSteward.Shared.Cryptography.Extensions;
using SecOpsSteward.Shared.Messages;
using SecOpsSteward.Shared.NonceTracking;
using SecOpsSteward.Shared.Packaging;
using static SecOpsSteward.Shared.ChimeraEntityIdentifier;

namespace SecOpsSteward.Shared.DiscoveryWorkflow
{
    public enum WorkflowPhases
    {
        DecryptEnvelope = 0,
        ValidateHeaders = 1,
        CheckWorkflowPreConditions = 2,
        ValidatePreviousReceiptSignatures = 3,
        EnforceWorkflowStepOrder = 4,
        ValidateStepSignatureAndGrantingUser = 5,
        CheckUserAccess = 6,
        CheckStepConditions = 7,
        AcquireAndValidatePackage = 8,
        ExecutePlugin = 9,
        CreateNextSteps = 10,
        FinalizeWorkflowReceipt = 11,
        SendingReceipts = 12,
        Complete = 13
    }

    public class WorkflowProcessorFactory
    {
        private readonly IServiceProvider _services;

        public WorkflowProcessorFactory(IServiceProvider services)
        {
            _services = services;
        }

        public ActiveWorkflow GetWorkflowProcessor(ChimeraEntityIdentifier agentId, string agentDescription,
            EncryptedMessageEnvelope envelope, bool ignoreUserPermissionRestrictions = false)
        {
            try
            {
                var instance = ActivatorUtilities.CreateInstance<ActiveWorkflow>(_services);
                instance.Configure(envelope, agentId, agentDescription, ignoreUserPermissionRestrictions);
                return instance;
            }
            catch
            {
            }

            return null;
        }
    }

    public class ActiveWorkflow
    {
        private readonly IConfigurationProvider _configProvider;
        private readonly ICryptographicService _cryptographicService;
        private readonly IMessageTransitService _messageTransit;
        private readonly INonceTrackingService _nonceTracker;
        private readonly PackageActionsService _packageActions;

        private readonly IServiceProvider _services;
        private readonly SecurityTripwire _tripwire;

        private readonly ILogger<ActiveWorkflow> _logger;
        private WorkflowPhases _phase = 0;

        public ActiveWorkflow(
            ILogger<ActiveWorkflow> logger,
            IServiceProvider services,
            ICryptographicService cryptographicService,
            IConfigurationProvider configProvider,
            INonceTrackingService nonceTracker,
            IMessageTransitService messageTransit,
            PackageActionsService packageActions,
            SecurityTripwire tripwire)
        {
            _logger = logger;
            _services = services;
            _cryptographicService = cryptographicService;
            _configProvider = configProvider;
            _nonceTracker = nonceTracker;
            _messageTransit = messageTransit;
            _packageActions = packageActions;
            _tripwire = tripwire;
        }

        public ChimeraEntityIdentifier AgentId { get; set; }
        public string AgentDescription { get; set; }

        public EncryptedMessageEnvelope Envelope { get; set; }
        public WorkflowExecutionMessage WorkflowMessage { get; set; }
        public WorkflowReceipt WorkflowReceipt { get; set; }

        public string WorkflowNextNonce { get; set; }

        private ExecutionStep CurrentStep => WorkflowMessage.Next == Guid.Empty
            ? WorkflowMessage.Steps[0]
            : WorkflowMessage.Steps.First(s => s.StepId == WorkflowMessage.Next);

        public ChimeraContainer StepContainer { get; set; }

        public ChimeraUserIdentifier GrantingUser
        {
            get
            {
                if (CurrentStep != null && CurrentStep.Signature != null)
                    return CurrentStep.Signature.Signer.Id;
                if (WorkflowMessage != null && WorkflowMessage.Signature != null)
                    return WorkflowMessage.Signature.Signer.Id;
                return null;
            }
        }

        public ExecutionStepReceipt CurrentStepReceipt { get; set; }

        public List<EncryptedObject> OutgoingReceipts { get; set; } = new();

        public bool IgnoreUserPermissionRestrictions { get; private set; }

        internal void Configure(
            EncryptedMessageEnvelope envelope,
            ChimeraEntityIdentifier agentId,
            string agentDescription,
            bool ignoreUserPermissionRestrictions = false)
        {
            Envelope = envelope;
            AgentId = agentId;
            AgentDescription = agentDescription;
            IgnoreUserPermissionRestrictions = ignoreUserPermissionRestrictions;
        }

        public async Task Run()
        {
            _phase = WorkflowPhases.DecryptEnvelope;
            await BeginProcessingEnvelope();

            WorkflowReceipt = new WorkflowReceipt
            {
                WorkflowRunCount = 1,
                Receipts = WorkflowMessage.Receipts,
                WorkflowComplete = false,
                WorkflowId = WorkflowMessage.WorkflowId
            };

            _phase = WorkflowPhases.ValidateHeaders;
            if (!await ValidateWorkflowHeaders())
                return;

            _phase = WorkflowPhases.CheckWorkflowPreConditions;
            if (!CheckWorkflowMessageConditions())
                return;

            _phase = WorkflowPhases.ValidatePreviousReceiptSignatures;
            if (!await ValidatePreviousReceiptSignatures())
                return;

            _phase = WorkflowPhases.EnforceWorkflowStepOrder;
            if (!await EnforceWorkflowMessageOrdering())
                return;

            CurrentStepReceipt = new ExecutionStepReceipt(CurrentStep.StepId);

            _phase = WorkflowPhases.ValidateStepSignatureAndGrantingUser;
            if (!await ValidateStepSignatureAndGrantingUser())
                return;

            _phase = WorkflowPhases.CheckUserAccess;
            if (AgentId.Type == EntityType.User)
                LogInfo("Bypassing security access check because we are running locally!");
            else if (IgnoreUserPermissionRestrictions)
                LogWarning(
                    "Bypassing security access check because IgnoreUserPermissionRestrictions is ON! Do NOT do this in production environments!");
            else if (!await CheckUserAccess())
                return;

            _phase = WorkflowPhases.CheckStepConditions;
            if (!CheckStepConditions())
                return;

            _phase = WorkflowPhases.AcquireAndValidatePackage;
            if (!await AcquireAndCheckPackage())
                return;

            _phase = WorkflowPhases.ExecutePlugin;
            if (!await ExecutePlugin())
                return;

            await SignStepReceipt();

            _phase = WorkflowPhases.CreateNextSteps;
            OutgoingReceipts.AddRange(await CreateNextSteps());

            _phase = WorkflowPhases.FinalizeWorkflowReceipt;
            OutgoingReceipts.Add(await GetFinalizedWorkflowReceipt());

            _phase = WorkflowPhases.SendingReceipts;

            LogInfo("Enqueueing {count} receipts and next-steps", OutgoingReceipts.Count);
            await Task.WhenAll(OutgoingReceipts.Select(r =>
                _messageTransit.Enqueue(new EncryptedMessageEnvelope(Envelope.ThreadId, r))));

            _phase = WorkflowPhases.Complete;
            LogInfo("Processing complete");
        }

        private void CompleteCurrentStepReceipt(ResultCodes code = ResultCodes.RanPluginOk, string failure = null)
        {
            if (!string.IsNullOrEmpty(CurrentStepReceipt.PluginResult.Exception))
                code = ResultCodes.RanPluginWithError;

            CurrentStepReceipt.ExecutionEnded = DateTimeOffset.UtcNow;
            CurrentStepReceipt.StepExecutionResult = code;

            CurrentStepReceipt.PluginResult.Exception = failure;
            if (code == ResultCodes.RanPluginWithError)
                CurrentStepReceipt.PluginResult.ResultCode = CommonResultCodes.Failure;
        }

        private async Task BeginProcessingEnvelope()
        {
            try
            {
                _logger.LogInformation("Decrypting message envelope with thread {id}", Envelope.ThreadId);
                WorkflowMessage = await Envelope.Payload.Decrypt<WorkflowExecutionMessage>(_cryptographicService);
            }
            catch (Exception ex)
            {
                LogError(ex, "Envelope decryption failed");
            }
        }

        private async Task<bool> ValidateWorkflowHeaders()
        {
            LogTrace("Checking if the incoming step is addressed to this agent");
            if (CurrentStep.RunningEntity != AgentId)
            {
                LogError("Recipient mismatch between envelope and message");
                TriggerTripwire(SecurityTripwireConditions.RecipientMismatch);
            }

            LogTrace("Verifying Workflow Message signature");
            if (!await WorkflowMessage.Verify(_cryptographicService))
            {
                LogError("Workflow message failed signature verification! {signature}", WorkflowMessage.Signature);
                TriggerTripwire(SecurityTripwireConditions.WorkflowMessageSignatureInvalid);
                return false;
            }

            return true;
        }

        private bool CheckWorkflowMessageConditions()
        {
            LogInfo("Checking workflow conditions for {id}", WorkflowMessage.WorkflowId);
            if (!WorkflowMessage.Conditions.IsValid(WorkflowMessage.LastRunReceipt))
            {
                LogError("Last run receipt (condition) is _NOT VALID_ for workflow {id}!", WorkflowMessage.WorkflowId);
                TriggerTripwire(SecurityTripwireConditions.WorkflowLastRunReceiptSignatureInvalid);
                return false;
            }

            if (WorkflowMessage.LastRunReceipt != null)
                WorkflowReceipt.WorkflowRunCount = WorkflowMessage.LastRunReceipt.WorkflowRunCount + 1;

            return true;
        }

        private async Task<bool> ValidatePreviousReceiptSignatures()
        {
            LogTrace("Validating signatures on all previous receipts");
            var results =
                await Task.WhenAll(WorkflowMessage.Receipts.Select(async r => await r.Verify(_cryptographicService)));
            if (results.Any(r => !r))
            {
                TriggerTripwire(SecurityTripwireConditions.ProvidedReceiptsSignaturesInvalid);
                LogError("{count} receipts from previous runs are _NOT VALID_ for workflow {id}!",
                    results.Count(r => !r), WorkflowMessage.WorkflowId);
                return false;
            }

            return true;
        }

        private async Task<bool> EnforceWorkflowMessageOrdering()
        {
            // -------- ### Validate Workflow Nonce (if 1st Step) ### --------
            if (WorkflowMessage.Next == Guid.Empty)
            {
                LogTrace("Checking workflow nonce");

                WorkflowNextNonce =
                    await _nonceTracker.ValidateNonce(AgentId, WorkflowMessage.WorkflowId, WorkflowMessage.Nonce);

                if (string.IsNullOrEmpty(WorkflowNextNonce))
                {
                    LogError("Invalid workflow nonce");
                    TriggerTripwire(SecurityTripwireConditions.WorkflowNonceCollision);
                    return false;
                }
            }
            else
            {
                // -------- ### Check Previous Running Identities Against Their Signatures ### --------
                LogTrace("Validating previous running identity matches signature");
                var previousReceipt = WorkflowMessage.Receipts.Last();
                var previousStep = WorkflowMessage.Steps.First(s => s.StepId == previousReceipt.StepId);
                if (previousReceipt.Signature.Signer != previousStep.RunningEntity)
                {
                    LogError("Previous receipt signer does not match the entity who was supposed to execute it!");
                    TriggerTripwire(SecurityTripwireConditions.PreviousSignerSignatureMismatch);
                    return false;
                }

                WorkflowNextNonce = WorkflowMessage.Nonce;
                if (string.IsNullOrEmpty(WorkflowNextNonce))
                {
                    LogError("Nonce was not carried forward!");
                    return false;
                }
            }

            return true;
        }

        // -- step --

        private async Task<bool> ValidateStepSignatureAndGrantingUser()
        {
            if (CurrentStep.Signature.IsSigned)
            {
                // per-step signatures are preferred over batch ones, if present
                LogTrace("Verifying step execution request signature");
                var msgVerified = await CurrentStep.Verify(_cryptographicService);
                if (!msgVerified)
                {
                    LogError("Step command failed signature verification! {signature}", CurrentStep.Signature);
                    TriggerTripwire(SecurityTripwireConditions.ExecutionStepSignatureInvalid);

                    CompleteCurrentStepReceipt(ResultCodes.InvalidMessageSignature, "Invalid signature");
                    return false;
                }
            }
            else if (!WorkflowMessage.Signature.IsSigned)
            {
                _logger.LogError("Step not signed and no batch signer present!");
                TriggerTripwire(SecurityTripwireConditions.ExecutionStepSignatureInvalid);

                CompleteCurrentStepReceipt(ResultCodes.InvalidMessageSignature, "Invalid signature");
                return false;
            }

            return true;
        }

        private async Task<bool> CheckUserAccess()
        {
            LogTrace("Checking user {user} access to package {package}", GrantingUser, CurrentStep.PackageId);
            var config = await _configProvider.GetConfiguration(CurrentStep.RunningEntity.Id);
            if (!config.AccessRules.HasAccess(GrantingUser, CurrentStep.PackageId))
            {
                LogError("User {user} is not authorized to execute this package.", GrantingUser);
                TriggerTripwire(SecurityTripwireConditions.UserNotAuthorized);
                CompleteCurrentStepReceipt(ResultCodes.Unauthorized, "Unauthorized");
                return false;
            }

            return true;
        }

        private bool CheckStepConditions()
        {
            LogTrace("Checking step conditions. {condition}", CurrentStep.Conditions);
            var isValid = CurrentStep.Conditions == null || CurrentStep.Conditions.IsValid(WorkflowMessage.Receipts);
            if (!isValid)
            {
                LogError("Required conditions not met to execute step.");
                TriggerTripwire(SecurityTripwireConditions.ConditionsNotMetForExecution);

                CompleteCurrentStepReceipt(ResultCodes.ConditionsNotMet, "Required conditions not met");
                return false;
            }

            return true;
        }

        private async Task<bool> AcquireAndCheckPackage()
        {
            LogTrace("Acquiring package {package} from repository", CurrentStep.PackageId);
            try
            {
                StepContainer = await _packageActions.LoadContainer(CurrentStep.PackageId, true);
            }
            catch (BadImageFormatException ex) // signature/hash failure
            {
                LogError(ex, "Package hash integrity or signature(s) invalid");
                TriggerTripwire(SecurityTripwireConditions.PackageSignaturesInvalid);
                CompleteCurrentStepReceipt(ResultCodes.PackageVerificationFailed, "Package hash mismatch");
                return false;
            }
            catch (Exception ex) // other failure
            {
                LogError(ex, "Package loading failed");
                TriggerTripwire(SecurityTripwireConditions.PackageLoadFailed);
                CompleteCurrentStepReceipt(ResultCodes.PackageVerificationFailed, "Package hash mismatch");
                return false;
            }

            // check expected hash against loaded version
            if (CurrentStep.PackageSignature != null && CurrentStep.PackageSignature.Length > 0 &&
                !_packageActions.CheckContainerHash(CurrentStep.PackageId, CurrentStep.PackageSignature))
            {
                LogError("Package hash did not match the expected hash from the step");
                TriggerTripwire(SecurityTripwireConditions.PackageContentHashMismatch);
                CompleteCurrentStepReceipt(ResultCodes.PackageVerificationFailed, "Package hash mismatch");
                return false;
            }

            return true;
        }

        private async Task<bool> ExecutePlugin()
        {
            // get outputs from the previous step
            var passthroughOutputs = new PluginOutputStructure(CommonResultCodes.Success);
            ExecutionStepReceipt previousStep = null;
            if (WorkflowMessage.Receipts.Count > 0)
            {
                previousStep = WorkflowMessage.Receipts[WorkflowMessage.Receipts.Count - 1];
                passthroughOutputs.SharedOutputs = previousStep.PluginResult.SharedOutputs.AsClone();

                if (!ChimeraSharedHelpers.GetHash(previousStep.PluginResult)
                    .SequenceEqual(previousStep.PluginResultHash))
                {
                    LogError("Content hash for previous step result did not match");
                    TriggerTripwire(SecurityTripwireConditions.PreviousResultHashMismatch);
                    CompleteCurrentStepReceipt(ResultCodes.InvalidMessageSignature,
                        "Step result content hash not valid");
                    return false;
                }
            }


            // execute
            LogTrace("Beginning execution of package {package}", CurrentStep.PackageId);

            try
            {
                var plugin = StepContainer.Wrapper.GetPlugin(CurrentStep.PackageId.Id)
                    .Emit(_services, CurrentStep.Arguments);
                CurrentStepReceipt.PluginResult = await plugin.Execute(passthroughOutputs);
                LogTrace("Execution of package {package} complete", CurrentStep.PackageId);
            }
            catch (Exception e)
            {
                CurrentStepReceipt.PluginResult = new PluginOutputStructure(CommonResultCodes.Failure);
                CurrentStepReceipt.PluginResult.Exception = e.ToString();
                LogError(e, "Execution of package {package} failed", CurrentStep.PackageId);
                TriggerTripwire(SecurityTripwireConditions.PluginExecutionFailed);
            }

            CurrentStepReceipt.PluginResultHash = ChimeraSharedHelpers.GetHash(CurrentStepReceipt.PluginResult);

            CompleteCurrentStepReceipt();

            return true;
        }

        private async Task SignStepReceipt()
        {
            LogTrace("Signing step receipt as {id}", AgentId);
            await CurrentStepReceipt.Sign(_cryptographicService, AgentId, AgentDescription);
            WorkflowMessage.Receipts.Add(CurrentStepReceipt);

            if (!WorkflowMessage.Steps
                .GetNextSteps(CurrentStepReceipt.StepId, CurrentStepReceipt.PluginResult.ResultCode).Any())
                WorkflowReceipt.WorkflowComplete = true;
        }

        // -- /step --

        private async Task<List<EncryptedObject>> CreateNextSteps()
        {
            var receipts = new List<EncryptedObject>();
            if (!WorkflowReceipt.WorkflowComplete)
            {
                var nextSteps = WorkflowMessage.Steps.GetNextSteps(CurrentStepReceipt.StepId,
                    CurrentStepReceipt.PluginResult.ResultCode);
                WorkflowMessage.Nonce = WorkflowNextNonce; // send new nonce along

                LogTrace("Writing orders for {count} next steps", nextSteps.Count());

                foreach (var step in nextSteps)
                {
                    WorkflowMessage.Next = step.StepId;
                    LogTrace("Writing orders for next step {stepId}", step.StepId.ToString());
                    receipts.Add(await WorkflowMessage.Encrypt(_cryptographicService, step.RunningEntity));
                }
            }
            else // If complete, embed the passed nonce
            {
                WorkflowReceipt.NewNonce = WorkflowNextNonce;
            }

            return receipts;
        }

        private async Task<EncryptedObject> GetFinalizedWorkflowReceipt()
        {
            // strip transitioning outputs from the intermediate workflow-level receipt
            WorkflowReceipt.Receipts.ForEach(r =>
                r.PluginResult.SharedOutputs = r.PluginResult.SharedOutputs.AsScrubbedCollection());

            // sign and encrypt the receipt
            LogTrace("Signing workflow receipt as {id}", AgentId);
            await WorkflowReceipt.Sign(_cryptographicService, AgentId, AgentDescription);

            LogTrace("Generating encrypted step receipt for sender {id}", WorkflowMessage.Signature.Signer);
            return await WorkflowReceipt.Encrypt(_cryptographicService, WorkflowMessage.Signature.Signer);
        }

        #region Logging

        private void TriggerTripwire(SecurityTripwireConditions condition)
        {
            _tripwire.HandleTripwire(condition, this);
        }

        private void LogTrace(string message, params object[] parameters)
        {
            parameters = InjectParameters(parameters);
            _logger.LogTrace("[{wfId}][{stepId}][{phase}]  " + message, parameters);
        }

        private void LogInfo(string message, params object[] parameters)
        {
            parameters = InjectParameters(parameters);
            _logger.LogInformation("[{wfId}][{stepId}][{phase}]  " + message, parameters);
        }

        private void LogWarning(string message, params object[] parameters)
        {
            parameters = InjectParameters(parameters);
            _logger.LogWarning("[{wfId}][{stepId}][{phase}]  " + message, parameters);
        }

        private void LogError(Exception ex, string message, params object[] parameters)
        {
            parameters = InjectParameters(parameters);
            _logger.LogError(ex, "[{wfId}][{stepId}][{phase}]  " + message, parameters);
        }

        private void LogError(string message, params object[] parameters)
        {
            parameters = InjectParameters(parameters);
            _logger.LogError("[{wfId}][{stepId}][{phase}]  " + message, parameters);
        }

        private object[] InjectParameters(object[] parameters)
        {
            var parametersList = new List<object>(parameters);
            parametersList.Insert(0, CurrentStep.StepId.ShortId());
            parametersList.Insert(1, WorkflowMessage.WorkflowId.ShortId());
            parametersList.Insert(2, _phase);
            return parametersList.ToArray();
        }

        #endregion
    }
}