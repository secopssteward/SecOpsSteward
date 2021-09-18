using System;
using System.Text.Json.Serialization;

namespace SecOpsSteward.Shared.Messages
{
    /// <summary>
    ///     Conditions which must be met for a Workflow to begin
    /// </summary>
    public class WorkflowExecutionConditions
    {
        /// <summary>
        ///     Earliest date/time the Workflow can be executed
        /// </summary>
        public DateTimeOffset ValidFrom { get; set; } = DateTimeOffset.MinValue;

        /// <summary>
        ///     Latest date/time the Workflow can be executed
        /// </summary>
        public DateTimeOffset ValidTo { get; set; } = DateTimeOffset.MaxValue;

        /// <summary>
        ///     Minimum time required to have elapsed between multiple executions of the same workflow
        /// </summary>
        [JsonIgnore]
        public TimeSpan TimeBetweenExecutions
        {
            get => TimeSpan.FromMinutes(TimeBetweenExecutionsMinutes);
            set => TimeBetweenExecutionsMinutes = (int) value.TotalMinutes;
        }

        /// <summary>
        ///     Minimum time required to have elapsed between multiple executions of the same workflow (in minutes)
        /// </summary>
        public int TimeBetweenExecutionsMinutes { get; set; }

        /// <summary>
        ///     Maximum number of times the Workflow can be executed
        /// </summary>
        public int MaximumNumberOfRuns { get; set; }

        /// <summary>
        ///     If the Workflow conditions have been met, in accordance with the previous run receipt if provided
        /// </summary>
        /// <param name="lastReceipt">Previous Workflow run receipt</param>
        /// <returns></returns>
        public bool IsValid(WorkflowReceipt lastReceipt)
        {
            if (ValidFrom > DateTime.UtcNow || ValidTo < DateTime.UtcNow)
                return false;
            if (lastReceipt != null)
            {
                if (!lastReceipt.WorkflowComplete)
                    return false;
                if (lastReceipt.WorkflowRunCount >= MaximumNumberOfRuns)
                    return false;
                if (DateTimeOffset.UtcNow - lastReceipt.Signature.Timestamp < TimeBetweenExecutions)
                    return false;
            }

            return true;
        }

        public override string ToString()
        {
            if (ValidFrom != DateTimeOffset.MinValue && ValidTo != DateTimeOffset.MaxValue) return string.Empty;
            var valid = $"Valid from {ValidFrom} to {ValidTo}.";
            if (TimeBetweenExecutionsMinutes > 0) valid += $" Must have {TimeBetweenExecutions} between runs.";
            if (MaximumNumberOfRuns > 1) valid += $" Can be run {MaximumNumberOfRuns} times";
            return valid;
        }
    }
}