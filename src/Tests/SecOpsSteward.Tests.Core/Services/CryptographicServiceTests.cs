using FluentAssertions;
using SecOpsSteward.Shared.Cryptography.Extensions;
using Xunit;

namespace SecOpsSteward.Tests.Core.Services
{
    public class CryptographicServiceTests
    {
        private readonly ICryptographicService _cryptoService;

        public CryptographicServiceTests(ICryptographicService cryptoService)
        {
            _cryptoService = cryptoService;
        }

        [Fact]
        public void KeyWrapShouldSucceed()
        {
            var key = TestValues.GetByteArray(32);
            _cryptoService.WrapKey(TestValues.SampleAgent, key).Result
                .Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void KeyUnwrapShouldSucceed()
        {
            var key = TestValues.GetByteArray(32);
            var wrapped = _cryptoService.WrapKey(TestValues.SampleAgent, key).Result;

            _cryptoService.UnwrapKey(TestValues.SampleAgent, wrapped)
                .Result.Should().BeEquivalentTo(key);
        }

        [Fact]
        public void KeyUnwrapWithDifferentAgentShouldFail()
        {
            var key = TestValues.GetByteArray(32);
            var wrapped = _cryptoService.WrapKey(TestValues.SampleAgent, key).Result;

            _cryptoService.UnwrapKey(TestValues.SampleAgent2, wrapped)
                .Result.Should().NotBeEquivalentTo(key);
        }

        [Fact]
        public void EncryptionShouldSucceed()
        {
            var data = TestValues.GetByteArray(32);
            _cryptoService.Encrypt(TestValues.SampleAgent, data).Result.Should().NotBeEmpty();
        }

        [Fact]
        public void DecryptionShouldSucceed()
        {
            var data = TestValues.GetByteArray(32);
            var encrypted = _cryptoService.Encrypt(TestValues.SampleAgent, data).Result;

            _cryptoService.Decrypt(TestValues.SampleAgent, encrypted).Result.Should().BeEquivalentTo(data);
        }

        [Fact]
        public void DecryptingWithDifferentAgentShouldFail()
        {
            var data = TestValues.GetByteArray(32);
            var encrypted = _cryptoService.Encrypt(TestValues.SampleAgent, data).Result;

            _cryptoService.Decrypt(TestValues.SampleAgent2, encrypted).Result
                .Should().NotBeEquivalentTo(data);
        }
    }
}