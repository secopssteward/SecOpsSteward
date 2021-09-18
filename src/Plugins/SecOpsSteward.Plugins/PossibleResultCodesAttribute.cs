using System;
using System.Linq;

namespace SecOpsSteward.Plugins
{
    /// <summary>
    ///     Attribute declaring possible output results from the annotated Plugin; "Success" and "Failure" are included by
    ///     default
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PossibleResultCodesAttribute : Attribute
    {
        /// <summary>
        ///     Attribute declaring possible output results from the annotated Plugin
        /// </summary>
        public PossibleResultCodesAttribute(params string[] outputs)
        {
            Outputs = outputs.Concat(new[] {CommonResultCodes.Success, CommonResultCodes.Failure}).Distinct().ToArray();
        }

        /// <summary>
        ///     Possible outputs from this Plugin
        /// </summary>
        public string[] Outputs { get; }
    }

    /// <summary>
    ///     Common plugin outputs
    /// </summary>
    public static class CommonResultCodes
    {
        /// <summary>
        ///     Output returned if plugin runs successfully
        /// </summary>
        public const string Success = "Success";

        /// <summary>
        ///     Output returned if plugin runs unsuccessfully
        /// </summary>
        public const string Failure = "Failure";
    }
}