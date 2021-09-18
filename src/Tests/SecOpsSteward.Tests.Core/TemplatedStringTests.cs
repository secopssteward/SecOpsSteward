using System;
using System.Collections.Generic;
using FluentAssertions;
using SecOpsSteward.Plugins;
using Xunit;

namespace SecOpsSteward.Tests.Core
{
    public class TemplatedStringTests
    {
        [Fact]
        public void InvalidTemplateFails()
        {
            TemplatedStrings.GetRequiredInputsForTemplateString("{invalid}")
                .Should().BeEmpty();
        }

        [Fact]
        public void StringWithoutTemplatesIsUnchanged()
        {
            var values = new PluginOutputStructure(string.Empty)
            {
                SharedOutputs = new PluginOutputStructure.PluginSharedOutputs
                {
                    Outputs = new Dictionary<string, string>
                    {
                        {"/A/B/C", "123"},
                        {"/D/E/F", "456"}
                    }
                }
            };

            TemplatedStrings.PopulateInputsInTemplateString("aabbcc", values)
                .Should().Be("aabbcc");
        }

        [Fact]
        public void TemplateWithMissingValueFails()
        {
            var values = new PluginOutputStructure(string.Empty)
            {
                SharedOutputs = new PluginOutputStructure.PluginSharedOutputs
                {
                    Outputs = new Dictionary<string, string>
                    {
                        {"/A/B/C", "123"},
                        {"/D/E/F", "456"}
                    }
                }
            };

            new Action(() => TemplatedStrings.PopulateInputsInTemplateString("aabbcc {{$/G/H/I}}", values))
                .Should().Throw<Exception>();
        }

        [Fact]
        public void TemplateHasCorrectInputs()
        {
            TemplatedStrings.GetRequiredInputsForTemplateString("{{$/A/B/C}} aabbcc {{$/D/E/F}}")
                .Should().BeEquivalentTo("/A/B/C", "/D/E/F");
        }

        [Fact]
        public void TemplateWithDuplicatesHasCorrectInputs()
        {
            TemplatedStrings.GetRequiredInputsForTemplateString("{{$/A/B/C}} aabbcc {{$/D/E/F}} ccbbaa {{$/A/B/C}}")
                .Should().BeEquivalentTo("/A/B/C", "/D/E/F");
        }

        [Fact]
        public void TemplateStringIsReplacedCorrectly()
        {
            var values = new PluginOutputStructure(string.Empty)
            {
                SharedOutputs = new PluginOutputStructure.PluginSharedOutputs
                {
                    Outputs = new Dictionary<string, string>
                    {
                        {"/A/B/C", "123"},
                        {"/D/E/F", "456"}
                    }
                }
            };

            TemplatedStrings.PopulateInputsInTemplateString("{{$/A/B/C}} aabbcc {{$/D/E/F}}", values)
                .Should().Be("123 aabbcc 456");
        }

        [Fact]
        public void TemplateStringWithDuplicatesIsReplacedCorrectly()
        {
            var values = new PluginOutputStructure(string.Empty)
            {
                SharedOutputs = new PluginOutputStructure.PluginSharedOutputs
                {
                    Outputs = new Dictionary<string, string>
                    {
                        {"/A/B/C", "123"},
                        {"/D/E/F", "456"}
                    }
                }
            };

            TemplatedStrings.PopulateInputsInTemplateString("{{$/A/B/C}} aabbcc {{$/D/E/F}} ccbbaa {{$/A/B/C}}", values)
                .Should().Be("123 aabbcc 456 ccbbaa 123");
        }
    }
}