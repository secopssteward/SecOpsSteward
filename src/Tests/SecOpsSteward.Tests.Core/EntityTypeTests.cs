using FluentAssertions;
using SecOpsSteward.Shared;
using Xunit;

namespace SecOpsSteward.Tests.Core
{
    public class EntityTypeTests
    {
        [Fact]
        public void EntityTypesShouldDiffer()
        {
            var agentId = new ChimeraAgentIdentifier(TestValues.SampleGuidA);
            var userId = new ChimeraUserIdentifier(TestValues.SampleGuidA);
            var packageId = new ChimeraPackageIdentifier(TestValues.SampleGuidA);

            agentId.Should().NotBeEquivalentTo(userId);
            agentId.Should().NotBeEquivalentTo(packageId);
            userId.Should().NotBeEquivalentTo(packageId);
        }

        [Fact]
        public void GuidShouldBeInString()
        {
            var agentId = new ChimeraAgentIdentifier(TestValues.SampleGuidA);
            var userId = new ChimeraAgentIdentifier(TestValues.SampleGuidA);
            var packageId = new ChimeraAgentIdentifier(TestValues.SampleGuidA);

            agentId.ToString().Should().EndWith(TestValues.SampleGuidA.ToString());
            userId.ToString().Should().EndWith(TestValues.SampleGuidA.ToString());
            packageId.ToString().Should().EndWith(TestValues.SampleGuidA.ToString());
        }

        [Fact]
        public void ShortIdShouldBeGuidStart()
        {
            var shortId = TestValues.SampleGuidA.ShortId();
            TestValues.SampleGuidA.ToString().Should().StartWith(shortId);
        }
    }
}