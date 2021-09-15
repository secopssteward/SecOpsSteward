using FluentAssertions;
using System;
using Xunit;
using SecOpsSteward.Shared.NonceTracking;

namespace SecOpsSteward.Tests.Core.Services
{
    public class NonceTrackingServiceTests
    {
        private readonly INonceTrackingService _nonceTracker;

        public NonceTrackingServiceTests(INonceTrackingService nonceTracker) =>
            _nonceTracker = nonceTracker;

        [Fact]
        public void FirstRequestWithEmptyNonceShouldPass()
        {
            var requestId = Guid.NewGuid();
            _nonceTracker.ValidateNonce(TestValues.SampleAgent, requestId, string.Empty)
                .Result.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void InvalidNonceForNextRequestShouldFail()
        {
            var requestId = Guid.NewGuid();
            var newNonce = _nonceTracker.ValidateNonce(TestValues.SampleAgent, requestId, string.Empty).Result;

            newNonce.Should().NotBeNullOrEmpty();

            _nonceTracker.ValidateNonce(TestValues.SampleAgent, requestId, "abc123")
                .Result.Should().BeNullOrEmpty();
        }

        [Fact]
        public void GeneratedNonceForNextRequestShouldPass()
        {
            var requestId = Guid.NewGuid();
            var newNonce = _nonceTracker.ValidateNonce(TestValues.SampleAgent, requestId, string.Empty).Result;

            _nonceTracker.ValidateNonce(TestValues.SampleAgent, requestId, newNonce)
                .Result.Should().NotBeNullOrEmpty();
        }
    }
}
