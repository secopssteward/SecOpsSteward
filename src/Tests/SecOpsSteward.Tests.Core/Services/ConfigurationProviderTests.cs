using FluentAssertions;
using SecOpsSteward.Shared.Configuration;
using System.Threading.Tasks;
using Xunit;

namespace SecOpsSteward.Tests.Core.Services
{
    public class ConfigurationProviderTests
    {
        private readonly IConfigurationProvider _configProvider;

        public ConfigurationProviderTests(IConfigurationProvider configProvider) =>
            _configProvider = configProvider;

        [Fact]
        public async Task ConfigurationProviderShouldAddCorrectly()
        {
            await _configProvider.UpdateConfiguration(TestValues.SampleAgent,
                new AgentConfiguration()
                {
                    AgentId = TestValues.SampleAgent,
                    DisplayAlias = "Test Agent A"
                });
            var cfg = _configProvider.GetConfiguration(TestValues.SampleAgent).Result;
            cfg.AgentId.Should().Be(TestValues.SampleAgent);
            cfg.DisplayAlias.Should().Be("Test Agent A");
        }

        [Fact]
        public async Task ConfigurationProviderShouldUpdateCorrectly()
        {
            await _configProvider.UpdateConfiguration(TestValues.SampleAgent,
                new AgentConfiguration()
                {
                    AgentId = TestValues.SampleAgent,
                    DisplayAlias = "Test Agent A"
                });
            _configProvider.GetConfiguration(TestValues.SampleAgent)
                .Result.DisplayAlias.Should().Be("Test Agent A");

            await _configProvider.UpdateConfiguration(TestValues.SampleAgent,
                new AgentConfiguration()
                {
                    AgentId = TestValues.SampleAgent,
                    DisplayAlias = "Test Agent A-1"
                });
            _configProvider.GetConfiguration(TestValues.SampleAgent)
                .Result.DisplayAlias.Should().Be("Test Agent A-1");
        }

        [Fact]
        public async Task ConfigurationProviderShouldAllowList()
        {
            await _configProvider.UpdateConfiguration(TestValues.SampleAgent,
                new AgentConfiguration()
                {
                    AgentId = TestValues.SampleAgent,
                    DisplayAlias = "Test Agent A"
                });

            _configProvider.ListConfigurations().Result.Count.Should().Be(1);
        }
    }
}
