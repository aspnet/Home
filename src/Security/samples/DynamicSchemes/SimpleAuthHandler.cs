﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthSamples.DynamicSchemes
{
    public class SimpleOptions : AuthenticationSchemeOptions
    {
        public string DisplayMessage { get; set; }
    }

    public class SimpleAuthHandler : AuthenticationHandler<SimpleOptions>
    {
        public SimpleAuthHandler(IOptionsMonitor<SimpleOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            throw new NotImplementedException();
        }
    }
}
