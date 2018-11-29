﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
{
    /// <summary>
    /// Provides configuration for XML formatters.
    /// </summary>
    public class MvcXmlOptions : IEnumerable<ICompatibilitySwitch>
    {
        private readonly CompatibilitySwitch<bool> _allowRfc7807CompliantProblemDetailsFormat;
        private readonly IReadOnlyList<ICompatibilitySwitch> _switches;

        /// <summary>
        /// Creates a new instance of <see cref="MvcXmlOptions"/>.
        /// </summary>
        public MvcXmlOptions()
        {
            _allowRfc7807CompliantProblemDetailsFormat = new CompatibilitySwitch<bool>(nameof(AllowRfc7807CompliantProblemDetailsFormat));

            _switches = new ICompatibilitySwitch[]
            {
                _allowRfc7807CompliantProblemDetailsFormat,
            };
        }

        /// <summary>
        /// Gets or sets a value inidicating whether <see cref="ProblemDetails"/> and <see cref="ValidationProblemDetails"/>
        /// are serialized in a format compliant with the RFC 7807 specification (https://tools.ietf.org/html/rfc7807).
        /// </summary>
        /// <value>
        /// The default value is <see langword="true"/>.
        /// </value>
        public bool AllowRfc7807CompliantProblemDetailsFormat
        {
            get => _allowRfc7807CompliantProblemDetailsFormat.Value;
            set => _allowRfc7807CompliantProblemDetailsFormat.Value = value;
        }

        public IEnumerator<ICompatibilitySwitch> GetEnumerator() => _switches.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
