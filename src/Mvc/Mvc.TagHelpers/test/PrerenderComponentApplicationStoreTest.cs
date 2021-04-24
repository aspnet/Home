// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    public class PrerenderComponentApplicationStoreTest
    {
        [Fact]
        public async Task PersistStateAsync_PersistsGivenState()
        {
            // Arrange
            var expected = "eyJNeVZhbHVlIjoiQVFJREJBPT0ifQ==";
            var store = new PrerenderComponentApplicationStore();
            var state = new Dictionary<string, ReadOnlySequence<byte>>()
            {
                ["MyValue"] = new ReadOnlySequence<byte>(new byte[] {1,2,3,4})
            };

            // Act
            await store.PersistStateAsync(state);

            // Assert
            Assert.Equal(expected, store.PersistedState.Span.ToString());
        }

        [Fact]
        public async Task GetPersistedStateAsync_RestoresPreexistingStateAsync()
        {
            // Arrange
            var persistedState = "eyJNeVZhbHVlIjoiQVFJREJBPT0ifQ==";
            var store = new PrerenderComponentApplicationStore(persistedState);
            var expected = new Dictionary<string, ReadOnlySequence<byte>>()
            {
                ["MyValue"] = new ReadOnlySequence<byte>(new byte [] { 1, 2, 3, 4 })
            };

            // Act
            var state = await store.GetPersistedStateAsync();

            // Assert
            Assert.Equal(
                expected.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray()),
                state.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray()));
        }
    }
}
