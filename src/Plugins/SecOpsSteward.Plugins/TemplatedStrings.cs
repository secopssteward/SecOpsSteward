using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SecOpsSteward.Plugins
{
    public static class TemplatedStrings
    {
        // {{$VAR_NAME}} <- evaluate "var_name" and use the result in the string replacement
        // {{$VAR_NAME_RETURNS_BOOL?TRUE_RESULT:FALSE_RESULT}} <- if the value associated with "var_name" returns "true", use the first result, otherwise use the second
        private static readonly Regex PathExtractor = new("{{\\$(.*?)}}");
        private static readonly Regex BooleanCheck = new("{{\\$(.*?)\\?(.*?)\\:(.*?)}}");

        /// <summary>
        ///     Generate a string based on a string with templates, replacing those with the corresponding values
        /// </summary>
        /// <param name="templateString">Template string to receive inputs</param>
        /// <param name="values">Values to use for replacement</param>
        /// <returns>Result string with populated values</returns>
        public static string PopulateInputsInTemplateString(string templateString, PluginOutputStructure values)
        {
            var newString = new string(templateString);
            foreach (Match match in PathExtractor.Matches(templateString))
            {
                var replace = match.Value;
                var value = string.Empty;

                // todo: maybe improve this with an enum selector based on integers?

                if (BooleanCheck.IsMatch(replace))
                    foreach (Match bMatch in BooleanCheck.Matches(replace))
                    {
                        var optA = bMatch.Groups[2].Value;
                        var optB = bMatch.Groups[3].Value;
                        bool val;
                        if (!bool.TryParse(values.SharedOutputs[bMatch.Groups[1].Value], out val))
                            throw new Exception("Value was not a boolean but template expected a boolean");
                        if (val)
                            value = optA;
                        else value = optB;
                    }
                else if (values.SharedOutputs.Contains(match.Groups[1].Value))
                    value = values.SharedOutputs[match.Groups[1].Value];
                else
                    throw new Exception("Shared Outputs does not contain expected key '" + match.Groups[1].Value + "'");

                newString = newString.Replace(replace, value);
            }

            return newString;
        }

        /// <summary>
        ///     Get the inputs from a string containing templates
        /// </summary>
        /// <param name="templateString">Template string to receive inputs</param>
        /// <returns>List of input variables</returns>
        public static List<string> GetRequiredInputsForTemplateString(string templateString)
        {
            return PathExtractor.Matches(templateString).Select(v => v.Groups[1].Value).Distinct().ToList();
        }
    }
}