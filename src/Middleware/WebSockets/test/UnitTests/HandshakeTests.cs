// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.WebSockets.Tests
{
    public class HandshakeTests
    {
        [Fact]
        public void CreatesCorrectResponseKey()
        {
            // Example taken from https://tools.ietf.org/html/rfc6455#section-1.3
            var key = "dGhlIHNhbXBsZSBub25jZQ==";
            var expectedResponse = "s3pPLMBiTxaQ9kYGzzhZRbK+xOo=";

            var response = HandshakeHelpers.CreateResponseKey(key);

            Assert.Equal(expectedResponse, response);
        }

        [Theory]
        [InlineData("VUfWn1u2Ot0AICM6f+/8Zg==")]
        public void AcceptsValidRequestKeys(string key)
        {
            Assert.True(HandshakeHelpers.IsRequestKeyValid(key));
        }

        [Theory]
        // 17 bytes when decoded
        [InlineData("dGhpcyBpcyAxNyBieXRlcy4=")]
        // 15 bytes when decoded
        [InlineData("dGhpcyBpcyAxNWJ5dGVz")]
        [InlineData("")]
        [InlineData("24 length not base64 str")]
        public void RejectsInvalidRequestKeys(string key)
        {
            Assert.False(HandshakeHelpers.IsRequestKeyValid(key));
        }
    }
}
