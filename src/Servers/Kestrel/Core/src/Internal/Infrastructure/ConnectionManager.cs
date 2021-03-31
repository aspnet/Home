// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal class ConnectionManager
    {
        private readonly ConcurrentDictionary<long, ConnectionReference> _connectionReferences = new ConcurrentDictionary<long, ConnectionReference>();
        private readonly IKestrelTrace _trace;

        public ConnectionManager(IKestrelTrace trace, long? upgradedConnectionLimit)
            : this(trace, GetCounter(upgradedConnectionLimit))
        {
        }

        public ConnectionManager(IKestrelTrace trace, ResourceCounter upgradedConnections)
        {
            UpgradedConnectionCount = upgradedConnections;
            _trace = trace;
        }

        /// <summary>
        /// Connections that have been switched to a different protocol.
        /// </summary>
        public ResourceCounter UpgradedConnectionCount { get; }

        public void AddConnection(long id, ConnectionReference connectionReference)
        {
            if (!_connectionReferences.TryAdd(id, connectionReference))
            {
                throw new ArgumentException(nameof(id));
            }
        }

        public void RemoveConnection(long id)
        {
            if (!_connectionReferences.TryRemove(id, out var reference))
            {
                throw new ArgumentException(nameof(id));
            }

            if (reference.TryGetConnection(out var connection))
            {
                connection.Complete();
            }
        }

        public void Walk(Action<KestrelConnection> callback)
        {
            foreach (var kvp in _connectionReferences)
            {
                var reference = kvp.Value;

                if (reference.TryGetConnection(out var connection))
                {
                    callback(connection);
                }
                else if (_connectionReferences.TryRemove(kvp.Key, out reference))
                {
                    // It's safe to modify the ConcurrentDictionary in the foreach.
                    // The connection reference has become unrooted because the application never completed.
                    _trace.ApplicationNeverCompleted(reference.ConnectionId);
                    reference.StopTrasnsportTracking();
                }

                // If both conditions are false, the connection was removed during the heartbeat.
            }
        }

        private static ResourceCounter GetCounter(long? number)
            => number.HasValue
                ? ResourceCounter.Quota(number.Value)
                : ResourceCounter.Unlimited;
    }
}
