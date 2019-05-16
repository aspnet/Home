// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys.Listener
{
    internal static class Utilities
    {
        internal static readonly int WriteRetryLimit = 1000;

        // When tests projects are run in parallel, overlapping port ranges can cause a race condition when looking for free
        // ports during dynamic port allocation.
        private const int BasePort = 8001;
        private const int MaxPort = 11000;
        private static int NextPort = BasePort;
        private static object PortLock = new object();

        internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

        internal static HttpSysListener CreateHttpServer(out string baseAddress)
        {
            string root;
            return CreateDynamicHttpServer(string.Empty, out root, out baseAddress);
        }

        internal static HttpSysListener CreateHttpServerReturnRoot(string path, out string root)
        {
            string baseAddress;
            return CreateDynamicHttpServer(path, out root, out baseAddress);
        }

        internal static HttpSysListener CreateDynamicHttpServer(string basePath, out string root, out string baseAddress)
        {
            lock (PortLock)
            {
                while (NextPort < MaxPort)
                {
                    var port = NextPort++;
                    var prefix = UrlPrefix.Create("http", "localhost", port, basePath);
                    root = prefix.Scheme + "://" + prefix.Host + ":" + prefix.Port;
                    baseAddress = prefix.ToString();
                    var listener = new HttpSysListener(new HttpSysOptions(), new LoggerFactory());
                    listener.Options.UrlPrefixes.Add(prefix);
                    try
                    {
                        listener.Start();
                        return listener;
                    }
                    catch (HttpSysException)
                    {
                        listener.Dispose();
                    }
                }
                NextPort = BasePort;
            }
            throw new Exception("Failed to locate a free port.");
        }

        internal static HttpSysListener CreateHttpsServer()
        {
            return CreateServer("https", "localhost", 9090, string.Empty);
        }

        internal static HttpSysListener CreateServer(string scheme, string host, int port, string path)
        {
            var listener = new HttpSysListener(new HttpSysOptions(), new LoggerFactory());
            listener.Options.UrlPrefixes.Add(UrlPrefix.Create(scheme, host, port, path));
            listener.Start();
            return listener;
        }

        /// <summary>
        /// AcceptAsync extension with timeout. This extension should be used in all tests to prevent
        /// unexpected hangs when a request does not arrive.
        /// </summary>
        internal static async Task<RequestContext> AcceptAsync(this HttpSysListener server, TimeSpan timeout)
        {
            var acceptTask = server.AcceptAsync().AsTask();
            var cts = new CancellationTokenSource();
            var completedTask = await Task.WhenAny(acceptTask, Task.Delay(timeout, cts.Token));

            if (completedTask == acceptTask)
            {
                cts.Cancel();
                var context = new RequestContext();
                context.InitializeCore(server, await acceptTask);
                return context;
            }
            else
            {
                server.Dispose();
                throw new TimeoutException("AcceptAsync has timed out.");
            }
        }

        // Fail if the given response task completes before the given accept task.
        internal static async Task<RequestContext> Before<T>(this Task<RequestContext> acceptTask, Task<T> responseTask)
        {
            var completedTask = await Task.WhenAny(acceptTask, responseTask);

            if (completedTask == acceptTask)
            {
                return await acceptTask;
            }
            else
            {
                var response = await responseTask;
                throw new InvalidOperationException("The response completed prematurely: " + response.ToString());
            }
        }
    }
}
