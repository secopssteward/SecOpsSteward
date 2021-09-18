using System;
using FluentAssertions;
using SecOpsSteward.Shared.Packaging;
using Xunit;

namespace SecOpsSteward.Tests.Core.Services
{
    public class PackageRepositoryTests
    {
        private readonly IPackageRepository _packageRepo;

        public PackageRepositoryTests(IPackageRepository packageRepo)
        {
            _packageRepo = packageRepo;
        }

        [Fact]
        public void RepositoryShouldBeEmptyAtStart()
        {
            _packageRepo.List().Result.Should().BeEmpty();
        }

        [Fact]
        public void GetInvalidPackageFromRepositoryShouldFail()
        {
            new Action(() => _ = _packageRepo.Get(TestValues.SamplePackage).Result)
                .Should().Throw<Exception>();
        }
    }
}