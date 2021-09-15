using FluentAssertions;
using SecOpsSteward.Shared;
using System;
using Xunit;

namespace SecOpsSteward.Tests.Core
{
    public class ServiceConfiguratorTests
    {
        [Fact]
        public void ConfiguratorShouldRetainOptionValues()
        {
            var serviceConfigurator = new ChimeraServiceConfigurator();
            serviceConfigurator["test1"] = "value1";

            serviceConfigurator.Options.Should().ContainKey("test1");
        }

        [Fact]
        public void ConfiguratorShouldTransformValues()
        {
            var serviceConfigurator = new ChimeraServiceConfigurator();
            serviceConfigurator.Derivations["test2"] = () => Guid.NewGuid().ToString();
            var val1 = serviceConfigurator["test2"];
            var val2 = serviceConfigurator["test2"];

            val1.Should().NotBeEquivalentTo(val2);
        }

        [Fact]
        public void ConfiguratorShouldPrioritizeDerivations()
        {
            var serviceConfigurator = new ChimeraServiceConfigurator();
            serviceConfigurator.Options["test1"] = "123";
            serviceConfigurator.Derivations["test1"] = () => Guid.NewGuid().ToString();

            serviceConfigurator["test1"].Should().NotBe("123");
        }
    }
}
