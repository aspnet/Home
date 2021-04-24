// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Infrastructure;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Represents a subscription to the <c>OnPersisting</c> callback that <see cref="ComponentStatePersistenceManager"/> callback will trigger
    /// when the application is being persisted.
    /// </summary>
    public readonly struct PersistingComponentStateSubscription : IDisposable
    {
        private readonly List<Func<Task>>? _callbacks;
        private readonly Func<Task>? _callback;

        internal PersistingComponentStateSubscription(List<Func<Task>> callbacks, Func<Task> callback)
        {
            _callbacks = callbacks;
            _callback = callback;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_callback != null)
            {
                _callbacks?.Remove(_callback);
            }
        }
    }
}
