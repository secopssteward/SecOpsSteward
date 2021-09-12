using SecOpsSteward.Data.Models;
using SecOpsSteward.Shared.Cryptography;
using SecOpsSteward.Shared.Messages;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SecOpsSteward.Data
{
    public class RuntimePeriodicActionService
    {
        private readonly SecOpsStewardDbContext _dbContext;
        private readonly IMessageTransitService _messageTransit;
        private readonly ICryptographicService _cryptoService;
        public RuntimePeriodicActionService(
            SecOpsStewardDbContext dbContext,
            IMessageTransitService messageTransit,
            ICryptographicService cryptoService)
        {
            _dbContext = dbContext;
            _messageTransit = messageTransit;
            _cryptoService = cryptoService;
        }

        public async Task PerformPeriodicActions()
        {
            await Task.WhenAll(_dbContext.WorkflowRecurrences
                      .ToList()
                      .Where(wfr => wfr.Approvers.Count >= wfr.NumberOfApproversRequired)
                      .Where(wfr => wfr.ShouldBeRun)
                      .Select(wfr => ProcessRecurrence(wfr)));
        }

        public async Task ProcessRecurrence(WorkflowRecurrenceModel recurrence)
        {
            _dbContext.WorkflowExecutions.Add(new WorkflowExecutionModel()
            {
                Approvers = recurrence.Approvers,
                Recurrence = recurrence,
                RunStarted = DateTimeOffset.UtcNow,
                Workflow = recurrence.Workflow
            });
            recurrence.MostRecentRun = DateTimeOffset.UtcNow;
            recurrence.Approvers = new System.Collections.Generic.List<Guid>();
            await _dbContext.SaveChangesAsync();

            var msg = recurrence.Workflow.WorkflowAuthorization;
            foreach (var step in msg.GetNextSteps())
            {
                var encrypted = await msg.Encrypt(_cryptoService, step.RunningEntity);
                var envelope = new EncryptedMessageEnvelope(encrypted);
                await _messageTransit.Enqueue(envelope);
            }
        }
    }
}
