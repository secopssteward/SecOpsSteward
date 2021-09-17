using FluentAssertions;
using SecOpsSteward.Shared.Configuration.Models;
using Xunit;

namespace SecOpsSteward.Tests.Core
{
    public class AccessRulesTests
    {
        [Fact]
        public void AccessRulesShouldBeAddable()
        {
            var accessRules = new AccessRules();
            accessRules.Add(TestValues.SampleGuidA, TestValues.SampleGuidB);

            accessRules.HasAccess(TestValues.SampleGuidA, TestValues.SampleGuidB)
                .Should().BeTrue();
        }

        [Fact]
        public void AccessRulesShouldBeCorrect()
        {
            var accessRules = new AccessRules();
            accessRules.Add(TestValues.SampleGuidA, TestValues.SampleGuidB);

            accessRules.HasAccess(TestValues.SampleGuidA, TestValues.SampleGuidB)
                .Should().BeTrue();
            accessRules.HasAccess(TestValues.SampleGuidB, TestValues.SampleGuidC)
                .Should().BeFalse();
        }

        [Fact]
        public void AccessRulesShouldBeRemovable()
        {
            var accessRules = new AccessRules();
            accessRules.Add(TestValues.SampleGuidA, TestValues.SampleGuidB);
            accessRules.HasAccess(TestValues.SampleGuidA, TestValues.SampleGuidB)
                .Should().BeTrue();

            accessRules.Remove(TestValues.SampleGuidA, TestValues.SampleGuidB);

            accessRules.HasAccess(TestValues.SampleGuidA, TestValues.SampleGuidB)
                .Should().BeFalse();
        }
    }
}
