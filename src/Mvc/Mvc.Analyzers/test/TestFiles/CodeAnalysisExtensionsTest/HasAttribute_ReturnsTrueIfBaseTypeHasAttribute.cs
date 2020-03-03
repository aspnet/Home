﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    [Controller]
    public class HasAttribute_ReturnsTrueIfBaseTypeHasAttributeBase { }

    public class HasAttribute_ReturnsTrueIfBaseTypeHasAttribute : HasAttribute_ReturnsTrueIfBaseTypeHasAttributeBase { }
}
