using FluentAssertions;
using System;
using Xunit;
using SecOpsSteward.Shared.NonceTracking;
using System.Threading.Tasks;

namespace SecOpsSteward.Tests.Core.Services
{
    public class NonceTrackingServiceTests
    {
        private readonly INonceTrackingService _nonceTracker;

        public NonceTrackingServiceTests(INonceTrackingService nonceTracker) =>
            _nonceTracker = nonceTracker;

        [Fact]
        public async Task FirstRequestWithEmptyNonceShouldPass()
        {
            var requestId = Guid.NewGuid();
            (await _nonceTracker.ValidateNonce(TestValues.SampleAgent, requestId, string.Empty))
                .Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task InvalidNonceForNextRequestShouldFail()
        {
            var requestId = Guid.NewGuid();
            var newNonce = await _nonceTracker.ValidateNonce(TestValues.SampleAgent, requestId, string.Empty);

            newNonce.Should().NotBeNullOrEmpty();

            (await _nonceTracker.ValidateNonce(TestValues.SampleAgent, requestId, "abc123"))
                .Should().BeNullOrEmpty();
        }

        [Fact]
        public async Task GeneratedNonceForNextRequestShouldPass()
        {
            var requestId = Guid.NewGuid();
            var newNonce = await _nonceTracker.ValidateNonce(TestValues.SampleAgent, requestId, string.Empty);

            _nonceTracker.ValidateNonce(TestValues.SampleAgent, requestId, newNonce)
                .Result.Should().NotBeNullOrEmpty();
        }
    }
}
