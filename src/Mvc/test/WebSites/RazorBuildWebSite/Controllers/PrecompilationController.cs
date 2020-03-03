﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace RazorBuildWebSite.Controllers
{
    public class PrecompilationController : Controller
    {
        public new ActionResult View()
        {
            return base.View();
        }
    }
}
