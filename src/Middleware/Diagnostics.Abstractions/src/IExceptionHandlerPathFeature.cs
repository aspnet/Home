// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Diagnostics
{
    /// <summary>
    /// Represents an exception handler with the original path of the request.
    /// </summary>
    public interface IExceptionHandlerPathFeature : IExceptionHandlerFeature
    {
        /// <summary>
        /// The portion of the request path that identifies the requested resource. The value
        /// is un-escaped.
        /// </summary>
        new string Path => ((IExceptionHandlerFeature)this).Path;
    }
}
