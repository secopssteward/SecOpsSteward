using FluentAssertions;
using SecOpsSteward.Shared;
using System.Linq;
using Xunit;

namespace SecOpsSteward.Tests.Core
{
    public class AccessRulesTests
    {
        [Fact]
        public void AccessRulesShouldBeAddable()
        {
            var accessRules = new Shared.Configuration.AccessRules();
            accessRules.Add(TestValues.SampleGuidA, TestValues.SampleGuidB);

            var rule = accessRules.Items.First();
            
            rule.UserId.Should().Be(new ChimeraUserIdentifier(TestValues.SampleGuidA));
            rule.PackageId.Should().Be(new ChimeraPackageIdentifier(TestValues.SampleGuidB));
        }

        [Fact]
        public void AccessRulesShouldBeCorrect()
        {
            var accessRules = new Shared.Configuration.AccessRules();
            accessRules.Add(TestValues.SampleGuidA, TestValues.SampleGuidB);

            accessRules.HasAccess(TestValues.SampleGuidA, TestValues.SampleGuidB)
                .Should().BeTrue();
            accessRules.HasAccess(TestValues.SampleGuidB, TestValues.SampleGuidC)
                .Should().BeFalse();
        }

        [Fact]
        public void AccessRulesShouldBeRemovable()
        {
            var accessRules = new Shared.Configuration.AccessRules();
            accessRules.Add(TestValues.SampleGuidA, TestValues.SampleGuidB);
            accessRules.HasAccess(TestValues.SampleGuidA, TestValues.SampleGuidB)
                .Should().BeTrue();

            accessRules.Remove(TestValues.SampleGuidA, TestValues.SampleGuidB);

            accessRules.HasAccess(TestValues.SampleGuidA, TestValues.SampleGuidB)
                .Should().BeFalse();
        }
    }
}
